public enum DBConnectionResult
{
    UnknownError,
    Success,
    ConnectionFailed,
    AuthenticationFailed,
    DatabaseNotFound,
    TooManyConnections
}

public enum DBLoginResult
{
    UnknownError,
    Success,
    ServerUnavailable,
    AccountNotFound,
    Timeout
}