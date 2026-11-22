using Cysharp.Threading.Tasks;
using System.Threading;

public interface IAccountService
{
    UniTask<DBLoginResult> LoginAsync(string id, string password, CancellationToken cancellationToken);

    UniTask<DBSignUpResult> SignUpAsync(string id, string password, CancellationToken cancellationToken);
}
