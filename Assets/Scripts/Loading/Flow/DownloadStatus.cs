public readonly struct DownloadStatus
{
    public DownloadStatus(long downloadedBytes, long totalBytes)
    {
        DownloadedBytes = downloadedBytes;
        TotalBytes = totalBytes;
    }

    public long DownloadedBytes { get; }

    public long TotalBytes { get; }
}
