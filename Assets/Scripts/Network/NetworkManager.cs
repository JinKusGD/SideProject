using Cysharp.Threading.Tasks;
using System.Threading;

public sealed class NetworkManager
{
    public static NetworkManager Instance { get; } = new NetworkManager();
 
    private readonly AccountApiClient _accountApiClient = new AccountApiClient();

    private NetworkManager() { }

    public UniTask<DBLoginPacket> RequestLoginAsync(string id, string password, CancellationToken cancellationToken)
    {
        return _accountApiClient.LoginAsync(id, password, cancellationToken);
    }

    public UniTask<DBSignUpResult> RequestSignUpAsync(string id, string password, CancellationToken cancellationToken)
    {
        return _accountApiClient.SignUpAsync(id, password, cancellationToken);
    }
}