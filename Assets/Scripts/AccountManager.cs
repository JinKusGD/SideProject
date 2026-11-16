using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class AccountManager
{
    public static AccountManager Instance { get; } = new AccountManager();

    private AccountManager() { }

    public uint AccountUid { get; private set; }

    public async UniTask<DBLoginResult> LoginAsync(string id, string password, CancellationToken cancellationToken)
    {
        DBLoginPacket dbLoginPacket = await NetworkManager.Instance.RequestLoginAsync(id, password, cancellationToken);

        if (dbLoginPacket.DBLoginResult == DBLoginResult.Success)
        {
            AccountUid = dbLoginPacket.AccountUid;
        }

        return dbLoginPacket.DBLoginResult;
    }

    public UniTask<DBSignUpResult> SignUpAsync(string id, string password, CancellationToken cancellationToken)
    {
        return NetworkManager.Instance.RequestSignUpAsync(id, password, cancellationToken);
    }
}
