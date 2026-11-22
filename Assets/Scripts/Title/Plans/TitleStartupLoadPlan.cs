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
            new GeneralLoadStep("update info", LocalizationKeys.UiLoadingStepUpdateInfo, CheckUpdateAsync),
            new DownloadStep("resources", LocalizationKeys.UiLoadingStepResources, FakeDownloadBytes, FakeDownloadSeconds),

            // 패치가 끝난 뒤에 Main 문자열을 불러온다. 이 단계 전까지 화면에는 Boot 문자열만 쓸 수 있다.
            new GeneralLoadStep("strings", LocalizationKeys.UiLoadingStepStrings, LoadMainStringsAsync)
        };

        return steps;
    }

    // Main 문자열은 Addressables로 읽는다. 실패하면 예외가 이 스텝의 실패로 이어져 자동 재시도/실패 팝업 흐름을 탄다.
    private UniTask LoadMainStringsAsync(CancellationToken cancellationToken)
    {
        return LocalizationManager.LoadMainTablesAsync(cancellationToken);
    }

    private UniTask CheckUpdateAsync(CancellationToken cancellationToken)
    {
        return UniTask.Delay(TimeSpan.FromSeconds(FakeUpdateCheckSeconds), ignoreTimeScale: true, cancellationToken: cancellationToken);
    }
}
