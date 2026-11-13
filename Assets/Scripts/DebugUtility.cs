using UnityEngine;

public static class DebugUtility
{
    public static void Log(string text)
    {
#if UNITY_EDITOR
        Debug.Log(text);
#endif
    }

    public static void LogWarning(string text)
    {
#if UNITY_EDITOR
        Debug.LogWarning(text);
#endif
    }

    public static void LogError(string text)
    {
#if UNITY_EDITOR
        Debug.LogError(text);
#endif
    }
}