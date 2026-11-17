using Cysharp.Threading.Tasks;
using System;
using System.Threading;

// 로딩 화면에서 실행되는 작업 하나. 실패 후 재시도하면 같은 인스턴스가 다시 실행될 수 있다.
public interface ILoadStep
{
    string Name { get; }
    float Weight { get; }

    UniTask ExecuteAsync(IProgress<float> progress, CancellationToken cancellationToken);
}
