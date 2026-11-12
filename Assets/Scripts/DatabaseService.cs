using MySqlConnector;
using System;

public enum DatabaseResult
{
    Error,
    NotFound,
    Success
}

public class DatabaseService
{
    private readonly string _connectionString = $"Server={DBData.Server};Port={DBData.Port};Database={DBData.Database};UserID={DBData.UserID};Password={DBData.Password};";

    private readonly string _findAccountQuery = "SELECT account_uid FROM accounts WHERE account_id = @id AND account_password = @password";

    public DatabaseResult TryGetAccountUid(string id, string password, out uint uid)
    {
        uid = uint.MinValue;

        try
        {
            using MySqlConnection mySqlConnection = new MySqlConnection(_connectionString);

            mySqlConnection.Open();

            using MySqlCommand mySqlCommand = new MySqlCommand(_findAccountQuery, mySqlConnection);

            mySqlCommand.Parameters.AddWithValue("@id", id);
            mySqlCommand.Parameters.AddWithValue("@password", password);

            object result = mySqlCommand.ExecuteScalar();

            if (result == null)
            {
                return DatabaseResult.NotFound;
            }

            uid = Convert.ToUInt32(result);

            return DatabaseResult.Success;
        }
        catch (MySqlException e)
        {
            return DatabaseResult.Error;
        }
    }
}
