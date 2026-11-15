using Cysharp.Threading.Tasks;
using MySqlConnector;
using System;
using System.Threading;

public readonly struct DBLoginPacket
{
    public readonly DBLoginResult DBLoginResult;
    public readonly uint AccountUid;

    public DBLoginPacket(DBLoginResult dbLoginResult, uint accountUid)
    {
        DBLoginResult = dbLoginResult;
        AccountUid = accountUid;
    }
}

public class DatabaseService
{
    private static readonly string ConnectionString = $"Server={DBData.Server};Port={DBData.Port};Database={DBData.Database};UserID={DBData.UserID};Password={DBData.Password};ConnectionTimeout={DBData.ConnectionTimeout};";

    private const string SelectAccountUidQuery = "SELECT account_uid FROM accounts WHERE account_id = @id AND account_password = @password";
    
    public async UniTask<DBLoginPacket> LoginAsync(string id, string password, CancellationToken cancellationToken)
    {
        uint accountUid = uint.MinValue;

        using MySqlConnection mySqlConnection = new MySqlConnection(ConnectionString);

        DBConnectionResult dbConnectionResult = await ConnectDatabaseAsync(mySqlConnection, cancellationToken);

        if (dbConnectionResult != DBConnectionResult.Success)
        {
            return new DBLoginPacket(DBLoginResult.ServerUnavailable, accountUid);
        }

        try
        {
            using MySqlCommand mySqlCommand = new MySqlCommand(SelectAccountUidQuery, mySqlConnection);

            mySqlCommand.Parameters.AddWithValue("@id", id);
            mySqlCommand.Parameters.AddWithValue("@password", password);

            object queryResult = await mySqlCommand.ExecuteScalarAsync(cancellationToken);

            if (queryResult == null)
            {
                return new DBLoginPacket(DBLoginResult.AccountNotFound, accountUid);
            }

            accountUid = Convert.ToUInt32(queryResult);

            return new DBLoginPacket(DBLoginResult.Success, accountUid);
        }
        catch (MySqlException exception)
        {
            DebugUtility.LogWarning(exception.ToString());

            bool isCommandTimeoutExpired = exception.ErrorCode == MySqlErrorCode.CommandTimeoutExpired;

            DBLoginResult dbLoginResult = isCommandTimeoutExpired ? DBLoginResult.Timeout : DBLoginResult.UnknownError;

            return new DBLoginPacket(dbLoginResult, accountUid);
        }
    }

    private async UniTask<DBConnectionResult> ConnectDatabaseAsync(MySqlConnection mySqlConnection, CancellationToken cancellationToken)
    {
        try
        {
            await mySqlConnection.OpenAsync(cancellationToken);

            return DBConnectionResult.Success;
        }
        catch (MySqlException exception)
        {
            DebugUtility.LogWarning(exception.ToString());

            switch (exception.ErrorCode)
            {
                case MySqlErrorCode.UnableToConnectToHost:
                    return DBConnectionResult.ConnectionFailed;
                case MySqlErrorCode.AccessDenied:
                case MySqlErrorCode.DatabaseAccessDenied:
                    return DBConnectionResult.AuthenticationFailed;
                case MySqlErrorCode.UnknownDatabase:
                    return DBConnectionResult.DatabaseNotFound;
                case MySqlErrorCode.ConnectionCountError:
                case MySqlErrorCode.TooManyUserConnections:
                    return DBConnectionResult.TooManyConnections;
                default:
                    return DBConnectionResult.UnknownError;
            }
        }
    }
}