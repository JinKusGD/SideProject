using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : UIBase
{
    private const string EmptyIdMessage = "Please enter your ID.";
    private const string EmptyPasswordMessage = "Please enter your password.";
    private const string LoggingInMessage = "Logging in...";

    private const string LoginSuccessMessage = "Login successful.";
    private const string LoginFailedMessage = "Incorrect ID or password.";
    private const string TimeoutMessage = "The request timed out. Please try again.";
    private const string ConnectionErrorMessage = "Could not connect to the server.";
    private const string UnknownErrorMessage = "Login failed. Please try again later.";

    private const string GameSceneName = "Game";

    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private TMP_Text _logText;

    public event Action SignUpRequested;

    private bool _isSubmitting;

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
        if (_isSubmitting)
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
            dbLoginResult = await LoginManager.Instance.LoginAsync(id, password, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            DebugUtility.LogError(exception.ToString());
            dbLoginResult = DBLoginResult.UnknownError;
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
            _logText.text = LoginSuccessMessage;
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

    private void SetSubmitting(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        _loginButton.interactable = !isSubmitting;
        _signUpButton.interactable = !isSubmitting;
    }

    private void FocusInputField(TMP_InputField inputField, string message)
    {
        inputField.Select();
        inputField.ActivateInputField();

        _logText.text = message;
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(GameSceneName);
    }
}