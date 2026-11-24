using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressablesCatalogStep : ILoadStep
{
    private const string AddressablesCatalogStepName = "AddressablesCatalog";

    public string Name { get; } = AddressablesCatalogStepName;

    public string MessageKey { get; } = LocalizationKeys.UiLoadingStepUpdateInfo;

    public TimeSpan Timeout { get; } = TimeSpan.Zero;

    public async UniTask ExecuteAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken);

        List<string> catalogIds = await CheckForCatalogUpdatesAsync(cancellationToken);

        if (catalogIds.Count > 0)
        {
            await UpdateCatalogsAsync(catalogIds, cancellationToken);
        }
    }

    private static async UniTask InitializeAsync(CancellationToken cancellationToken)
    {
        AsyncOperationHandle<IResourceLocator> handle = Addressables.InitializeAsync(false);

        try
        {
            await handle.ToUniTask(cancellationToken: cancellationToken);
        }
        finally
        {
            Addressables.Release(handle);
        }
    }

    private static async UniTask<List<string>> CheckForCatalogUpdatesAsync(CancellationToken cancellationToken)
    {
        AsyncOperationHandle<List<string>> handle = Addressables.CheckForCatalogUpdates(false);
    
        try
        {
            List<string> catalogIds = await handle.ToUniTask(cancellationToken: cancellationToken);
            return new List<string>(catalogIds);
        }
        finally
        {
            Addressables.Release(handle);
        }
    }

    private static async UniTask UpdateCatalogsAsync(List<string> catalogIds, CancellationToken cancellationToken)
    {
        AsyncOperationHandle<List<IResourceLocator>> handle = Addressables.UpdateCatalogs(true, catalogIds, false);

        try
        {
            await handle.ToUniTask(cancellationToken: cancellationToken);
        }
        finally
        {
            Addressables.Release(handle);
        }
    }
}
