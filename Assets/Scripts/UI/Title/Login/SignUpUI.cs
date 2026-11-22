using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpUI : AccountFormUI
{
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private Button _backButton;

    public event Action BackRequested;
    public event Action<string> SignUpCompleted;

    private void Awake()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_idInput, nameof(_idInput), this);
        hasAllReferences &= UnityUtility.CheckReference(_passwordInput, nameof(_passwordInput), this);
        hasAllReferences &= UnityUtility.CheckReference(_passwordConfirmInput, nameof(_passwordConfirmInput), this);
        hasAllReferences &= UnityUtility.CheckReference(_signUpButton, nameof(_signUpButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_backButton, nameof(_backButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_logText, nameof(_logText), this);

        enabled = hasAllReferences;
    }

    private void OnEnable()
    {
        _idInput.onSubmit.AddListener(OnInputSubmitted);
        _passwordInput.onSubmit.AddListener(OnInputSubmitted);
        _passwordConfirmInput.onSubmit.AddListener(OnInputSubmitted);
        _signUpButton.onClick.AddListener(OnSignUpButtonClicked);
        _backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnDisable()
    {
        _idInput.onSubmit.RemoveListener(OnInputSubmitted);
        _passwordInput.onSubmit.RemoveListener(OnInputSubmitted);
        _passwordConfirmInput.onSubmit.RemoveListener(OnInputSubmitted);
        _signUpButton.onClick.RemoveListener(OnSignUpButtonClicked);
        _backButton.onClick.RemoveListener(OnBackButtonClicked);
    }

    public override void Show()
    {
        ResetForm();
        base.Show();
    }

    protected override void SetButtonsInteractable(bool isInteractable)
    {
        _signUpButton.interactable = isInteractable;
        _backButton.interactable = isInteractable;
    }

    private void OnInputSubmitted(string _)
    {
        SubmitAsync().Forget();
    }

    private void OnSignUpButtonClicked()
    {
        SubmitAsync().Forget();
    }

    private void OnBackButtonClicked()
    {
        BackRequested?.Invoke();
    }

    private async UniTask SubmitAsync()
    {
        if (IsSubmitting)
        {
            return;
        }

        string id = _idInput.text;
        string password = _passwordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;

        if (!ValidateInput(id, password, passwordConfirm))
        {
            return;
        }

        SetSubmitting(true);
        ShowInfo(LocalizationKeys.UiSignUpInProgress);

        CancellationToken cancellationToken = destroyCancellationToken;
        DBSignUpResult dbSignUpResult;

        try
        {
            dbSignUpResult = await AccountManager.Instance.SignUpAsync(id, password, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            DebugUtility.LogError(exception.ToString());
            SetSubmitting(false);
            ShowError(LocalizationKeys.UiSignUpUnknownError);
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        HandleSignUpResult(dbSignUpResult, id);
    }

    private bool ValidateInput(string id, string password, string passwordConfirm)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            ShowErrorAndFocus(_idInput, LocalizationKeys.UiAccountEmptyId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowErrorAndFocus(_passwordInput, LocalizationKeys.UiAccountEmptyPassword);
            return false;
        }

        if (string.IsNullOrWhiteSpace(passwordConfirm))
        {
            ShowErrorAndFocus(_passwordConfirmInput, LocalizationKeys.UiSignUpEmptyPasswordConfirm);
            return false;
        }

        bool isPasswordMatched = string.Equals(password, passwordConfirm, StringComparison.Ordinal);

        if (!isPasswordMatched)
        {
            ShowErrorAndFocus(_passwordConfirmInput, LocalizationKeys.UiSignUpPasswordMismatch);
            return false;
        }

        return true;
    }

    private void HandleSignUpResult(DBSignUpResult dbSignUpResult, string id)
    {
        SetSubmitting(false);

        switch (dbSignUpResult)
        {
            case DBSignUpResult.Success:
                SignUpCompleted?.Invoke(id);
                break;
            case DBSignUpResult.AccountAlreadyExists:
                ShowErrorAndFocus(_idInput, LocalizationKeys.UiSignUpDuplicateId);
                break;
            case DBSignUpResult.ServerUnavailable:
                ShowError(LocalizationKeys.UiAccountConnectionError);
                break;
            case DBSignUpResult.Timeout:
                ShowError(LocalizationKeys.UiAccountTimeout);
                break;
            default:
                ShowError(LocalizationKeys.UiSignUpUnknownError);
                break;
        }
    }

    private void ResetForm()
    {
        _idInput.text = string.Empty;
        _passwordInput.text = string.Empty;
        _passwordConfirmInput.text = string.Empty;
        ClearMessage();
    }
}