using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class NetworkManager
{
    public static NetworkManager Instance { get; } = new NetworkManager();
 
    private readonly DatabaseService _databaseService = new DatabaseService();

    private NetworkManager() { }

    public UniTask<DBLoginPacket> RequestLoginAsync(string id, string password, CancellationToken cancellationToken)
    {
        return _databaseService.LoginAsync(id, password, cancellationToken);
    }
}