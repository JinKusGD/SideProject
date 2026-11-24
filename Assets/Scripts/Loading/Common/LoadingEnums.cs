public enum LoadResultStatus
{
    Succeeded,
    Failed,
    DownloadConfirmationRequired,
    Canceled,
    Redirected
}

public enum LoadFailureAction
{
    Retry,
    ReturnToPrevious,
    GoToTitle,
    Quit
}

public enum LoadingFailureChoice
{
    Retry,
    GiveUp
}
