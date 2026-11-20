using UnityEngine;

public class LoginPopup : UIBase
{
    [SerializeField] private LoginUI _loginUI;
    [SerializeField] private SignUpUI _signUpUI;

    private void Awake()
    {
        ShowLogin();
    }

    private void OnEnable()
    {
        _loginUI.SignUpRequested += OnSignUpRequested;
        _signUpUI.BackRequested += OnBackRequested;
        _signUpUI.SignUpCompleted += OnSignUpCompleted;
    }

    private void OnDisable()
    {
        _loginUI.SignUpRequested -= OnSignUpRequested;
        _signUpUI.BackRequested -= OnBackRequested;
        _signUpUI.SignUpCompleted -= OnSignUpCompleted;
    }

    public bool HasAllReferences()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_loginUI, nameof(_loginUI), this);
        hasAllReferences &= UnityUtility.CheckReference(_signUpUI, nameof(_signUpUI), this);

        return hasAllReferences;
    }

    private void OnSignUpRequested()
    {
        ShowSignUp();
    }

    private void OnBackRequested()
    {
        ShowLogin();
    }

    private void OnSignUpCompleted(string id)
    {
        ShowLogin();
        _loginUI.ShowSignUpCompleted(id);
    }

    private void ShowLogin()
    {
        _signUpUI.Hide();
        _loginUI.Show();
    }

    private void ShowSignUp()
    {
        _loginUI.Hide();
        _signUpUI.Show();
    }
}