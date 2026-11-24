using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class LoadingSession
{
    private const int MaxAutoRetryCount = 2;
    private const float AutoRetryBaseDelaySeconds = 1f;
    private const float FailureDisplaySeconds = 2f;

    private LoadingSequence _loadingSequence;
    private SceneLoadStep _sceneLoadStep;

    public event Action<int, int> RetryStarted;

    public event Action FallingBackToBoot;

    public event Action DownloadStarted;

    public ILoadingStatus Status => _loadingSequence;

    public async UniTask<LoadResult> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await LoadAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LoadResult.Canceled;
            }

            DebugUtility.LogWarning($"{nameof(LoadingSession)}: unexpected error.\n{exception}");
            return await FallBackToBootAsync(cancellationToken);
        }
    }

    public void ConfirmDownload()
    {
        _loadingSequence.ConfirmPendingDownload();
    }

    public LoadResult DeclineDownload()
    {
        string stepName = _loadingSequence.CurrentStepName;
        LoadFailure failure = LoadFailure.Create(stepName, LoadErrorCodes.DownloadDeclined, LoadFailureAction.Quit);

        DebugUtility.LogWarning($"{nameof(LoadingSession)}: loading failed. code={failure.ErrorCode}, step={failure.StepName}, action={failure.Action}");

        return LoadResult.Failed(failure);
    }

    public void GiveUp(LoadFailure failure)
    {
        switch (failure.Action)
        {
            case LoadFailureAction.GoToTitle:
                SceneTransition.FallBackTo(SceneNames.Boot);
                break;
            case LoadFailureAction.Quit:
                SceneTransition.Cancel();
                ApplicationUtility.Quit();
                break;
            case LoadFailureAction.Retry:
            case LoadFailureAction.ReturnToPrevious:
            default:
                SceneTransition.ReturnToPreviousScene();
                break;
        }
    }

    private async UniTask<LoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        if (_loadingSequence == null && !TryPrepare())
        {
            return await FallBackToBootAsync(cancellationToken);
        }

        int autoRetryCount = 0;

        while (true)
        {
            (Exception exception, long pendingDownloadBytes) = await AttemptLoadAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return LoadResult.Canceled;
            }

            if (exception == null)
            {
                if (pendingDownloadBytes > 0)
                {
                    return LoadResult.DownloadConfirmationRequired(pendingDownloadBytes);
                }

                break;
            }

            LoadFailure failure = LoadFailure.FromException(exception, _loadingSequence.CurrentStepName);

            DebugUtility.LogWarning($"{nameof(LoadingSession)}: loading failed. code={failure.ErrorCode}, step={failure.StepName}, action={failure.Action}\n{exception}");

            if (!failure.IsRetryable || autoRetryCount >= MaxAutoRetryCount)
            {
                return LoadResult.Failed(failure);
            }

            autoRetryCount++;

            bool isCanceled = await WaitAutoRetryAsync(autoRetryCount, cancellationToken);

            if (isCanceled)
            {
                return LoadResult.Canceled;
            }
        }

        SceneTransition.Complete();

        if (_sceneLoadStep != null)
        {
            _sceneLoadStep.Activate();
        }

        return LoadResult.Succeeded;
    }

    private bool TryPrepare()
    {
        ILoadPlan plan = SceneTransition.CurrentPlan;

        if (plan == null)
        {
            DebugUtility.LogWarning($"{nameof(LoadingSession)}: no load plan. Open the loading scene only through {nameof(SceneTransition)}.{nameof(SceneTransition.Start)}(plan), not by {nameof(UnityEngine.SceneManagement.SceneManager)}.LoadScene or by playing it directly.");
            return false;
        }

        SceneLoadStep sceneLoadStep = CreateSceneLoadStep(plan);

        if (!TryCreateLoadingSequence(plan, sceneLoadStep, out LoadingSequence loadingSequence))
        {
            return false;
        }

        _sceneLoadStep = sceneLoadStep;
        _loadingSequence = loadingSequence;
        _loadingSequence.DownloadStarted += OnSequenceDownloadStarted;
        return true;
    }

    private static SceneLoadStep CreateSceneLoadStep(ILoadPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.TargetSceneName))
        {
            return null;
        }

        return new SceneLoadStep(plan.TargetSceneName);
    }

    private static bool TryCreateLoadingSequence(ILoadPlan plan, SceneLoadStep sceneLoadStep, out LoadingSequence loadingSequence)
    {
        loadingSequence = null;

        IReadOnlyList<ILoadStep> steps = plan.CreateSteps();

        if (steps == null)
        {
            DebugUtility.LogWarning($"{nameof(LoadingSession)}: {plan.GetType().Name}.{nameof(ILoadPlan.CreateSteps)}() returned null. Return Array.Empty<{nameof(ILoadStep)}>() when there are no steps.");
            return false;
        }

        LoadingSequence sequence = new LoadingSequence();

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] == null)
            {
                DebugUtility.LogWarning($"{nameof(LoadingSession)}: {plan.GetType().Name}.{nameof(ILoadPlan.CreateSteps)}() returned a null step at index {i}.");
                return false;
            }

            sequence.AddStep(steps[i]);
        }

        if (sceneLoadStep != null)
        {
            sequence.AddStep(sceneLoadStep);
        }

        loadingSequence = sequence;
        return true;
    }

    private async UniTask<(Exception Exception, long PendingDownloadBytes)> AttemptLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            long pendingDownloadBytes = await _loadingSequence.RunRemainingStepsAsync(cancellationToken);
            return (null, pendingDownloadBytes);
        }
        catch (Exception exception)
        {
            return (exception, 0);
        }
    }

    private async UniTask<bool> WaitAutoRetryAsync(int autoRetryCount, CancellationToken cancellationToken)
    {
        RetryStarted?.Invoke(autoRetryCount, MaxAutoRetryCount);

        float delaySeconds = AutoRetryBaseDelaySeconds * (1 << (autoRetryCount - 1));

        UniTask delayTask = UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), ignoreTimeScale: true, cancellationToken: cancellationToken);

        return await delayTask.SuppressCancellationThrow();
    }

    private async UniTask<LoadResult> FallBackToBootAsync(CancellationToken cancellationToken)
    {
        FallingBackToBoot?.Invoke();

        UniTask displayTask = UniTask.Delay(TimeSpan.FromSeconds(FailureDisplaySeconds), ignoreTimeScale: true, cancellationToken: cancellationToken);
        bool isCanceled = await displayTask.SuppressCancellationThrow();

        if (isCanceled)
        {
            return LoadResult.Canceled;
        }

        SceneTransition.FallBackTo(SceneNames.Boot);
        return LoadResult.Redirected;
    }

    private void OnSequenceDownloadStarted()
    {
        DownloadStarted?.Invoke();
    }
}
