using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly string connectionString;
    private readonly ILogger<AccountController> logger;

    private const string InsertAccountQuery = "INSERT INTO accounts (account_id, account_password) VALUES (@id, @password)";
    private const string SelectAccountUidQuery = "SELECT account_uid, account_password FROM accounts WHERE account_id = @id";

    public AccountController(IConfiguration config, ILogger<AccountController> logger)
    {
        connectionString = config.GetConnectionString("Server") ?? throw new InvalidOperationException("ConnectionStrings:Server is missing.");
        this.logger = logger;
    }

    [HttpPost("signUp")]
    public async Task<IActionResult> SignUp(AccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest();
        }

        await using MySqlConnection mySqlConnection = new MySqlConnection(connectionString);
        DBConnectionResult dbConnectionResult = await ConnectDatabaseAsync(mySqlConnection);

        if (dbConnectionResult != DBConnectionResult.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            await using MySqlCommand insertAccountCommand = new MySqlCommand(InsertAccountQuery, mySqlConnection);

            insertAccountCommand.Parameters.AddWithValue("@id", request.Id);
            insertAccountCommand.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(request.Password));

            int insertedRowCount = await insertAccountCommand.ExecuteNonQueryAsync();

            return insertedRowCount > 0 ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (MySqlException exception)
        {
            bool isDuplicateId = exception.ErrorCode == MySqlErrorCode.DuplicateKeyEntry;

            if (isDuplicateId)
            {
                return Conflict();
            }

            logger.LogWarning(exception, $"SignUp failed. Id: {request.Id}");

            bool isCommandTimeoutExpired = exception.ErrorCode == MySqlErrorCode.CommandTimeoutExpired;

            return isCommandTimeoutExpired ? StatusCode(StatusCodes.Status504GatewayTimeout) : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(AccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest();
        }

        await using MySqlConnection mySqlConnection = new MySqlConnection(connectionString);

        DBConnectionResult dbConnectionResult = await ConnectDatabaseAsync(mySqlConnection);

        if (dbConnectionResult != DBConnectionResult.Success)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            await using MySqlCommand selectAccountUidCommand = new MySqlCommand(SelectAccountUidQuery, mySqlConnection);

            selectAccountUidCommand.Parameters.AddWithValue("@id", request.Id);

            await using MySqlDataReader reader = await selectAccountUidCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return Unauthorized();
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, reader.GetString(1)))
            {
                return Unauthorized();
            }

            return Ok(new LoginResponse { UId = reader.GetUInt32(0) });
        }
        catch (MySqlException exception)
        {
            logger.LogWarning(exception, $"Login failed. Id: {request.Id}");

            bool isCommandTimeoutExpired = exception.ErrorCode == MySqlErrorCode.CommandTimeoutExpired;

            return isCommandTimeoutExpired ? StatusCode(StatusCodes.Status504GatewayTimeout) : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
 
    private async Task<DBConnectionResult> ConnectDatabaseAsync(MySqlConnection mySqlConnection)
    {
        try
        {
            await mySqlConnection.OpenAsync();

            return DBConnectionResult.Success;
        }
        catch (MySqlException exception)
        {
            logger.LogWarning(exception, "Database connection failed.");

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

public enum DBConnectionResult
{
    UnknownError,
    Success,
    ConnectionFailed,
    AuthenticationFailed,
    DatabaseNotFound,
    TooManyConnections
}

public class AccountRequest
{
    public string Id { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public uint UId { get; set; }
}