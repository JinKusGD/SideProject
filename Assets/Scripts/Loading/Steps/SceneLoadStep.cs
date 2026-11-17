using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoadStep : ILoadStep
{
    private const string SceneStepName = "Scene";

    private const float SceneReadyProgress = 0.9f;

    private readonly string _sceneName;

    private AsyncOperation _loadOperation;

    public SceneLoadStep(string sceneName, float weight)
    {
        _sceneName = sceneName;
        Name = SceneStepName;
        Weight = weight;
    }

    public string Name { get; }
    public float Weight { get; }

    public async UniTask ExecuteAsync(IProgress<float> progress, CancellationToken cancellationToken)
    {
        // 로드할 수 없는 씬이면 Unity가 에러 로그를 남기기 전에 먼저 막는다. (DebugUtility는 에러 로그가 나면 앱을 종료한다)
        if (string.IsNullOrWhiteSpace(_sceneName) || !Application.CanStreamedLevelBeLoaded(_sceneName))
        {
            throw new InvalidOperationException($"{nameof(SceneLoadStep)}: scene '{_sceneName}' could not be loaded. Check Build Settings.");
        }

        _loadOperation = SceneManager.LoadSceneAsync(_sceneName);

        if (_loadOperation == null)
        {
            throw new InvalidOperationException($"{nameof(SceneLoadStep)}: scene '{_sceneName}' could not be loaded. Check Build Settings.");
        }

        _loadOperation.allowSceneActivation = false;

        while (_loadOperation.progress < SceneReadyProgress)
        {
            progress.Report(_loadOperation.progress / SceneReadyProgress);

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    public void Activate()
    {
        if (_loadOperation == null)
        {
            return;
        }

        _loadOperation.allowSceneActivation = true;
    }
}
