using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownloadDetailUI : UIBase
{
    private const float MinimumFillSeconds = 1f;
    private const float MaxFillSpeed = 1f / MinimumFillSeconds;
    private const float FillSmoothTime = 0.4f;
    private const float FillCompleteThreshold = 0.999f;
    private const string PercentTextFormat = "{0}%";

    private const string SizeTextFormat = "{0:1}MB / {1:1}MB";
    private const string SpeedTextFormat = "{0:1}MB/s";
    private const float BytesPerMegabyte = 1024f * 1024f;
    private const float SpeedSampleSeconds = 0.5f;
    private const float HoldSeconds = 0.3f;
    private const float FadeSeconds = 0.2f;

    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TMP_Text _percentText;
    [SerializeField] private TMP_Text _sizeText;
    [SerializeField] private TMP_Text _speedText;
    [SerializeField] private CanvasGroup _canvasGroup;

    private bool _isRunning;
    private bool _isFinishing;
    private DownloadStatus _lastDownloadStatus;

    private Sequence _finishSequence;
    private Tween _fadeTween;

    private float _displayedProgress;
    private float _fillVelocity;
    private int _displayedPercent = -1;

    private int _displayedDownloadedTenths = -1;
    private int _displayedTotalTenths = -1;

    private long _speedSampleBytes;
    private float _speedSampleElapsed;

    public bool IsHidden => !gameObject.activeSelf;

    public bool HasAllReferences()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_progressSlider, nameof(_progressSlider), this);
        hasAllReferences &= UnityUtility.CheckReference(_percentText, nameof(_percentText), this);
        hasAllReferences &= UnityUtility.CheckReference(_sizeText, nameof(_sizeText), this);
        hasAllReferences &= UnityUtility.CheckReference(_speedText, nameof(_speedText), this);
        hasAllReferences &= UnityUtility.CheckReference(_canvasGroup, nameof(_canvasGroup), this);

        return hasAllReferences;
    }

    public void Run(ILoadingStatus status, CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            return;
        }

        RunAsync(status, cancellationToken).Forget();
    }

    public override void Hide()
    {
        KillTweens();
        _isFinishing = false;
        base.Hide();
    }

    private async UniTask RunAsync(ILoadingStatus status, CancellationToken cancellationToken)
    {
        _isRunning = true;

        try
        {
            do
            {
                Refresh(status);

                bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow();

                if (isCanceled)
                {
                    return;
                }
            }
            while (!IsHidden);
        }
        finally
        {
            _isRunning = false;
        }
    }

    private void Refresh(ILoadingStatus status)
    {
        if (status.TryGetCurrentDownload(out DownloadStatus downloadStatus))
        {
            ShowProgress(downloadStatus);
            return;
        }

        if (!IsHidden)
        {
            Finish();
        }
    }

    private void ShowProgress(DownloadStatus downloadStatus)
    {
        if (IsHidden || _isFinishing)
        {
            Begin(downloadStatus);
        }

        _lastDownloadStatus = downloadStatus;

        AdvanceDisplayedProgress((float)((double)downloadStatus.DownloadedBytes / downloadStatus.TotalBytes));
        UpdateProgressBar(_displayedProgress);

        UpdateSizeText(downloadStatus.DownloadedBytes, downloadStatus.TotalBytes);
        UpdateSpeedText(downloadStatus.DownloadedBytes);
    }

    private void Begin(DownloadStatus downloadStatus)
    {
        bool wasHidden = IsHidden;

        KillTweens();
        _isFinishing = false;
        Show();

        if (wasHidden)
        {
            _canvasGroup.alpha = 0f;
        }

        _fadeTween = _canvasGroup.DOFade(1f, FadeSeconds).SetUpdate(true).SetLink(gameObject);

        _displayedProgress = 0f;
        _fillVelocity = 0f;
        UpdateProgressBar(0f);

        ResetSpeedSample(downloadStatus);
    }

    private void Finish()
    {
        if (_isFinishing)
        {
            return;
        }

        _isFinishing = true;
        UpdateSizeText(_lastDownloadStatus.TotalBytes, _lastDownloadStatus.TotalBytes);

        float fillSeconds = (1f - _displayedProgress) / MaxFillSpeed;

        _finishSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        _finishSequence.Append(DOTween.To(GetDisplayedProgress, SetDisplayedProgress, 1f, fillSeconds).SetEase(Ease.OutQuad));
        _finishSequence.AppendInterval(HoldSeconds);
        _finishSequence.Append(_canvasGroup.DOFade(0f, FadeSeconds));
        _finishSequence.AppendCallback(OnFinishCompleted);
    }

    private float GetDisplayedProgress()
    {
        return _displayedProgress;
    }

    private void SetDisplayedProgress(float progress)
    {
        _displayedProgress = progress;
        UpdateProgressBar(progress);
    }

    private void OnFinishCompleted()
    {
        _finishSequence = null;
        Hide();
    }

    private void KillTweens()
    {
        _finishSequence?.Kill();
        _finishSequence = null;

        _fadeTween?.Kill();
        _fadeTween = null;
    }

    private void AdvanceDisplayedProgress(float actualProgress)
    {
        float smoothedProgress = Mathf.SmoothDamp(_displayedProgress, actualProgress, ref _fillVelocity, FillSmoothTime, MaxFillSpeed, Time.unscaledDeltaTime);

        _displayedProgress = Mathf.Max(_displayedProgress, smoothedProgress);

        if (actualProgress >= 1f && _displayedProgress >= FillCompleteThreshold)
        {
            _displayedProgress = 1f;
        }
    }

    private void UpdateProgressBar(float progress)
    {
        _progressSlider.value = progress;

        int percent = Mathf.RoundToInt(progress * 100f);

        if (percent == _displayedPercent)
        {
            return;
        }

        _displayedPercent = percent;
        _percentText.SetText(PercentTextFormat, percent);
    }

    private void UpdateSizeText(long downloadedBytes, long totalBytes)
    {
        int downloadedTenths = Mathf.RoundToInt(downloadedBytes / BytesPerMegabyte * 10f);
        int totalTenths = Mathf.RoundToInt(totalBytes / BytesPerMegabyte * 10f);

        if (downloadedTenths == _displayedDownloadedTenths && totalTenths == _displayedTotalTenths)
        {
            return;
        }

        _displayedDownloadedTenths = downloadedTenths;
        _displayedTotalTenths = totalTenths;
        _sizeText.SetText(SizeTextFormat, downloadedTenths / 10f, totalTenths / 10f);
    }

    private void ResetSpeedSample(DownloadStatus downloadStatus)
    {
        _speedSampleBytes = downloadStatus.DownloadedBytes;
        _speedSampleElapsed = 0f;
        _speedText.SetText(SpeedTextFormat, 0f);
    }

    private void UpdateSpeedText(long downloadedBytes)
    {
        _speedSampleElapsed += Time.unscaledDeltaTime;

        if (_speedSampleElapsed < SpeedSampleSeconds)
        {
            return;
        }

        float bytesPerSecond = (downloadedBytes - _speedSampleBytes) / _speedSampleElapsed;
        _speedText.SetText(SpeedTextFormat, bytesPerSecond / BytesPerMegabyte);

        _speedSampleBytes = downloadedBytes;
        _speedSampleElapsed = 0f;
    }
}
