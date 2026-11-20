using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

public sealed class TitleStartupLoadPlan : ILoadPlan
{
    private const float FakeUpdateCheckSeconds = 5f;
    private const long FakeDownloadBytes = 456L * 1024 * 1024;
    private const float FakeDownloadSeconds = 6f;

    public string TargetSceneName => null;

    public string LoadingSceneName => SceneNames.Title;

    public IReadOnlyList<ILoadStep> CreateSteps()
    {
        List<ILoadStep> steps = new List<ILoadStep>
        {
            new GeneralLoadStep("update info", CheckUpdateAsync),
            new DownloadStep("resources", FakeDownloadBytes, FakeDownloadSeconds)
        };

        return steps;
    }

    private UniTask CheckUpdateAsync(CancellationToken cancellationToken)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(FakeUpdateCheckSeconds), ignoreTimeScale: true, cancellationToken: cancellationToken);
    }
}
