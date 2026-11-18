using UnityEngine;

public static class ApplicationUtility
{
    // 에디터에서는 플레이 모드를 멈추고, 빌드에서는 앱을 종료한다.
    public static void Quit()
    {
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
