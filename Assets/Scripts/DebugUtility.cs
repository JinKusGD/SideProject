using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class DebugUtility
{
    private const string EditorSymbol = "UNITY_EDITOR";
    private const string DevelopmentBuildSymbol = "DEVELOPMENT_BUILD";

    [Conditional(EditorSymbol), Conditional(DevelopmentBuildSymbol)]
    public static void Log(string message)
    {
        Debug.Log(message);
    }

    [Conditional(EditorSymbol), Conditional(DevelopmentBuildSymbol)]
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    public static void LogError(string message)
    {
        Debug.LogError(message);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterQuitOnError()
    {
        Application.logMessageReceived -= QuitOnError;
        Application.logMessageReceived += QuitOnError;
    }

    private static void QuitOnError(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
        {
            return;
        }

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#else
        Application.Quit();
#endif
    }
}