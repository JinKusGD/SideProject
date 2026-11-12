using UnityEngine;
using MySqlConnector;

public class Test_DBConnect : MonoBehaviour
{
    private readonly string connectionString = $"Server={DBData.Server};Port={DBData.Port};Database={DBData.Database};UserID={DBData.UserID};Password={DBData.Password};";

    void Start()
    {
        try
        {
            using MySqlConnection connection = new MySqlConnection(connectionString);

            connection.Open();

            Debug.Log("MySQL 연결 성공!");
        }
        catch (MySqlException e)
        {
            Debug.LogError($"MySQL 연결 실패: {e.Message}");
        }
    }
}
