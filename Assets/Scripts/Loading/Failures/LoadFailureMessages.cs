using System.Collections.Generic;

public static class LoadFailureMessages
{
    private static readonly Dictionary<string, string> MessageKeysByErrorCode = new Dictionary<string, string>
    {
        { LoadErrorCodes.Timeout, LocalizationKeys.UiLoadFailTimeout },
        { LoadErrorCodes.DownloadDeclined, LocalizationKeys.UiLoadFailDownloadDeclined },
    };

    public static string GetMessageKey(LoadFailure failure)
    {
        if (failure.ErrorCode != null && MessageKeysByErrorCode.TryGetValue(failure.ErrorCode, out string key))
        {
            return key;
        }

        switch (failure.Action)
        {
            case LoadFailureAction.Retry:
                return LocalizationKeys.UiLoadFailRetry;
            case LoadFailureAction.GoToTitle:
                return LocalizationKeys.UiLoadFailGoToTitle;
            case LoadFailureAction.Quit:
                return LocalizationKeys.UiLoadFailQuit;
            default:
                return LocalizationKeys.UiLoadFailReturnToPrevious;
        }
    }
}
