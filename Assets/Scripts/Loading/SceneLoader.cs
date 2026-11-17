using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string LoadingSceneName = "Loading";

    public static string TargetSceneName { get; private set; }

    public static string PreviousSceneName { get; private set; }

    public static IReadOnlyList<ILoadStep> Steps { get; private set; } = Array.Empty<ILoadStep>();

    public static bool IsLoading { get; private set; }

    public static void Load(string sceneName, params ILoadStep[] steps)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            DebugUtility.LogWarning($"{nameof(SceneLoader)}: scene name is empty. Ignored.");
            return;
        }

        if (IsLoading)
        {
            DebugUtility.LogWarning($"{nameof(SceneLoader)}: already loading '{TargetSceneName}'. Ignored request for '{sceneName}'.");
            return;
        }

        IsLoading = true;
        TargetSceneName = sceneName;
        PreviousSceneName = SceneManager.GetActiveScene().name;

        if (steps == null)
        {
            Steps = Array.Empty<ILoadStep>();
        }
        else
        {
            Steps = steps;
        }

        SceneManager.LoadScene(LoadingSceneName);
    }

    public static void Complete()
    {
        Clear();
    }

    public static void ReturnToPreviousScene()
    {
        string previousSceneName = PreviousSceneName;

        Clear();

        if (string.IsNullOrWhiteSpace(previousSceneName))
        {
            DebugUtility.LogWarning($"{nameof(SceneLoader)}: no previous scene to return to.");
            return;
        }

        SceneManager.LoadScene(previousSceneName);
    }

    private static void Clear()
    {
        IsLoading = false;
        TargetSceneName = null;
        PreviousSceneName = null;
        Steps = Array.Empty<ILoadStep>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        Clear();
    }
}
