public interface ILoadingProgress
{
    float TotalProgress { get; }

    string CurrentStepName { get; }
}
