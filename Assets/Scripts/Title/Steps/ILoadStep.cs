using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// cancellationToken을 무시하면 타임아웃이 동작하지 않는다.
// 재시도 때 같은 인스턴스로 다시 실행된다.
// LoadStepException 외의 예외는 재시도 가능으로 처리된다.
public interface ILoadStep
{
    string Name { get; }

    TimeSpan Timeout { get; }

    UniTask ExecuteAsync(CancellationToken cancellationToken);
}
