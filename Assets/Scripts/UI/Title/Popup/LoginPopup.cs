using UnityEngine;

public class LoginPopup : UIBase
{
    [SerializeField] private LoginView _loginView;
    [SerializeField] private SignUpView _signUpView;

    private void Awake()
    {
        ShowLogin();
    }

    private void OnEnable()
    {
        _loginView.SignUpRequested += OnSignUpRequested;
        _signUpView.BackRequested += OnBackRequested;
        _signUpView.SignUpCompleted += OnSignUpCompleted;
    }

    private void OnDisable()
    {
        _loginView.SignUpRequested -= OnSignUpRequested;
        _signUpView.BackRequested -= OnBackRequested;
        _signUpView.SignUpCompleted -= OnSignUpCompleted;
    }

    public bool HasAllReferences()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_loginView, nameof(_loginView), this);
        hasAllReferences &= UnityUtility.CheckReference(_signUpView, nameof(_signUpView), this);

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
        _loginView.ShowSignUpCompleted(id);
    }

    private void ShowLogin()
    {
        _signUpView.Hide();
        _loginView.Show();
    }

    private void ShowSignUp()
    {
        _loginView.Hide();
        _signUpView.Show();
    }
}