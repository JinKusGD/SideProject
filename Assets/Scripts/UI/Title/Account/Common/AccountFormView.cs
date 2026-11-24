using TMPro;
using UnityEngine;

public abstract class AccountFormView : UIBase, IAccountFormView
{
    private const string IdleMessage = "...";

    private static readonly Color ErrorColor = new Color32(0xDC, 0x26, 0x26, 0xFF);
    private static readonly Color InfoColor = new Color32(0x64, 0x74, 0x8B, 0xFF);
    private static readonly Color SuccessColor = new Color32(0x15, 0x80, 0x3D, 0xFF);

    [SerializeField] protected TMP_Text _logText;

    public void SetSubmitting(bool isSubmitting)
    {
        SetButtonsInteractable(!isSubmitting);
    }

    public void ShowError(string key)
    {
        string errorString = LocalizationManager.Get(key);
        SetMessage(errorString, ErrorColor);
    }

    public void ShowInfo(string key)
    {
        string infoString = LocalizationManager.Get(key);
        SetMessage(infoString, InfoColor);
    }

    public void ShowSuccess(string key)
    {
        string showString = LocalizationManager.Get(key);
        SetMessage(showString, SuccessColor);
    }

    public void ShowErrorAndFocus(AccountInputField inputField, string key)
    {
        ShowError(key);
        FocusInputField(inputField);
    }

    protected void ClearMessage()
    {
        SetMessage(IdleMessage, InfoColor);
    }

    protected void FocusInputField(AccountInputField inputField)
    {
        TMP_InputField targetInput = GetInputField(inputField);
        targetInput.Select();
        targetInput.ActivateInputField();
    }

    protected abstract TMP_InputField GetInputField(AccountInputField inputField);

    protected abstract void SetButtonsInteractable(bool isInteractable);

    private void SetMessage(string message, Color color)
    {
        _logText.text = message;
        _logText.color = color;
    }
}
