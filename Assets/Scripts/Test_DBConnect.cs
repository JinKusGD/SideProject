using MySqlConnector;
using UnityEngine;

public class Test_DBConnect : MonoBehaviour
{
    private readonly string connectionString = $"Server={DBData.Server};Port={DBData.Port};Database={DBData.Database};UserID={DBData.UserID};Password={DBData.Password};";

    private string testId = "newId";
    private string testpassword = "newPassword";

    void Start()
    {
        try
        {
            using MySqlConnection connection = new MySqlConnection(connectionString);

            connection.Open();

            Debug.Log("MySQL 연결 성공!");

            InsertAccount(connection);
        }
        catch (MySqlException e)
        {
            Debug.LogError($"MySQL 연결 실패: {e.Message}");
        }
    }

    private void SelectAllAccountData(MySqlConnection connection)
    {
        string selectAcountQuery = "SELECT * FROM accounts";

        using MySqlCommand command = new MySqlCommand(selectAcountQuery, connection);

        MySqlDataReader mySqlDataReader = command.ExecuteReader();

        while (mySqlDataReader.Read())
        {
            string id = mySqlDataReader.GetString("account_id");
            string password = mySqlDataReader.GetString("account_password");

            Debug.Log($"{id}, {password}");
        }
    }

    private void InsertAccount(MySqlConnection connection)
    {
        string insertAccountQuery = "INSERT INTO accounts (account_id, account_password) VALUES (@id, @password) ";

        using MySqlCommand command = new MySqlCommand(insertAccountQuery, connection);

        command.Parameters.AddWithValue("@id", testId);
        command.Parameters.AddWithValue("@password", testpassword);

        int rowResult = command.ExecuteNonQuery();

        if (rowResult < 1)     
        {
            Debug.Log($"{testId} 회원가입 실패");
            return;
        }

        Debug.Log($"{testId} 회원가입 성공");
    }
}
