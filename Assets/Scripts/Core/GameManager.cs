using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            // 로딩 실패 후 Boot로 되돌아온 경우. 새로 생긴 쪽은 지우고 기존 매니저가 진입 흐름을 다시 시작한다.
            Destroy(gameObject);
            Instance.EnterTitle();
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnterTitle();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void EnterTitle()
    {
        SceneTransition.Start(new TitleStartupLoadPlan());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }
}
