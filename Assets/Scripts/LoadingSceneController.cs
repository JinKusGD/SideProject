using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    private const float MinimumDisplaySeconds = 1f;

    private const float TestWorkSeconds = 50.0f;

    private const string ProgressTextFormat = "{0}%";

    private const string TestMessage = "Loading test...";
    private const string SceneLoadMessage = "Loading scene...";

    [SerializeField] private Slider _progressBar;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private TMP_Text _messageText;

    private void Awake()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_progressBar, nameof(_progressBar), this);
        hasAllReferences &= UnityUtility.CheckReference(_progressText, nameof(_progressText), this);
        hasAllReferences &= UnityUtility.CheckReference(_messageText, nameof(_messageText), this);

        enabled = hasAllReferences;

        if (hasAllReferences)
        {
            SetProgress(0f);
        }
    }

    private void Start()
    {
        LoadAsync().Forget();
    }

    private async UniTask LoadAsync()
    {
        //string targetSceneName = SceneLoader.TargetSceneName;
        string targetSceneName = "Game";

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            //추후 수정
            DebugUtility.LogError($"{nameof(LoadingSceneController)}: target scene is not set. Use.");
            return;
        }

        CancellationToken cancellationToken = destroyCancellationToken;

        bool isTestLoaded = await LoadTestAsync(cancellationToken);
        
        if (!isTestLoaded)
        {
            return;
        }

        await LoadTargetSceneAsync(targetSceneName, cancellationToken);
    }

    private async UniTask<bool> LoadTestAsync(CancellationToken cancellationToken)
    {
        UniTask delayTask = UniTask.Delay(TimeSpan.FromSeconds(TestWorkSeconds), cancellationToken: cancellationToken);

        UniTaskLoadProgress workProgress = new UniTaskLoadProgress(delayTask);

        bool isCompleted = await FillBarAsync(TestMessage, workProgress, cancellationToken);
      
        if (!isCompleted)
        {
            return false;
        }

        await delayTask;
       
        return isCompleted;
    }

    private async UniTask<bool> LoadTargetSceneAsync(string targetSceneName, CancellationToken cancellationToken)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);
        loadOperation.allowSceneActivation = false;

        SceneLoadProgress sceneLoadProgress = new SceneLoadProgress(loadOperation);

        bool isCompleted = await FillBarAsync(SceneLoadMessage, sceneLoadProgress, cancellationToken);

        if (isCompleted)
        {
            loadOperation.allowSceneActivation = true;
        }

        return isCompleted;
    }

    private async UniTask<bool> FillBarAsync(string message, ILoadProgress loadProgress, CancellationToken cancellationToken)
    {
        _messageText.text = message;

        float startSeconds = Time.realtimeSinceStartup;
        float progress = 0f;

        while (progress < 1f)
        {
            float elapsedSeconds = Time.realtimeSinceStartup - startSeconds;

            float workProgress = loadProgress.Value;
            float timeProgress = Mathf.Clamp01(elapsedSeconds / MinimumDisplaySeconds);

            progress = Mathf.Min(workProgress, timeProgress);

            SetProgress(progress);

            UniTask yieldUpdateTask = UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            bool isCanceled = await yieldUpdateTask.SuppressCancellationThrow();

            if (isCanceled)
            {
                return false;
            }
        }

        return true;
    }

    private void SetProgress(float progress)
    {
        _progressBar.value = progress;
        _progressText.text = string.Format(ProgressTextFormat, Mathf.RoundToInt(progress * 100f));
    }
}
