using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class GeneralLoadStep : ILoadStep
{
    private const float DefaultTimeoutSeconds = 30f;

    private readonly Func<IProgress<float>, CancellationToken, UniTask> _work;

    public GeneralLoadStep(string name, float weight, Func<IProgress<float>, CancellationToken, UniTask> work) : this(name, weight, work, TimeSpan.FromSeconds(DefaultTimeoutSeconds))
    {
    }

    public GeneralLoadStep(string name, float weight, Func<IProgress<float>, CancellationToken, UniTask> work, TimeSpan timeout)
    {
        if (work == null)
        {
            throw new ArgumentNullException(nameof(work));
        }

        Name = name;
        Weight = weight;
        Timeout = timeout;
        _work = work;
    }

    public string Name { get; }

    public float Weight { get; }

    public TimeSpan Timeout { get; }

    public UniTask ExecuteAsync(IProgress<float> progress, CancellationToken cancellationToken)
    {
        return _work(progress, cancellationToken);
    }
}
