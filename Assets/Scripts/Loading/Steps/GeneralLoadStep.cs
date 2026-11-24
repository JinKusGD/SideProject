using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class GeneralLoadStep : ILoadStep
{
    private const float DefaultTimeoutSeconds = 30f;

    private readonly Func<CancellationToken, UniTask> _work;

    public GeneralLoadStep(string name, string messageKey, Func<CancellationToken, UniTask> work) : this(name, messageKey, work, TimeSpan.FromSeconds(DefaultTimeoutSeconds))
    {
    }

    public GeneralLoadStep(string name, string messageKey, Func<CancellationToken, UniTask> work, TimeSpan timeout)
    {
        if (work == null)
        {
            throw new ArgumentNullException(nameof(work));
        }

        Name = name;
        MessageKey = messageKey;
        Timeout = timeout;
        _work = work;
    }

    public string Name { get; }

    public string MessageKey { get; }

    public TimeSpan Timeout { get; }

    public UniTask ExecuteAsync(CancellationToken cancellationToken)
    {
        return _work(cancellationToken);
    }
}
