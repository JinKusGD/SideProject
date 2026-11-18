using System;
using System.Collections.Generic;

public sealed class GameEntryLoadPlan : ILoadPlan
{
    public string TargetSceneName => SceneNames.Game;

    public string LoadingSceneName => SceneNames.LoginToGameLoading;

    public float SceneLoadWeight => 1f;

    public IReadOnlyList<ILoadStep> CreateSteps()
    {
        return Array.Empty<ILoadStep>();
    }
}
