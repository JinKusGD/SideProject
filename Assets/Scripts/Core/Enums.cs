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

public enum DBSignUpResult
{
    UnknownError,
    Success,
    ServerUnavailable,
    AccountAlreadyExists,
    Timeout
}

public enum LoadingFailureChoice
{
    Retry,
    GiveUp
}

public enum LoadingSequenceState
{
    NotStarted,
    Running,
    Completed,
    Failed
}

public enum LoadFailureAction
{
    Retry,
    ReturnToPrevious,
    GoToLogin,
    Quit
}