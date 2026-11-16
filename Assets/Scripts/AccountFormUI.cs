using TMPro;
using UnityEngine;

public abstract class AccountFormUI : UIBase
{
    protected const string EmptyIdMessage = "Please enter your ID.";
    protected const string EmptyPasswordMessage = "Please enter your password.";
    protected const string ConnectionErrorMessage = "Could not connect to the server.";
    protected const string TimeoutMessage = "The request timed out. Please try again.";

    [SerializeField] protected TMP_Text _logText;

    protected bool IsSubmitting { get; private set; }

    protected void SetSubmitting(bool isSubmitting)
    {
        IsSubmitting = isSubmitting;
        SetButtonsInteractable(!isSubmitting);
    }

    protected void FocusInputField(TMP_InputField inputField, string message)
    {
        inputField.Select();
        inputField.ActivateInputField();

        _logText.text = message;
    }

    protected abstract void SetButtonsInteractable(bool isInteractable);
}