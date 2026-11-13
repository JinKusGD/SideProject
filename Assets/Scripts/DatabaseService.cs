using Cysharp.Threading.Tasks;
using MySqlConnector;
using System;
using System.Threading;

public enum DatabaseResult
{
    Error,
    NotFound,
    Success
}

public class DatabaseLoginPacket
{
    public readonly DatabaseResult DatabaseResult;
    public readonly uint ResultUid;

    public DatabaseLoginPacket(DatabaseResult databaseResult, uint uid)
    {
        DatabaseResult = databaseResult;
        ResultUid = uid;
    }
}

public class DatabaseService
{
    private static readonly string ConnectionString = $"Server={DBData.Server};Port={DBData.Port};Database={DBData.Database};UserID={DBData.UserID};Password={DBData.Password};ConnectionTimeout={DBData.ConnectionTimeout};";

    private const string FindAccountQuery = "SELECT account_uid FROM accounts WHERE account_id = @id AND account_password = @password";

    public async UniTask<DatabaseLoginPacket> GetAccountUidAsync(string id, string password, CancellationToken cancellationToken)
    {
        try
        {
            using MySqlConnection mySqlConnection = new MySqlConnection(ConnectionString);

            await mySqlConnection.OpenAsync(cancellationToken);

            using MySqlCommand mySqlCommand = new MySqlCommand(FindAccountQuery, mySqlConnection);

            mySqlCommand.Parameters.AddWithValue("@id", id);
            mySqlCommand.Parameters.AddWithValue("@password", password);

            object result = await mySqlCommand.ExecuteScalarAsync(cancellationToken);

            if (result == null)
            {
                return new DatabaseLoginPacket(DatabaseResult.NotFound, uint.MinValue);
            }

            return new DatabaseLoginPacket(DatabaseResult.Success, Convert.ToUInt32(result));
        }
        catch (MySqlException e)
        {
            DebugUtility.LogWarning(e.ToString());
            return new DatabaseLoginPacket(DatabaseResult.Error, uint.MinValue);
        }
    }
}
