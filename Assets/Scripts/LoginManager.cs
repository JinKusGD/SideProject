using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class LoginManager
{
    public static LoginManager Instance { get; } = new LoginManager();

    private LoginManager() { }

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
}