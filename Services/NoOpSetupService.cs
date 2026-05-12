namespace IAMDemoProject.Services;

public sealed class NoOpSetupService : ISetupService
{
    public Task InitializeSecurityAsync() => Task.CompletedTask;
}
