using System;

public readonly struct LoadFailure
{
    private LoadFailure(string stepName, string errorCode, LoadFailureAction action)
    {
        StepName = stepName;
        ErrorCode = errorCode;
        Action = action;
    }

    public string StepName { get; }

    public string ErrorCode { get; }

    public LoadFailureAction Action { get; }

    public bool IsRetryable => Action == LoadFailureAction.Retry;

    public static LoadFailure FromException(Exception exception, string stepName)
    {
        if (exception is LoadStepException loadStepException)
        {
            return new LoadFailure(stepName, loadStepException.ErrorCode, loadStepException.Action);
        }

        return new LoadFailure(stepName, LoadErrorCodes.Unknown, LoadFailureAction.Retry);
    }
}
