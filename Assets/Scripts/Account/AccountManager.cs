using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class AccountManager : IAccountService
{
    public static AccountManager Instance { get; } = new AccountManager();

    private AccountManager() { }

    public uint AccountUid { get; private set; }

    public async UniTask<LoginResult> LoginAsync(string id, string password, CancellationToken cancellationToken)
    {
        LoginPacket loginPacket = await NetworkManager.Instance.RequestLoginAsync(id, password, cancellationToken);

        if (loginPacket.LoginResult == LoginResult.Success)
        {
            AccountUid = loginPacket.AccountUid;
        }

        return loginPacket.LoginResult;
    }

    public UniTask<SignUpResult> SignUpAsync(string id, string password, CancellationToken cancellationToken)
    {
        return NetworkManager.Instance.RequestSignUpAsync(id, password, cancellationToken);
    }
}
