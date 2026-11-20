using System.Threading;
using Cysharp.Threading.Tasks;

public interface IDownloadStep : ILoadStep
{
    long DownloadedBytes { get; }

    long TotalBytes { get; }

    UniTask<long> GetDownloadSizeAsync(CancellationToken cancellationToken);
}
