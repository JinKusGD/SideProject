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
    private const string InsertAccountQuery = "INSERT INTO accounts (account_id, account_password) VALUES (@id, @password)";

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
            using MySqlCommand selectAccountUidCommand = new MySqlCommand(SelectAccountUidQuery, mySqlConnection);

            selectAccountUidCommand.Parameters.AddWithValue("@id", id);
            selectAccountUidCommand.Parameters.AddWithValue("@password", password);

            object queryResult = await selectAccountUidCommand.ExecuteScalarAsync(cancellationToken);

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

    public async UniTask<DBSignUpResult> SignUpAsync(string id, string password, CancellationToken cancellationToken)
    {
        using MySqlConnection mySqlConnection = new MySqlConnection(ConnectionString);

        DBConnectionResult dbConnectionResult = await ConnectDatabaseAsync(mySqlConnection, cancellationToken);

        if (dbConnectionResult != DBConnectionResult.Success)
        {
            return DBSignUpResult.ServerUnavailable;
        }

        try
        {
            using MySqlCommand insertAccountCommand = new MySqlCommand(InsertAccountQuery, mySqlConnection);

            insertAccountCommand.Parameters.AddWithValue("@id", id);
            insertAccountCommand.Parameters.AddWithValue("@password", password);

            int insertedRowCount = await insertAccountCommand.ExecuteNonQueryAsync(cancellationToken);

            return insertedRowCount > 0 ? DBSignUpResult.Success : DBSignUpResult.UnknownError;
        }
        catch (MySqlException exception)
        {
            bool isDuplicateId = exception.ErrorCode == MySqlErrorCode.DuplicateKeyEntry;

            if (isDuplicateId)
            {
                // account_id가 기본 키라서 이미 있는 ID면 INSERT가 중복 키 에러로 실패한다.
                return DBSignUpResult.AccountAlreadyExists;
            }

            DebugUtility.LogWarning(exception.ToString());

            bool isCommandTimeoutExpired = exception.ErrorCode == MySqlErrorCode.CommandTimeoutExpired;

            return isCommandTimeoutExpired ? DBSignUpResult.Timeout : DBSignUpResult.UnknownError;
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