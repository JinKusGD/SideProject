using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : AccountFormUI
{
    private const string LoggingInMessage = "Logging in...";

    private const string LoginFailedMessage = "Incorrect ID or password.";
    private const string UnknownErrorMessage = "Login failed. Please try again later.";

    private const string SignUpCompletedMessage = "Account created. Please log in.";

    private const string GameSceneName = "Game";

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
        _logText.text = string.Empty;

        base.Show();
    }

    public void ShowSignUpCompleted(string id)
    {
        _idInput.text = id;
        FocusInputField(_passwordInput, SignUpCompletedMessage);
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

        string id = _idInput.text;
        string password = _passwordInput.text;

        if (!ValidateInput(id, password))
        {
            return;
        }

        SetSubmitting(true);
        _logText.text = LoggingInMessage;

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
            FocusInputField(_idInput, EmptyIdMessage);
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            FocusInputField(_passwordInput, EmptyPasswordMessage);
            return false;
        }

        return true;
    }

    private void HandleLoginResult(DBLoginResult dbLoginResult)
    {
        if (dbLoginResult == DBLoginResult.Success)
        {
            LoadGameScene(); // 임시
            return;
        }

        SetSubmitting(false);

        switch (dbLoginResult)
        {
            case DBLoginResult.AccountNotFound:
                _passwordInput.text = string.Empty;
                FocusInputField(_passwordInput, LoginFailedMessage);
                break;
            case DBLoginResult.ServerUnavailable:
                _logText.text = ConnectionErrorMessage;
                break;
            case DBLoginResult.Timeout:
                _logText.text = TimeoutMessage;
                break;
            default:
                _logText.text = UnknownErrorMessage;
                break;
        }
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(GameSceneName);
    }
}