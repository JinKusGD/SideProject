using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// 로딩 화면에서 실행되는 작업 하나.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>cancellationToken을 반드시 확인해야 한다. 무시하면 타임아웃이 동작하지 않는다.</description></item>
/// <item><description>재시도 시 같은 인스턴스가 다시 실행되므로 여러 번 실행해도 문제가 없어야 한다.</description></item>
/// <item><description>실패 처리 방식을 정하려면 <see cref="LoadStepException"/>을 던진다. 그 외 예외는 재시도 가능으로 처리된다.</description></item>
/// </list>
/// </remarks>
public interface ILoadStep
{
    string Name { get; }

    float Weight { get; }

    TimeSpan Timeout { get; }

    UniTask ExecuteAsync(IProgress<float> progress, CancellationToken cancellationToken);
}
