using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : AccountFormUI
{
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _signUpButton;

    public event Action SignUpRequested;

    private void Awake()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_idInput, nameof(_idInput), this);
        hasAllReferences &= UnityUtility.CheckReference(_passwordInput, nameof(_passwordInput), this);
        hasAllReferences &= UnityUtility.CheckReference(_loginButton, nameof(_loginButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_signUpButton, nameof(_signUpButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_logText, nameof(_logText), this);

        enabled = hasAllReferences;
    }

    private void OnEnable()
    {
        _idInput.onSubmit.AddListener(OnInputSubmitted);
        _passwordInput.onSubmit.AddListener(OnInputSubmitted);
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
        _signUpButton.onClick.AddListener(OnSignUpButtonClicked);
    }

    private void OnDisable()
    {
        _idInput.onSubmit.RemoveListener(OnInputSubmitted);
        _passwordInput.onSubmit.RemoveListener(OnInputSubmitted);
        _loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        _signUpButton.onClick.RemoveListener(OnSignUpButtonClicked);
    }

    public override void Show()
    {
        _passwordInput.text = string.Empty;
        ClearMessage();

        base.Show();
    }

    public void ShowSignUpCompleted(string id)
    {
        _idInput.text = id;
        ShowSuccess(LocalizationKeys.UiLoginSignUpCompleted);
        FocusInputField(_passwordInput);
    }

    protected override void SetButtonsInteractable(bool isInteractable)
    {
        _loginButton.interactable = isInteractable;
        _signUpButton.interactable = isInteractable;
    }

    private void OnInputSubmitted(string _)
    {
        SubmitAsync().Forget();
    }

    private void OnLoginButtonClicked()
    {
        SubmitAsync().Forget();
    }

    private void OnSignUpButtonClicked()
    {
        SignUpRequested?.Invoke();
    }

    private async UniTask SubmitAsync()
    {
        if (IsSubmitting)
        {
            return;
        }

        //string id = _idInput.text;
        //string password = _passwordInput.text;

        string id = TestAccount.ID;
        string password = TestAccount.PASSWORD;

        if (!ValidateInput(id, password))
        {
            return;
        }

        SetSubmitting(true);
        ShowInfo(LocalizationKeys.UiLoginInProgress);

        CancellationToken cancellationToken = destroyCancellationToken;
        DBLoginResult dbLoginResult;

        try
        {
            dbLoginResult = await AccountManager.Instance.LoginAsync(id, password, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            DebugUtility.LogError(exception.ToString());
            SetSubmitting(false);
            ShowError(LocalizationKeys.UiLoginUnknownError);
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        HandleLoginResult(dbLoginResult);
    }

    private bool ValidateInput(string id, string password)
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

        return true;
    }

    private void HandleLoginResult(DBLoginResult dbLoginResult)
    {
        if (dbLoginResult == DBLoginResult.Success)
        {
            SceneTransition.Start(new GameEntryLoadPlan());
            return;
        }

        SetSubmitting(false);

        switch (dbLoginResult)
        {
            case DBLoginResult.AccountNotFound:
                _passwordInput.text = string.Empty;
                ShowErrorAndFocus(_passwordInput, LocalizationKeys.UiLoginFailed);
                break;
            case DBLoginResult.ServerUnavailable:
                ShowError(LocalizationKeys.UiAccountConnectionError);
                break;
            case DBLoginResult.Timeout:
                ShowError(LocalizationKeys.UiAccountTimeout);
                break;
            default:
                ShowError(LocalizationKeys.UiLoginUnknownError);
                break;
        }
    }
}