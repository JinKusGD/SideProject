using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginView : AccountFormView, ILoginView
{
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _signUpButton;

    private LoginPresenter _presenter;

    public event Action SignUpRequested;

    private void Awake()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_idInput, nameof(_idInput), this);
        hasAllReferences &= UnityUtility.CheckReference(_passwordInput, nameof(_passwordInput), this);
        hasAllReferences &= UnityUtility.CheckReference(_loginButton, nameof(_loginButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_signUpButton, nameof(_signUpButton), this);
        hasAllReferences &= UnityUtility.CheckReference(_logText, nameof(_logText), this);

        _presenter = new LoginPresenter(this, AccountManager.Instance);

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
        ClearPassword();
        ClearMessage();

        base.Show();
    }

    public void ShowSignUpCompleted(string id)
    {
        _idInput.text = id;
        ShowSuccess(LocalizationKeys.UiLoginSignUpCompleted);
        FocusInputField(AccountInputField.Password);
    }

    public void ClearPassword()
    {
        _passwordInput.text = string.Empty;
    }

    protected override void SetButtonsInteractable(bool isInteractable)
    {
        _loginButton.interactable = isInteractable;
        _signUpButton.interactable = isInteractable;
    }

    protected override TMP_InputField GetInputField(AccountInputField inputField)
    {
        switch (inputField)
        {
            case AccountInputField.Id:
                return _idInput;
            case AccountInputField.Password:
                return _passwordInput;
            default:
                throw new ArgumentOutOfRangeException(nameof(inputField), inputField, null);
        }
    }

    private void OnInputSubmitted(string _)
    {
        Submit();
    }

    private void OnLoginButtonClicked()
    {
        Submit();
    }

    private void OnSignUpButtonClicked()
    {
        SignUpRequested?.Invoke();
    }

    private void Submit()
    {
        //string id = _idInput.text;
        //string password = _passwordInput.text;

        string id = TestAccount.ID;
        string password = TestAccount.PASSWORD;

        _presenter.SubmitAsync(id, password, destroyCancellationToken).Forget();
    }
}
