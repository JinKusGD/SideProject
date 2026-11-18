using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class LoadingSequence : ILoadingProgress
{
    private readonly List<LoadStepRunner> _runners = new List<LoadStepRunner>();

    private LoadStepRunner _currentRunner;

    private LoadingSequenceState _loadingSequenceState;

    public float TotalProgress
    {
        get
        {
            float weightedProgress = 0f;
            float totalWeight = 0f;

            foreach (LoadStepRunner runner in _runners)
            {
                weightedProgress += runner.Progress * runner.Step.Weight;
                totalWeight += runner.Step.Weight;
            }

            if (totalWeight <= 0f)
            {
                return 1f;
            }

            return weightedProgress / totalWeight;
        }
    }

    public string CurrentStepName
    {
        get
        {
            if (_currentRunner == null)
            {
                return string.Empty;
            }

            return _currentRunner.Step.Name;
        }
    }

    public void AddStep(ILoadStep step)
    {
        if (step == null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (_loadingSequenceState != LoadingSequenceState.NotStarted)
        {
            throw new InvalidOperationException($"{nameof(LoadingSequence)}: cannot add steps after execution started.");
        }

        _runners.Add(new LoadStepRunner(step));
    }

    public async UniTask RunRemainingStepsAsync(CancellationToken cancellationToken)
    {
        if (_loadingSequenceState == LoadingSequenceState.Running)
        {
            throw new InvalidOperationException($"{nameof(LoadingSequence)}: already running.");
        }

        if (_loadingSequenceState == LoadingSequenceState.Completed)
        {
            throw new InvalidOperationException($"{nameof(LoadingSequence)}: already completed. Create a new sequence for each load.");
        }

        _loadingSequenceState = LoadingSequenceState.Running;

        try
        {
            foreach (LoadStepRunner runner in _runners)
            {
                if (runner.IsCompleted)
                {
                    continue;
                }

                _currentRunner = runner;
                runner.ResetProgress();

                await runner.ExecuteAsync(cancellationToken);

                runner.Complete();
            }

            _currentRunner = null;
            _loadingSequenceState = LoadingSequenceState.Completed;
        }
        catch
        {
            _loadingSequenceState = LoadingSequenceState.Failed;
            throw;
        }
    }

    private sealed class LoadStepRunner : IProgress<float>
    {
        public LoadStepRunner(ILoadStep step)
        {
            Step = step;
        }

        public ILoadStep Step { get; }

        public float Progress { get; private set; }

        public bool IsCompleted { get; private set; }

        public void Report(float value)
        {
            Progress = Mathf.Clamp01(value);
        }

        public void Complete()
        {
            Progress = 1f;
            IsCompleted = true;
        }

        public void ResetProgress()
        {
            Progress = 0f;
        }

        public async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            TimeSpan timeout = Step.Timeout;

            if (timeout <= TimeSpan.Zero)
            {
                await Step.ExecuteAsync(this, cancellationToken);
                return;
            }

            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutSource.CancelAfter(timeout);

            try
            {
                await Step.ExecuteAsync(this, timeoutSource.Token);
            }
            catch (OperationCanceledException exception) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new LoadStepException(LoadErrorCodes.Timeout, LoadFailureAction.Retry, $"{nameof(LoadingSequence)}: step '{Step.Name}' timed out after {timeout.TotalSeconds}s.", exception);
            }
        }
    }
}
