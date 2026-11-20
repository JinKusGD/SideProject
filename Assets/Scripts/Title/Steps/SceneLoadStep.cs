using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoadStep : ILoadStep
{
    private const string SceneStepName = "Scene";
    private const float SceneReadyProgress = 0.9f;

    private readonly string _sceneName;

    private AsyncOperation _loadOperation;

    public SceneLoadStep(string sceneName)
    {
        _sceneName = sceneName;
    }

    public string Name { get; } = SceneStepName;

    public TimeSpan Timeout { get; } = TimeSpan.Zero;

    public async UniTask ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_loadOperation == null)
        {
            StartLoad();
        }

        while (_loadOperation.progress < SceneReadyProgress)
        {
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

    private void StartLoad()
    {
        if (string.IsNullOrWhiteSpace(_sceneName) || !Application.CanStreamedLevelBeLoaded(_sceneName))
        {
            throw CreateSceneNotFoundException();
        }

        _loadOperation = SceneManager.LoadSceneAsync(_sceneName);

        if (_loadOperation == null)
        {
            throw CreateSceneNotFoundException();
        }

        _loadOperation.allowSceneActivation = false;
    }

    private LoadStepException CreateSceneNotFoundException()
    {
        return new LoadStepException(LoadErrorCodes.SceneNotFound, LoadFailureAction.ReturnToPrevious, $"{nameof(SceneLoadStep)}: scene '{_sceneName}' could not be loaded. Check Build Settings.");
    }
}
