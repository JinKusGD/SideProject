using UnityEngine;

public class AccountUIController : MonoBehaviour
{
    [SerializeField] private LoginUI _loginUI;
    [SerializeField] private SignUpUI _signUpUI;

    private void Awake()
    {
        bool hasAllReferences = true;

        hasAllReferences &= UnityUtility.CheckReference(_loginUI, nameof(_loginUI), this);
        hasAllReferences &= UnityUtility.CheckReference(_signUpUI, nameof(_signUpUI), this);

        enabled = hasAllReferences;

        if (hasAllReferences)
        {
            ShowLogin();
        }
    }

    private void OnEnable()
    {
        _loginUI.SignUpRequested += OnSignUpRequested;
        _signUpUI.BackRequested += OnBackRequested;
    }

    private void OnDisable()
    {
        _loginUI.SignUpRequested -= OnSignUpRequested;
        _signUpUI.BackRequested -= OnBackRequested;
    }

    private void OnSignUpRequested()
    {
        ShowSignUp();
    }

    private void OnBackRequested()
    {
        ShowLogin();
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