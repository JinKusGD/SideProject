using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ILoadProgress
{
    float Value { get; }
}

public sealed class SceneLoadProgress : ILoadProgress
{
    private const float SceneReadyProgress = 0.9f;

    private readonly AsyncOperation _loadOperation;

    public SceneLoadProgress(AsyncOperation loadOperation)
    {
        _loadOperation = loadOperation;
    }

    public float Value
    {
        get
        {
            return Mathf.Clamp01(_loadOperation.progress / SceneReadyProgress);
        }
    }
}

public sealed class UniTaskLoadProgress : ILoadProgress
{
    private const float IncompleteProgressCap = 0.99f;

    private readonly UniTask _uniTask;

    public UniTaskLoadProgress(UniTask uniTask)
    {
        _uniTask = uniTask;
    }

    public float Value
    {
        get
        {
            bool isWorkCompleted = _uniTask.Status.IsCompleted();

            return isWorkCompleted ? 1f : IncompleteProgressCap;
        }
    }
}
