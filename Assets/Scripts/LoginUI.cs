using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    private const string EmptyIdMessage = "Please enter your ID.";
    private const string EmptyPasswordMessage = "Please enter your password.";
    private const string LoggingInMessage = "Logging in...";
    private const string LoginSuccessMessage = "Login successful.";
    private const string LoginFailedMessage = "Incorrect ID or password.";
    private const string ConnectionErrorMessage = "Could not connect to the server.";

    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private TMP_Text _logText;

    private bool _isSubmitting;

    private void Awake()
    {
        if (_idInput == null || _passwordInput == null || _loginButton == null || _logText == null)
        {
            DebugUtility.LogError("LoginUI references are not assigned.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        _idInput.onSubmit.AddListener(OnInputSubmitted);
        _passwordInput.onSubmit.AddListener(OnInputSubmitted);
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
    }

    private void OnDisable()
    {
        _idInput.onSubmit.RemoveListener(OnInputSubmitted);
        _passwordInput.onSubmit.RemoveListener(OnInputSubmitted);
        _loginButton.onClick.RemoveListener(OnLoginButtonClicked);
    }

    private void OnInputSubmitted(string _)
    {
        SubmitAsync().Forget();
    }

    private void OnLoginButtonClicked()
    {
        SubmitAsync().Forget();
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

        CancellationToken cancellationToken = destroyCancellationToken;

        SetSubmitting(true);
        _logText.text = LoggingInMessage;

        DatabaseResult result;

        try
        {
            result = await LoginManager.Instance.LoginAsync(id, password, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            DebugUtility.LogError(e.ToString());
            result = DatabaseResult.Error;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        HandleLoginResult(result);
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

    private void HandleLoginResult(DatabaseResult result)
    {
        switch (result)
        {
            case DatabaseResult.Success:
                _logText.text = LoginSuccessMessage;
                LoadGameScene(); //임시
                break;
            case DatabaseResult.NotFound:
                _passwordInput.text = string.Empty;
                FocusInputField(_passwordInput, LoginFailedMessage);
                SetSubmitting(false);
                break;
            case DatabaseResult.Error:
            default:
                _logText.text = ConnectionErrorMessage;
                SetSubmitting(false);
                break;
        }
    }

    private void SetSubmitting(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        _loginButton.interactable = !isSubmitting;
    }

    private void FocusInputField(TMP_InputField inputField, string message)
    {
        inputField.Select();
        inputField.ActivateInputField();

        _logText.text = message;
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}