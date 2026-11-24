using Cysharp.Threading.Tasks;
using System.Threading;

public interface IAccountService
{
    UniTask<LoginResult> LoginAsync(string id, string password, CancellationToken cancellationToken);

    UniTask<SignUpResult> SignUpAsync(string id, string password, CancellationToken cancellationToken);
}
