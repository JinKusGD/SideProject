using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressablesDownloadStep : IDownloadStep
{
    private readonly string _label;

    private AsyncOperationHandle _downloadHandle;
    private long _downloadedBytes;

    public AddressablesDownloadStep(string name, string messageKey, string label)
    {
        Name = name;
        MessageKey = messageKey;
        _label = label;
    }

    public string Name { get; }

    public string MessageKey { get; }

    public TimeSpan Timeout { get; } = TimeSpan.Zero;

    public long TotalBytes { get; private set; }

    public long DownloadedBytes
    {
        get
        {
            if (_downloadHandle.IsValid())
            {
                return Math.Min(TotalBytes, _downloadedBytes + _downloadHandle.GetDownloadStatus().DownloadedBytes);
            }

            return _downloadedBytes;
        }
    }

    public async UniTask<long> GetDownloadSizeAsync(CancellationToken cancellationToken)
    {
        TotalBytes = await GetRemainingBytesAsync(cancellationToken);
        _downloadedBytes = 0;
        return TotalBytes;
    }

    public async UniTask ExecuteAsync(CancellationToken cancellationToken)
    {
        long remainingBytes = await GetRemainingBytesAsync(cancellationToken);
        _downloadedBytes = Math.Max(0, TotalBytes - remainingBytes);

        _downloadHandle = Addressables.DownloadDependenciesAsync(_label, false);

        try
        {
            await _downloadHandle.ToUniTask(cancellationToken: cancellationToken);
            _downloadedBytes = TotalBytes;
        }
        finally
        {
            Addressables.Release(_downloadHandle);
            _downloadHandle = default;
        }
    }

    private async UniTask<long> GetRemainingBytesAsync(CancellationToken cancellationToken)
    {
        AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(_label);

        try
        {
            return await handle.ToUniTask(cancellationToken: cancellationToken);
        }
        finally
        {
            Addressables.Release(handle);
        }
    }
}
