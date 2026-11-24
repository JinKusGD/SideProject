public readonly struct LoadResult
{
    private LoadResult(LoadResultStatus status, LoadFailure failure, long downloadBytes)
    {
        Status = status;
        Failure = failure;
        DownloadBytes = downloadBytes;
    }

    public static LoadResult Succeeded => new LoadResult(LoadResultStatus.Succeeded, default, 0);

    public static LoadResult Canceled => new LoadResult(LoadResultStatus.Canceled, default, 0);

    public static LoadResult Redirected => new LoadResult(LoadResultStatus.Redirected, default, 0);

    public LoadResultStatus Status { get; }

    // Status가 Failed일 때만 의미가 있다.
    public LoadFailure Failure { get; }

    // Status가 DownloadConfirmationRequired일 때만 의미가 있다.
    public long DownloadBytes { get; }

    public static LoadResult Failed(LoadFailure failure)
    {
        return new LoadResult(LoadResultStatus.Failed, failure, 0);
    }

    public static LoadResult DownloadConfirmationRequired(long downloadBytes)
    {
        return new LoadResult(LoadResultStatus.DownloadConfirmationRequired, default, downloadBytes);
    }
}
