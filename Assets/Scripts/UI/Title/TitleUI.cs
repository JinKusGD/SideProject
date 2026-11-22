using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class TitleUI : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private StatusIndicator _statusIndicator;
    [SerializeField] private LocalizedText _statusMessageText;

    [Header("Download")]
    [SerializeField] private DownloadDetailUI _downloadDetailView;

    [Header("Popup")]
    [SerializeField] private LoginPopup _loginPopup;
    [SerializeField] private LoadingFailurePopup _failurePopup;
    [SerializeField] private DownloadConfirmPopup _downloadConfirmPopup;

    private LoadingSession _session;

    private string _displayedStepMessageKey;

    private void Awake()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_statusMessageText, nameof(_statusMessageText), this);
        hasAllReferences &= UnityUtility.CheckReference(_statusIndicator, nameof(_statusIndicator), this);
        hasAllReferences &= UnityUtility.CheckReference(_downloadDetailView, nameof(_downloadDetailView), this);
        hasAllReferences &= UnityUtility.CheckReference(_loginPopup, nameof(_loginPopup), this);
        hasAllReferences &= UnityUtility.CheckReference(_failurePopup, nameof(_failurePopup), this);
        hasAllReferences &= UnityUtility.CheckReference(_downloadConfirmPopup, nameof(_downloadConfirmPopup), this);

        if (!hasAllReferences)
        {
            enabled = false;
            return;
        }

        hasAllReferences &= _statusIndicator.HasAllReferences();
        hasAllReferences &= _downloadDetailView.HasAllReferences();
        hasAllReferences &= _loginPopup.HasAllReferences();
        hasAllReferences &= _failurePopup.HasAllReferences();
        hasAllReferences &= _downloadConfirmPopup.HasAllReferences();

        _downloadDetailView.Hide();
        _loginPopup.Hide();
        _failurePopup.Hide();
        _downloadConfirmPopup.Hide();

        enabled = hasAllReferences;
    }

    private void Start()
    {
        _session = new LoadingSession();
        _session.RetryStarted += OnRetryStarted;
        _session.FallingBackToBoot += OnFallingBackToBoot;
        _session.DownloadStarted += OnDownloadStarted;

        RunLoadingAsync(destroyCancellationToken).Forget();
    }

    private void Update()
    {
        ILoadingStatus status = _session.Status;

        if (status == null)
        {
            return;
        }

        if (status.IsRunning)
        {
            UpdateStepMessage(status.CurrentStepMessageKey);
        }
    }

    private void OnDestroy()
    {
        if (_session == null)
        {
            return;
        }

        _session.RetryStarted -= OnRetryStarted;
        _session.FallingBackToBoot -= OnFallingBackToBoot;
        _session.DownloadStarted -= OnDownloadStarted;
    }

    private async UniTask RunLoadingAsync(CancellationToken cancellationToken)
    {
        LoadResult result = await _session.RunAsync(cancellationToken);

        while (result.Status == LoadResultStatus.Failed || result.Status == LoadResultStatus.DownloadConfirmationRequired)
        {
            bool shouldResume;

            if (result.Status == LoadResultStatus.DownloadConfirmationRequired)
            {
                shouldResume = await ResolveDownloadConfirmationAsync(result.DownloadBytes, cancellationToken);
            }
            else
            {
                shouldResume = await ResolveFailureAsync(result.Failure, cancellationToken);
            }

            if (!shouldResume)
            {
                return;
            }

            bool isYieldCanceled = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow();

            if (isYieldCanceled)
            {
                return;
            }

            result = await _session.RunAsync(cancellationToken);
        }

        if (result.Status == LoadResultStatus.Succeeded)
        {
            await CompleteLoadingAsync(cancellationToken);
        }
    }

    private async UniTask<bool> ResolveDownloadConfirmationAsync(long downloadBytes, CancellationToken cancellationToken)
    {
        ShowUpdateCheckedMessage();

        (bool isCanceled, bool isConfirmed) = await _downloadConfirmPopup.ShowAsync(downloadBytes, cancellationToken).SuppressCancellationThrow();

        if (isCanceled)
        {
            return false;
        }

        if (!isConfirmed)
        {
            LoadResult declinedResult = _session.DeclineDownload();
            return await ResolveFailureAsync(declinedResult.Failure, cancellationToken);
        }

        _session.ConfirmDownload();
        _statusIndicator.ShowLoading();
        return true;
    }

    private async UniTask<bool> ResolveFailureAsync(LoadFailure failure, CancellationToken cancellationToken)
    {
        ShowFailureMessage();

        (bool isCanceled, LoadingFailureChoice choice) = await _failurePopup.ShowAsync(failure, cancellationToken).SuppressCancellationThrow();

        if (isCanceled)
        {
            return false;
        }

        if (choice != LoadingFailureChoice.Retry)
        {
            _session.GiveUp(failure);
            return false;
        }

        return true;
    }

    private async UniTask CompleteLoadingAsync(CancellationToken cancellationToken)
    {
        ShowCompletedMessage();

        bool isCanceled = await UniTask.WaitUntil(IsDownloadDetailHidden, PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow();

        if (isCanceled)
        {
            return;
        }

        _loginPopup.Show();
    }

    private bool IsDownloadDetailHidden()
    {
        return _downloadDetailView.IsHidden;
    }

    private void OnRetryStarted(int retryCount, int maxRetryCount)
    {
        ShowRetryingMessage(retryCount, maxRetryCount);
    }

    private void OnFallingBackToBoot()
    {
        ShowFailureMessage();
    }

    private void OnDownloadStarted()
    {
        _downloadDetailView.Run(_session.Status, destroyCancellationToken);
    }

    private void UpdateStepMessage(string messageKey)
    {
        if (string.IsNullOrWhiteSpace(messageKey) || string.Equals(messageKey, _displayedStepMessageKey, StringComparison.Ordinal))
        {
            return;
        }

        _displayedStepMessageKey = messageKey;
        _statusMessageText.SetKey(messageKey);
        _statusIndicator.ShowLoading();
    }

    private void ShowRetryingMessage(int retryCount, int maxRetryCount)
    {
        _displayedStepMessageKey = null;
        _statusMessageText.SetKey(LocalizationKeys.UiLoadingRetrying, retryCount, maxRetryCount);
        _statusIndicator.ShowLoading();
    }

    private void ShowFailureMessage()
    {
        _displayedStepMessageKey = null;
        _statusMessageText.SetKey(LocalizationKeys.UiLoadingFailed);
        _statusIndicator.ShowFailed();
    }

    private void ShowCompletedMessage()
    {
        _displayedStepMessageKey = null;
        _statusMessageText.SetKey(LocalizationKeys.UiLoadingCompleted);
        _statusIndicator.ShowCompleted();
    }

    private void ShowUpdateCheckedMessage()
    {
        _displayedStepMessageKey = null;
        _statusMessageText.SetKey(LocalizationKeys.UiLoadingUpdateChecked);
        _statusIndicator.ShowCompleted();
    }
}
