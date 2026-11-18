using System.Collections.Generic;

/// <summary>
/// 목적지 씬에 들어가기 위해 필요한 것을 정의한다.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>씬 로드 스텝은 <see cref="LoadingSession"/>이 항상 마지막에 추가하므로 <see cref="CreateSteps"/>에 넣지 않는다.</description></item>
/// <item><description><see cref="CreateSteps"/>는 null이나 null 스텝을 반환하지 않는다. 스텝이 없으면 빈 배열을 반환한다.</description></item>
/// <item><description>plan은 이전 씬에서 만들어져 로딩 씬으로 넘어가므로 데이터(ID 등)만 담고 씬 오브젝트를 참조하지 않는다.</description></item>
/// </list>
/// </remarks>
public interface ILoadPlan
{
    string TargetSceneName { get; }

    string LoadingSceneName { get; }

    float SceneLoadWeight { get; }

    IReadOnlyList<ILoadStep> CreateSteps();
}
