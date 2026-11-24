public enum LoginResult
{
    UnknownError,
    Success,
    ServerUnavailable,
    AccountNotFound,
    Timeout
}

public enum SignUpResult
{
    UnknownError,
    Success,
    ServerUnavailable,
    AccountAlreadyExists,
    Timeout
}
