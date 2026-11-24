public interface ILoadingStatus
{
    string CurrentStepName { get; }

    string CurrentStepMessageKey { get; }

    bool IsRunning { get; }

    bool TryGetCurrentDownload(out DownloadStatus status);
}
