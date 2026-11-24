public enum DBLoginResult
{
    UnknownError,
    Success,
    ServerUnavailable,
    AccountNotFound,
    Timeout
}

public enum DBSignUpResult
{
    UnknownError,
    Success,
    ServerUnavailable,
    AccountAlreadyExists,
    Timeout
}
