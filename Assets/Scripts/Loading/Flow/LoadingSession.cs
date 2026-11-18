using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class LoadingSession
{
    private const int MaxAutoRetryCount = 2;
    private const float AutoRetryBaseDelaySeconds = 1f;
    private const float FailureDisplaySeconds = 2f;

    private readonly ILoadingView _view;

    public LoadingSession(ILoadingView view)
    {
        if (view == null)
        {
            throw new ArgumentNullException(nameof(view));
        }

        _view = view;
    }

    public async UniTask RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            DebugUtility.LogWarning($"{nameof(LoadingSession)}: unexpected error.\n{exception}");
            await FallBackToLoginAsync(cancellationToken);
        }
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

        sequence.AddStep(sceneLoadStep);

        loadingSequence = sequence;
        return true;
    }

    private static void GiveUp(LoadFailureAction action)
    {
        switch (action)
        {
            case LoadFailureAction.GoToLogin:
                SceneTransition.FallBackTo(SceneNames.Login);
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

    private async UniTask LoadAsync(CancellationToken cancellationToken)
    {
        ILoadPlan plan = SceneTransition.CurrentPlan;

        if (plan == null)
        {
            DebugUtility.LogWarning($"{nameof(LoadingSession)}: no load plan. Open the loading scene only through {nameof(SceneTransition)}.{nameof(SceneTransition.Start)}(plan), not by {nameof(UnityEngine.SceneManagement.SceneManager)}.LoadScene or by playing it directly.");
            await FallBackToLoginAsync(cancellationToken);
            return;
        }

        SceneLoadStep sceneLoadStep = new SceneLoadStep(plan.TargetSceneName, plan.SceneLoadWeight);

        if (!TryCreateLoadingSequence(plan, sceneLoadStep, out LoadingSequence loadingSequence))
        {
            await FallBackToLoginAsync(cancellationToken);
            return;
        }

        int autoRetryCount = 0;

        while (true)
        {
            Exception exception = await AttemptLoadAsync(loadingSequence, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (exception == null)
            {
                break;
            }

            LoadFailure failure = LoadFailure.FromException(exception, loadingSequence.CurrentStepName);

            DebugUtility.LogWarning($"{nameof(LoadingSession)}: loading failed. code={failure.ErrorCode}, step={failure.StepName}, action={failure.Action}\n{exception}");

            if (failure.IsRetryable && autoRetryCount < MaxAutoRetryCount)
            {
                autoRetryCount++;

                bool isCanceled = await WaitAutoRetryAsync(autoRetryCount, cancellationToken);

                if (isCanceled)
                {
                    return;
                }

                continue;
            }

            UniTask<LoadingFailureChoice> choiceTask = _view.AskFailureChoiceAsync(failure, cancellationToken);
            (bool isChoiceCanceled, LoadingFailureChoice choice) = await choiceTask.SuppressCancellationThrow();

            if (isChoiceCanceled)
            {
                return;
            }

            if (choice == LoadingFailureChoice.Retry)
            {
                autoRetryCount = 0;
                continue;
            }

            GiveUp(failure.Action);
            return;
        }

        sceneLoadStep.Activate();

        SceneTransition.Complete();
    }

    private async UniTask<Exception> AttemptLoadAsync(LoadingSequence loadingSequence, CancellationToken cancellationToken)
    {
        using CancellationTokenSource viewSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        UniTask sequenceTask = loadingSequence.RunRemainingStepsAsync(cancellationToken);
        UniTask viewTask = _view.ShowProgressAsync(loadingSequence, viewSource.Token);

        try
        {
            await sequenceTask;
        }
        catch (Exception exception)
        {
            viewSource.Cancel();
            await viewTask.SuppressCancellationThrow();

            return exception;
        }

        await viewTask.SuppressCancellationThrow();

        return null;
    }

    private async UniTask<bool> WaitAutoRetryAsync(int autoRetryCount, CancellationToken cancellationToken)
    {
        _view.ShowRetrying(autoRetryCount, MaxAutoRetryCount);

        float delaySeconds = AutoRetryBaseDelaySeconds * (1 << (autoRetryCount - 1));

        UniTask delayTask = UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), ignoreTimeScale: true, cancellationToken: cancellationToken);

        return await delayTask.SuppressCancellationThrow();
    }

    private async UniTask FallBackToLoginAsync(CancellationToken cancellationToken)
    {
        _view.ShowFailed();

        UniTask displayTask = UniTask.Delay(TimeSpan.FromSeconds(FailureDisplaySeconds), ignoreTimeScale: true, cancellationToken: cancellationToken);
        bool isCanceled = await displayTask.SuppressCancellationThrow();

        if (isCanceled)
        {
            return;
        }

        SceneTransition.FallBackTo(SceneNames.Login);
    }
}
