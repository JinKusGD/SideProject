using System.Collections.Generic;

// 씬 로드 스텝은 LoadingSession이 마지막에 자동으로 붙이므로 CreateSteps에 넣지 않는다.
// 씬을 넘어 전달되므로 씬 오브젝트를 참조하지 않는다.
public interface ILoadPlan
{
    string TargetSceneName { get; }

    string LoadingSceneName { get; }

    IReadOnlyList<ILoadStep> CreateSteps();
}
