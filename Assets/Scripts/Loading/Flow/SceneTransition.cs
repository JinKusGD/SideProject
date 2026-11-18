using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransition
{
    public static ILoadPlan CurrentPlan { get; private set; }

    public static string PreviousSceneName { get; private set; }

    public static bool IsInProgress => CurrentPlan != null;

    public static void Start(ILoadPlan plan)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (IsInProgress)
        {
            DebugUtility.LogWarning($"{nameof(SceneTransition)}: transition to '{CurrentPlan.TargetSceneName}' already in progress. Ignored request for '{plan.TargetSceneName}'.");
            return;
        }

        if (!CanLoadScene(plan.LoadingSceneName))
        {
            DebugUtility.LogError($"{nameof(SceneTransition)}: loading scene '{plan.LoadingSceneName}' could not be loaded. Check Build Settings.");
            return;
        }

        if (!CanLoadScene(plan.TargetSceneName))
        {
            DebugUtility.LogError($"{nameof(SceneTransition)}: target scene '{plan.TargetSceneName}' could not be loaded. Check Build Settings.");
            return;
        }

        CurrentPlan = plan;

        Scene currentScene = SceneManager.GetActiveScene();
        PreviousSceneName = currentScene.name;

        SceneManager.LoadScene(plan.LoadingSceneName);
    }

    public static void Complete()
    {
        Clear();
    }

    public static void Cancel()
    {
        Clear();
    }

    public static void FallBackTo(string sceneName)
    {
        Clear();

        if (!CanLoadScene(sceneName))
        {
            DebugUtility.LogError($"{nameof(SceneTransition)}: scene '{sceneName}' could not be loaded. Check Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public static void ReturnToPreviousScene()
    {
        FallBackTo(PreviousSceneName);
    }

    private static bool CanLoadScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
    }

    private static void Clear()
    {
        CurrentPlan = null;
        PreviousSceneName = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        Clear();
    }
}
