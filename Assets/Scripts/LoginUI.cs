using TMPro;
using UnityEngine;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;

    [SerializeField] private TMP_Text _logText;

    private const string EmptyIdMessage = "Please enter your ID.";
    private const string EmptyPasswordMessage = "Please enter your password.";

    private void Awake()
    {
        if (_idInput == null || _passwordInput == null || _logText == null)
        {
            Debug.LogError("LoginUI references are not assigned.");
        }
    }

    private void OnEnable()
    {
        _idInput.onSubmit.AddListener(OnIdSubmitted);
        _passwordInput.onSubmit.AddListener(OnPasswordSubmitted);
    }

    private void OnDisable()
    {
        _idInput.onSubmit.RemoveListener(OnIdSubmitted);
        _passwordInput.onSubmit.RemoveListener(OnPasswordSubmitted);
    }

    private void OnIdSubmitted(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            ActivateInputField(_idInput, EmptyIdMessage);
  
            return;
        }

        if (string.IsNullOrWhiteSpace(_passwordInput.text))
        {
            SelectInputField(_passwordInput, EmptyPasswordMessage);
            return;
        }

        TryLogin();
    }

    private void OnPasswordSubmitted(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            ActivateInputField(_passwordInput, EmptyPasswordMessage);
            return;
        }

        if (string.IsNullOrWhiteSpace(_idInput.text))
        {
            SelectInputField(_idInput, EmptyIdMessage);
            return;
        }

        TryLogin();
    }

    private void SelectInputField(TMP_InputField inputField, string message)
    {
        _logText.text = message;
        inputField.Select();
    }

    private void ActivateInputField(TMP_InputField inputField, string message)
    {
        _logText.text = message;
        inputField.ActivateInputField();
    }

    private void TryLogin()
    {
        NetworkManager.Instance.TestDataBase(_idInput.text, _passwordInput.text);
    }
}