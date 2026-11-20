using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class LoadingSequence : ILoadingStatus
{
    private readonly List<LoadStepRunner> _runners = new List<LoadStepRunner>();

    private LoadStepRunner _currentRunner;
    private LoadStepRunner _pendingDownloadRunner;

    private LoadingSequenceState _loadingSequenceState;

    public event Action DownloadStarted;

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

    public bool IsRunning => _loadingSequenceState == LoadingSequenceState.Running;

    public bool TryGetCurrentDownload(out DownloadStatus status)
    {
        status = default;

        if (_currentRunner == null || !_currentRunner.IsDownloadConfirmed)
        {
            return false;
        }

        IDownloadStep download = _currentRunner.Step as IDownloadStep;

        if (download == null || download.TotalBytes <= 0)
        {
            return false;
        }

        status = new DownloadStatus(download.DownloadedBytes, download.TotalBytes);
        return true;
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

    public async UniTask<long> RunRemainingStepsAsync(CancellationToken cancellationToken)
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

                long pendingDownloadBytes = await PrepareDownloadAsync(runner, cancellationToken);

                if (pendingDownloadBytes > 0)
                {
                    _pendingDownloadRunner = runner;
                    _loadingSequenceState = LoadingSequenceState.Paused;
                    return pendingDownloadBytes;
                }

                await runner.RunAsync(cancellationToken);
            }

            _currentRunner = null;
            _loadingSequenceState = LoadingSequenceState.Completed;
            return 0;
        }
        catch
        {
            _loadingSequenceState = LoadingSequenceState.Failed;
            throw;
        }
    }

    public void ConfirmPendingDownload()
    {
        if (_pendingDownloadRunner == null)
        {
            throw new InvalidOperationException($"{nameof(LoadingSequence)}: no download is waiting for confirmation.");
        }

        _pendingDownloadRunner.ConfirmDownload();
        _pendingDownloadRunner = null;
    }

    private async UniTask<long> PrepareDownloadAsync(LoadStepRunner runner, CancellationToken cancellationToken)
    {
        IDownloadStep downloadStep = runner.Step as IDownloadStep;

        if (downloadStep == null)
        {
            return 0;
        }

        if (!runner.IsDownloadConfirmed)
        {
            long downloadBytes = await downloadStep.GetDownloadSizeAsync(cancellationToken);

            if (downloadBytes > 0)
            {
                return downloadBytes;
            }

            runner.ConfirmDownload();
        }

        if (!runner.IsDownloadStarted)
        {
            runner.StartDownload();
            DownloadStarted?.Invoke();
        }

        return 0;
    }

    private enum LoadingSequenceState
    {
        NotStarted,
        Running,
        Paused,
        Completed,
        Failed
    }

    private sealed class LoadStepRunner
    {
        public LoadStepRunner(ILoadStep step)
        {
            Step = step;
        }

        public ILoadStep Step { get; }

        public bool IsCompleted { get; private set; }

        public bool IsDownloadConfirmed { get; private set; }

        public bool IsDownloadStarted { get; private set; }

        public void ConfirmDownload()
        {
            IsDownloadConfirmed = true;
        }

        public void StartDownload()
        {
            IsDownloadStarted = true;
        }

        public async UniTask RunAsync(CancellationToken cancellationToken)
        {
            await ExecuteStepAsync(cancellationToken);
            IsCompleted = true;
        }

        private async UniTask ExecuteStepAsync(CancellationToken cancellationToken)
        {
            TimeSpan timeout = Step.Timeout;

            if (timeout <= TimeSpan.Zero)
            {
                await Step.ExecuteAsync(cancellationToken);
                return;
            }

            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutSource.CancelAfter(timeout);

            try
            {
                await Step.ExecuteAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException exception) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new LoadStepException(LoadErrorCodes.Timeout, LoadFailureAction.Retry, $"{nameof(LoadingSequence)}: step '{Step.Name}' timed out after {timeout.TotalSeconds}s.", exception);
            }
        }
    }
}
