using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class LoginManager
{
    public static LoginManager Instance { get; } = new LoginManager();

    private LoginManager() { }

    public uint Uid { get; private set; }

    public async UniTask<DatabaseResult> LoginAsync(string id, string password, CancellationToken cancellationToken)
    {
        DatabaseLoginPacket databaseLoginPacket = await NetworkManager.Instance.RequestLoginAsync(id, password, cancellationToken);

        if (databaseLoginPacket.DatabaseResult == DatabaseResult.Success)
        {
            Uid = databaseLoginPacket.ResultUid;
        }

        return databaseLoginPacket.DatabaseResult;
    }
}