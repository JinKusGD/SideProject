using System;

public sealed class LoadStepException : Exception
{
    public LoadStepException(string errorCode, LoadFailureAction action, string message) : base(message)
    {
        ErrorCode = errorCode;
        Action = action;
    }

    public LoadStepException(string errorCode, LoadFailureAction action, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
        Action = action;
    }

    public string ErrorCode { get; }

    public LoadFailureAction Action { get; }
}
