using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpView : AccountFormView, ISignUpView
{
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private Button _backButton;

    private SignUpPresenter _presenter;

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

        _presenter = new SignUpPresenter(this, AccountManager.Instance);

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

    public void CompleteSignUp(string id)
    {
        SignUpCompleted?.Invoke(id);
    }

    protected override void SetButtonsInteractable(bool isInteractable)
    {
        _signUpButton.interactable = isInteractable;
        _backButton.interactable = isInteractable;
    }

    private void OnInputSubmitted(string _)
    {
        Submit();
    }

    private void OnSignUpButtonClicked()
    {
        Submit();
    }

    private void Submit()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;

        _presenter.SubmitAsync(id, password, passwordConfirm, destroyCancellationToken).Forget();
    }

    private void OnBackButtonClicked()
    {
        BackRequested?.Invoke();
    }

    private void ResetForm()
    {
        _idInput.text = string.Empty;
        _passwordInput.text = string.Empty;
        _passwordConfirmInput.text = string.Empty;
        ClearMessage();
    }

    protected override TMP_InputField GetInputField(AccountInputField inputField)
    {
        switch (inputField)
        {
            case AccountInputField.Id:
                return _idInput;
            case AccountInputField.Password:
                return _passwordInput;
            case AccountInputField.PasswordConfirm:
                return _passwordConfirmInput;
            default:
                throw new ArgumentOutOfRangeException(nameof(inputField), inputField, null);
        }
    }
}