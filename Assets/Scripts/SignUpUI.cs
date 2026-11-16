using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpUI : UIBase
{
    private const string EmptyIdMessage = "Please enter your ID.";
    private const string EmptyPasswordMessage = "Please enter your password.";
    private const string EmptyPasswordConfirmMessage = "Please confirm your password.";
    private const string PasswordMismatchMessage = "Passwords do not match.";

    private const string SigningUpMessage = "Creating account...";

    private const string SignUpSuccessMessage = "Account created.";
    private const string DuplicateIdMessage = "This ID is already in use.";
    private const string ConnectionErrorMessage = "Could not connect to the server.";
    private const string TimeoutMessage = "The request timed out. Please try again.";
    private const string UnknownErrorMessage = "Sign up failed. Please try again later.";

    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private TMP_Text _logText;
    
    public event Action BackRequested;

    private bool _isSubmitting;

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
        if (_isSubmitting)
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
        _logText.text = SigningUpMessage;

        CancellationToken cancellationToken = destroyCancellationToken;
        DBSignUpResult dbSignUpResult;

        try
        {
            dbSignUpResult = await NetworkManager.Instance.RequestSignUpAsync(id, password, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            DebugUtility.LogError(exception.ToString());
            dbSignUpResult = DBSignUpResult.UnknownError;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        HandleSignUpResult(dbSignUpResult);
    }

    private bool ValidateInput(string id, string password, string passwordConfirm)
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

        if (string.IsNullOrWhiteSpace(passwordConfirm))
        {
            FocusInputField(_passwordConfirmInput, EmptyPasswordConfirmMessage);
            return false;
        }

        bool isPasswordMatched = string.Equals(password, passwordConfirm, StringComparison.Ordinal);

        if (!isPasswordMatched)
        {
            FocusInputField(_passwordConfirmInput, PasswordMismatchMessage);
            return false;
        }

        return true;
    }

    private void HandleSignUpResult(DBSignUpResult dbSignUpResult)
    {
        SetSubmitting(false);

        switch (dbSignUpResult)
        {
            case DBSignUpResult.Success:
                ResetForm();
                _logText.text = SignUpSuccessMessage;
                break;
            case DBSignUpResult.AccountAlreadyExists:
                FocusInputField(_idInput, DuplicateIdMessage);
                break;
            case DBSignUpResult.ServerUnavailable:
                _logText.text = ConnectionErrorMessage;
                break;
            case DBSignUpResult.Timeout:
                _logText.text = TimeoutMessage;
                break;
            default:
                _logText.text = UnknownErrorMessage;
                break;
        }
    }

    private void ResetForm()
    {
        _idInput.text = string.Empty;
        _passwordInput.text = string.Empty;
        _passwordConfirmInput.text = string.Empty;
        _logText.text = string.Empty;
    }

    private void SetSubmitting(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        _signUpButton.interactable = !isSubmitting;
        _backButton.interactable = !isSubmitting;
    }

    private void FocusInputField(TMP_InputField inputField, string message)
    {
        inputField.Select();
        inputField.ActivateInputField();

        _logText.text = message;
    }
}