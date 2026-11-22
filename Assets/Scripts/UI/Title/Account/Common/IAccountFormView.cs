public interface IAccountFormView
{
    void SetSubmitting(bool isSubmitting);

    void ShowError(string key);

    void ShowInfo(string key);

    void ShowSuccess(string key);

    void ShowErrorAndFocus(AccountInputField field, string key);
}
