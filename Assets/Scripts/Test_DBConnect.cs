using MySqlConnector;
using UnityEngine;

public class Test_DBConnect : MonoBehaviour
{
    private readonly string connectionString = $"Server={DBData.Server};Port={DBData.Port};Database={DBData.Database};UserID={DBData.UserID};Password={DBData.Password};";

    private string _testId = "newId";
    private string _testPassword = "newPassword";

    private string _testNewPassword = "updatePassword";

    void Start()
    {
        try
        {
            using MySqlConnection connection = new MySqlConnection(connectionString);

            connection.Open();

            Debug.Log("MySQL 연결 성공!");

            UpdateAccountPassword(connection);
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
        string insertAccountQuery = "INSERT INTO accounts (account_id, account_password) VALUES (@id, @password)";

        using MySqlCommand command = new MySqlCommand(insertAccountQuery, connection);

        command.Parameters.AddWithValue("@id", _testId);
        command.Parameters.AddWithValue("@password", _testPassword);

        int rowResult = command.ExecuteNonQuery();

        if (rowResult < 1)     
        {
            Debug.Log($"{_testId} 회원가입 실패");
            return;
        }

        Debug.Log($"{_testId} 회원가입 성공");
    }

    private void UpdateAccountPassword(MySqlConnection connection)
    {
        string UpdatePasswordQuery = "UPDATE accounts SET account_password = @newPassword WHERE account_id = @account_id";

        using MySqlCommand command = new MySqlCommand(UpdatePasswordQuery, connection);

        command.Parameters.AddWithValue("@newPassword", _testNewPassword);
        command.Parameters.AddWithValue("@account_id", _testId);

        int rowResult = command.ExecuteNonQuery();

        if (rowResult < 1)     
        {
            Debug.Log($"{_testId} 비밀번호 수정 실패");
            return;
        }

        Debug.Log($"{_testId} 비밀번호 수정 성공");
    }
}
