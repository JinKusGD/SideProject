using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class DownloadStep : IDownloadStep
{
    private const float SpeedVariation = 0.35f;
    private const float SpeedNoiseFrequency = 0.8f;

    private readonly double _averageBytesPerSecond;

    private double _downloadedBytes;

    public DownloadStep(string name, long totalBytes, float durationSeconds)
    {
        Name = name;
        TotalBytes = totalBytes;
        _averageBytesPerSecond = totalBytes / Math.Max(durationSeconds, 0.01f);
    }

    public string Name { get; }

    public TimeSpan Timeout { get; } = TimeSpan.Zero;

    public long TotalBytes { get; }

    public long DownloadedBytes => (long)_downloadedBytes;

    public UniTask<long> GetDownloadSizeAsync(CancellationToken cancellationToken)
    {
        return UniTask.FromResult(TotalBytes - DownloadedBytes);
    }

    public async UniTask ExecuteAsync(CancellationToken cancellationToken)
    {
        float elapsed = 0f;

        while (_downloadedBytes < TotalBytes)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;

            float noise = Mathf.PerlinNoise(elapsed * SpeedNoiseFrequency, 0f) * 2f - 1f;
            double bytesPerSecond = _averageBytesPerSecond * (1f + noise * SpeedVariation);

            _downloadedBytes = Math.Min(TotalBytes, _downloadedBytes + bytesPerSecond * deltaTime);
        }
    }
}
