public interface ILoadingStatus
{
    string CurrentStepName { get; }

    bool IsRunning { get; }

    bool TryGetCurrentDownload(out DownloadStatus status);
}
