using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    private const float MinimumFillSeconds = 1f;
    private const float FillSmoothSeconds = 0.4f;
    private const float FillCompleteThreshold = 0.999f;
    private const float SceneWeight = 0.1f;

    private const float FailureDisplaySeconds = 2f;

    private const string ProgressTextFormat = "{0}%";
    private const string MessageFormat = "Loading {0}...";
    private const string FailureMessage = "Loading failed.";

    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _messageText;

    private string _displayedStepName;
    private int _displayedPercent = -1;

    private void Awake()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_progressBar, nameof(_progressBar), this);
        hasAllReferences &= UnityUtility.CheckReference(_progressText, nameof(_progressText), this);
        hasAllReferences &= UnityUtility.CheckReference(_messageText, nameof(_messageText), this);

        enabled = hasAllReferences;
    }

    private void Start()
    {
        SetProgress(0f);

        LoadAsync().Forget();
    }

    private async UniTask LoadAsync()
    {
        CancellationToken cancellationToken = destroyCancellationToken;

        if (!SceneLoader.IsLoading)
        {
            DebugUtility.LogWarning($"{nameof(LoadingSceneController)}: no load request. Use {nameof(SceneLoader)}.{nameof(SceneLoader.Load)}().");
            ShowFailure();
            return;
        }

        SceneLoadStep sceneStep = new SceneLoadStep(SceneLoader.TargetSceneName, SceneWeight);

        LoadingSequence sequence = CreateSequence(sceneStep);

        UniTask workTask = sequence.ExecuteAsync(cancellationToken);

        bool isBarFilled = await FillBarAsync(sequence, cancellationToken);

        bool isWorkCompleted = await WaitWorkAsync(workTask, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!isWorkCompleted || !isBarFilled)
        {
            await ReturnToPreviousSceneAsync(cancellationToken);
            return;
        }

        SceneLoader.Complete();

        sceneStep.Activate();
    }

    private static LoadingSequence CreateSequence(SceneLoadStep sceneStep)
    {
        LoadingSequence sequence = new LoadingSequence();

        foreach (ILoadStep step in SceneLoader.Steps)
        {
            sequence.AddStep(step);
        }

        sequence.AddStep(sceneStep);

        return sequence;
    }

    private static async UniTask<bool> WaitWorkAsync(UniTask workTask, CancellationToken cancellationToken)
    {
        try
        {
            await workTask;

            return true;
        }
        catch (OperationCanceledException exception)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LogLoadFailure(exception);
            }

            return false;
        }
        catch (Exception exception)
        {
            LogLoadFailure(exception);

            return false;
        }
    }

    private static void LogLoadFailure(Exception exception)
    {
        DebugUtility.LogWarning($"{nameof(LoadingSceneController)}: loading failed. {exception}");
    }

    private async UniTask ReturnToPreviousSceneAsync(CancellationToken cancellationToken)
    {
        ShowFailure();

        UniTask delayTask = UniTask.Delay(TimeSpan.FromSeconds(FailureDisplaySeconds), ignoreTimeScale: true, cancellationToken: cancellationToken);
        bool isCanceled = await delayTask.SuppressCancellationThrow();

        if (isCanceled)
        {
            return;
        }

        SceneLoader.ReturnToPreviousScene();
    }

    private async UniTask<bool> FillBarAsync(LoadingSequence sequence, CancellationToken cancellationToken)
    {
        float maxFillSpeed = 1f / MinimumFillSeconds;

        float progress = 0f;
        float fillVelocity = 0f;

        while (progress < 1f)
        {
            if (sequence.IsFailed)
            {
                return false;
            }

            float workProgress = sequence.TotalProgress;

            progress = Mathf.SmoothDamp(progress, workProgress, ref fillVelocity, FillSmoothSeconds, maxFillSpeed, Time.unscaledDeltaTime);

            if (workProgress >= 1f && progress >= FillCompleteThreshold)
            {
                progress = 1f;
            }

            SetProgress(progress);
            SetMessage(sequence.CurrentStepName);

            UniTask yieldUpdateTask = UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            bool isCanceled = await yieldUpdateTask.SuppressCancellationThrow();

            if (isCanceled)
            {
                return false;
            }
        }

        return true;
    }

    private void ShowFailure()
    {
        _displayedStepName = null;
        _messageText.text = FailureMessage;
    }

    private void SetProgress(float progress)
    {
        _progressBar.value = progress;

        int percent = Mathf.RoundToInt(progress * 100f);

        if (percent == _displayedPercent)
        {
            return;
        }

        _displayedPercent = percent;
        _progressText.text = string.Format(ProgressTextFormat, percent);
    }

    private void SetMessage(string stepName)
    {
        if (string.IsNullOrWhiteSpace(stepName) || string.Equals(stepName, _displayedStepName, StringComparison.Ordinal))
        {
            return;
        }

        _displayedStepName = stepName;
        _messageText.text = string.Format(MessageFormat, stepName);
    }
}
