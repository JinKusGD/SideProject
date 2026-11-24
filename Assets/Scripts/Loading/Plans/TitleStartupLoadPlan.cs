using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

public sealed class TitleStartupLoadPlan : ILoadPlan
{
    private const string PatchLabel = "Patch";

    public string TargetSceneName => null;

    public string LoadingSceneName => SceneNames.Title;

    public IReadOnlyList<ILoadStep> CreateSteps()
    {
        List<ILoadStep> steps = new List<ILoadStep>
        {
            new AddressablesCatalogStep(),
            new AddressablesDownloadStep("resources", LocalizationKeys.UiLoadingStepResources, PatchLabel),

            new GeneralLoadStep("strings", LocalizationKeys.UiLoadingStepStrings, LoadMainStringsAsync)
        };

        return steps;
    }

    private UniTask LoadMainStringsAsync(CancellationToken cancellationToken)
    {
        return LocalizationManager.LoadMainTablesAsync(cancellationToken);
    }
}
