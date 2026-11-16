using TMPro;
using UnityEngine;

public abstract class AccountFormUI : UIBase
{
    protected const string EmptyIdMessage = "Please enter your ID.";
    protected const string EmptyPasswordMessage = "Please enter your password.";
    protected const string ConnectionErrorMessage = "Could not connect to the server.";
    protected const string TimeoutMessage = "The request timed out. Please try again.";

    // 표시할 메시지가 없을 때 LogText에 보여 줄 문구
    private const string IdleMessage = "...";

    private static readonly Color ErrorColor = new Color32(0xDC, 0x26, 0x26, 0xFF);
    private static readonly Color InfoColor = new Color32(0x64, 0x74, 0x8B, 0xFF);
    private static readonly Color SuccessColor = new Color32(0x15, 0x80, 0x3D, 0xFF);

    [SerializeField] protected TMP_Text _logText;

    protected bool IsSubmitting { get; private set; }

    protected void SetSubmitting(bool isSubmitting)
    {
        IsSubmitting = isSubmitting;
        SetButtonsInteractable(!isSubmitting);
    }

    protected void ShowError(string message)
    {
        SetMessage(message, ErrorColor);
    }

    protected void ShowInfo(string message)
    {
        SetMessage(message, InfoColor);
    }

    protected void ShowSuccess(string message)
    {
        SetMessage(message, SuccessColor);
    }

    protected void ClearMessage()
    {
        SetMessage(IdleMessage, InfoColor);
    }

    protected void ShowErrorAndFocus(TMP_InputField inputField, string message)
    {
        ShowError(message);
        FocusInputField(inputField);
    }

    protected void FocusInputField(TMP_InputField inputField)
    {
        inputField.Select();
        inputField.ActivateInputField();
    }

    protected abstract void SetButtonsInteractable(bool isInteractable);

    private void SetMessage(string message, Color color)
    {
        _logText.text = message;
        _logText.color = color;
    }
}
