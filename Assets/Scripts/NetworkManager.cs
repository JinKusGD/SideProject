using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    private readonly DatabaseService _databaseService = new DatabaseService();

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

    public bool TestDataBase(string id, string password)
    {
        DatabaseResult databaseResult = _databaseService.TryGetAccountUid(id, password, out uint uid);

        switch (databaseResult)
        {
            case DatabaseResult.Error:
                Debug.Log("실패");
                return false;
            case DatabaseResult.NotFound:
                Debug.Log("없음");
                return false;
            case DatabaseResult.Success:
                Debug.Log("성공");
                return true;
        }

        return false;
    }
}
