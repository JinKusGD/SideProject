using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            Instance.EnterTitle();
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LocalizationManager.Initialize();
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
