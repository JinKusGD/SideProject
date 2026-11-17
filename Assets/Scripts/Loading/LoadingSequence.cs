using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public sealed class LoadingSequence
{
    private sealed class StepRunner : IProgress<float>
    {
        public StepRunner(ILoadStep step)
        {
            Step = step;
        }

        public ILoadStep Step { get; }

        public float Progress { get; private set; }

        public void Report(float value)
        {
            Progress = Mathf.Clamp01(value);
        }

        public void Complete()
        {
            Progress = 1f;
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            return Step.ExecuteAsync(this, cancellationToken);
        }
    }

    private readonly List<StepRunner> _runners = new List<StepRunner>();

    private StepRunner _currentRunner;

    private bool _isStarted;

    public bool IsFailed { get; private set; }

    public float TotalProgress
    {
        get
        {
            float weightedProgress = 0f;
            float totalWeight = 0f;

            foreach (StepRunner runner in _runners)
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

        ThrowIfStarted();

        _runners.Add(new StepRunner(step));
    }

    public async UniTask ExecuteAsync(CancellationToken cancellationToken)
    {
        ThrowIfStarted();

        _isStarted = true;

        foreach (StepRunner runner in _runners)
        {
            _currentRunner = runner;

            try
            {
                await runner.ExecuteAsync(cancellationToken);
            }
            catch
            {
                IsFailed = true;

                throw;
            }

            runner.Complete();
        }

        _currentRunner = null;
    }

    private void ThrowIfStarted()
    {
        if (_isStarted)
        {
            throw new InvalidOperationException($"{nameof(LoadingSequence)}: already started. Create a new sequence for each load.");
        }
    }
}
