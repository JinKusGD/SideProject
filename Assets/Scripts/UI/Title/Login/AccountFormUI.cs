using TMPro;
using UnityEngine;

public abstract class AccountFormUI : UIBase
{
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

    protected void ShowError(string key)
    {
        string errorString = LocalizationManager.Get(key);
        SetMessage(errorString, ErrorColor);
    }

    protected void ShowInfo(string key)
    {
        string infoString = LocalizationManager.Get(key);
        SetMessage(infoString, InfoColor);
    }

    protected void ShowSuccess(string key)
    {
        string showString = LocalizationManager.Get(key);
        SetMessage(showString, SuccessColor);
    }

    protected void ClearMessage()
    {
        SetMessage(IdleMessage, InfoColor);
    }

    protected void ShowErrorAndFocus(TMP_InputField inputField, string key)
    {
        ShowError(key);
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
