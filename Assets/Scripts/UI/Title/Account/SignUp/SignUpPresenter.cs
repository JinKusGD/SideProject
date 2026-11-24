using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public sealed class SignUpPresenter
{
    private readonly ISignUpView _view;
    private readonly IAccountService _accountService;

    private bool _isSubmitting;

     public SignUpPresenter(ISignUpView view, IAccountService accountService)
    {
        _view = view;
        _accountService = accountService;
    }

    public async UniTask SubmitAsync(string id, string password, string passwordConfirm, CancellationToken cancellationToken)
    {
        if (_isSubmitting)
        {
            return;
        }

        if (!ValidateInput(id, password, passwordConfirm))
        {
            return;
        }

        SetSubmitting(true);
        _view.ShowInfo(LocalizationKeys.UiSignUpInProgress);

        SignUpResult signUpResult;

        try
        {
            signUpResult = await _accountService.SignUpAsync(id, password, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            DebugUtility.LogError(exception.ToString());
            SetSubmitting(false);
            _view.ShowError(LocalizationKeys.UiSignUpUnknownError);
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        HandleSignUpResult(signUpResult, id);
    }

    private bool ValidateInput(string id, string password, string passwordConfirm)
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

        if (string.IsNullOrWhiteSpace(passwordConfirm))
        {
            _view.ShowErrorAndFocus(AccountInputField.PasswordConfirm, LocalizationKeys.UiSignUpEmptyPasswordConfirm);
            return false;
        }

        bool isPasswordMatched = string.Equals(password, passwordConfirm, StringComparison.Ordinal);

        if (!isPasswordMatched)
        {
            _view.ShowErrorAndFocus(AccountInputField.PasswordConfirm, LocalizationKeys.UiSignUpPasswordMismatch);
            return false;
        }

        return true;
    }

    private void HandleSignUpResult(SignUpResult signUpResult, string id)
    {
        SetSubmitting(false);

        switch (signUpResult)
        {
            case SignUpResult.Success:
                _view.CompleteSignUp(id);
                break;
            case SignUpResult.AccountAlreadyExists:
                _view.ShowErrorAndFocus(AccountInputField.Id, LocalizationKeys.UiSignUpDuplicateId);
                break;
            case SignUpResult.ServerUnavailable:
                _view.ShowError(LocalizationKeys.UiAccountConnectionError);
                break;
            case SignUpResult.Timeout:
                _view.ShowError(LocalizationKeys.UiAccountTimeout);
                break;
            default:
                _view.ShowError(LocalizationKeys.UiSignUpUnknownError);
                break;
        }
    }

    private void SetSubmitting(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        _view.SetSubmitting(isSubmitting);
    }
}
