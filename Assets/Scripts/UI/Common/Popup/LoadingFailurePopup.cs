using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingFailurePopup : UIBase
{
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private TMP_Text _errorCodeText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _returnButton;
    [SerializeField] private TMP_Text _returnButtonText;

    private UniTaskCompletionSource<LoadingFailureChoice> _choiceSource;

    private void OnEnable()
    {
        _retryButton.onClick.AddListener(OnRetryButtonClicked);
        _returnButton.onClick.AddListener(OnReturnButtonClicked);
    }

    private void OnDisable()
    {
        _retryButton.onClick.RemoveListener(OnRetryButtonClicked);
        _returnButton.onClick.RemoveListener(OnReturnButtonClicked);
    }

    public bool HasAllReferences()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_messageText, nameof(_messageText), this);
        hasAllReferences &= UnityUtility.CheckReference(_errorCodeText, nameof(_errorCodeText), this);
        hasAllReferences &= UnityUtility.CheckReference(_retryButton, nameof(_retryButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_returnButton, nameof(_returnButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_returnButtonText, nameof(_returnButtonText), this);

        return hasAllReferences;
    }

    public async UniTask<LoadingFailureChoice> ShowAsync(LoadFailure failure, CancellationToken cancellationToken)
    {
        _choiceSource?.TrySetCanceled();
        _choiceSource = new UniTaskCompletionSource<LoadingFailureChoice>();

        string messageKey = LoadFailureMessages.GetMessageKey(failure);
        string messageString = LocalizationManager.Get(messageKey);
        _messageText.text = messageString;

        string errorCodeString = LocalizationManager.Get(LocalizationKeys.UiLoadFailErrorCode, failure.ErrorCode, failure.StepName);
        _errorCodeText.text = errorCodeString;

        _retryButton.gameObject.SetActive(failure.IsRetryable);

        string returnButtonKey = failure.IsRetryable ? LocalizationKeys.UiButtonReturn : LocalizationKeys.UiButtonConfirm;
        string returnButtonString = LocalizationManager.Get(returnButtonKey);
        _returnButtonText.text = returnButtonString;

        Show();

        CancellationTokenRegistration registration = cancellationToken.Register(CancelChoice);

        try
        {
            return await _choiceSource.Task;
        }
        finally
        {
            registration.Dispose();
            _choiceSource = null;

            if (this != null)
            {
                Hide();
            }
        }
    }

    private void CancelChoice()
    {
        _choiceSource?.TrySetCanceled();
    }

    private void OnRetryButtonClicked()
    {
        _choiceSource?.TrySetResult(LoadingFailureChoice.Retry);
    }

    private void OnReturnButtonClicked()
    {
        _choiceSource?.TrySetResult(LoadingFailureChoice.GiveUp);
    }
}
