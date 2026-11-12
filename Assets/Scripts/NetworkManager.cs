using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"Duplicate networkManager instance detected.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


}
