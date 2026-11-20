using System.Collections.Generic;

public static class LoadFailureMessages
{
    private const string RetryMessage = "Loading failed.\nPlease check your connection and try again.";
    private const string ReturnToPreviousMessage = "Unable to continue loading.\nPlease try again later.";
    private const string GoToTitleMessage = "Your session has expired.\nPlease log in again.";
    private const string QuitMessage = "A problem occurred.\nThe game will now close.";

    private static readonly Dictionary<string, string> MessagesByErrorCode = new Dictionary<string, string>
    {
        { LoadErrorCodes.Timeout, "The server is not responding.\nPlease try again." },
        { LoadErrorCodes.DownloadDeclined, "Additional resources are required to play.\nThe game will now close." },
    };

    public static string GetMessage(LoadFailure failure)
    {
        if (failure.ErrorCode != null && MessagesByErrorCode.TryGetValue(failure.ErrorCode, out string message))
        {
            return message;
        }

        switch (failure.Action)
        {
            case LoadFailureAction.Retry:
                return RetryMessage;
            case LoadFailureAction.GoToTitle:
                return GoToTitleMessage;
            case LoadFailureAction.Quit:
                return QuitMessage;
            default:
                return ReturnToPreviousMessage;
        }
    }
}
