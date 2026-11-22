using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownloadConfirmPopup : UIBase
{
    private const string SizeTextFormat = "{0:0.0}MB";
    private const float BytesPerMegabyte = 1024f * 1024f;

    [SerializeField] private TMP_Text _sizeText;
    [SerializeField] private Button _downloadButton;
    [SerializeField] private Button _cancelButton;

    private UniTaskCompletionSource<bool> _choiceSource;

    private void OnEnable()
    {
        _downloadButton.onClick.AddListener(OnDownloadButtonClicked);
        _cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    private void OnDisable()
    {
        _downloadButton.onClick.RemoveListener(OnDownloadButtonClicked);
        _cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
    }

    public bool HasAllReferences()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_sizeText, nameof(_sizeText), this);
        hasAllReferences &= UnityUtility.CheckReference(_downloadButton, nameof(_downloadButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_cancelButton, nameof(_cancelButton), this);

        return hasAllReferences;
    }

    public async UniTask<bool> ShowAsync(long downloadBytes, CancellationToken cancellationToken)
    {
        _choiceSource?.TrySetCanceled();
        _choiceSource = new UniTaskCompletionSource<bool>();

        _sizeText.text = string.Format(SizeTextFormat, downloadBytes / BytesPerMegabyte);

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

    private void OnDownloadButtonClicked()
    {
        _choiceSource?.TrySetResult(true);
    }

    private void OnCancelButtonClicked()
    {
        _choiceSource?.TrySetResult(false);
    }
}
