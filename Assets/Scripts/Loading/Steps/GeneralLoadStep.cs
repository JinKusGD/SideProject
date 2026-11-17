using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public delegate UniTask LoadWork(IProgress<float> progress, CancellationToken cancellationToken);

public sealed class GeneralLoadStep : ILoadStep
{
    public GeneralLoadStep(string stepName, float stepWeight, LoadWork work)
    {
        Name = stepName;
        Weight = stepWeight;
        _work = work;
    }

    private readonly LoadWork _work;

    public string Name { get; }

    public float Weight { get; }

    public UniTask ExecuteAsync(IProgress<float> progress, CancellationToken cancellationToken)
    {
        return _work(progress, cancellationToken);
    }
}
