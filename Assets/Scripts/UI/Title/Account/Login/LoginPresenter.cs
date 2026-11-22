using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public sealed class LoginPresenter
{
    private readonly ILoginView _view;
    private readonly IAccountService _accountService;

    private bool _isSubmitting;

    public LoginPresenter(ILoginView view, IAccountService accountService)
    {
        _view = view;
        _accountService = accountService;
    }

    public async UniTask SubmitAsync(string id, string password, CancellationToken cancellationToken)
    {
        if (_isSubmitting)
        {
            return;
        }

        if (!ValidateInput(id, password))
        {
            return;
        }

        SetSubmitting(true);
        _view.ShowInfo(LocalizationKeys.UiLoginInProgress);

        DBLoginResult dbLoginResult;

        try
        {
            dbLoginResult = await _accountService.LoginAsync(id, password, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            DebugUtility.LogError(exception.ToString());
            SetSubmitting(false);
            _view.ShowError(LocalizationKeys.UiLoginUnknownError);
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        HandleLoginResult(dbLoginResult);
    }

    private bool ValidateInput(string id, string password)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _view.ShowErrorAndFocus(AccountInputField.Id, LocalizationKeys.UiAccountEmptyId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            _view.ShowErrorAndFocus(AccountInputField.Password, LocalizationKeys.UiAccountEmptyPassword);
            return false;
        }

        return true;
    }

    private void HandleLoginResult(DBLoginResult dbLoginResult)
    {
        if (dbLoginResult == DBLoginResult.Success)
        {
            SceneTransition.Start(new GameEntryLoadPlan());
            return;
        }

        SetSubmitting(false);

        switch (dbLoginResult)
        {
            case DBLoginResult.AccountNotFound:
                _view.ClearPassword();
                _view.ShowErrorAndFocus(AccountInputField.Password, LocalizationKeys.UiLoginFailed);
                break;
            case DBLoginResult.ServerUnavailable:
                _view.ShowError(LocalizationKeys.UiAccountConnectionError);
                break;
            case DBLoginResult.Timeout:
                _view.ShowError(LocalizationKeys.UiAccountTimeout);
                break;
            default:
                _view.ShowError(LocalizationKeys.UiLoginUnknownError);
                break;
        }
    }

    private void SetSubmitting(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        _view.SetSubmitting(isSubmitting);
    }
}
