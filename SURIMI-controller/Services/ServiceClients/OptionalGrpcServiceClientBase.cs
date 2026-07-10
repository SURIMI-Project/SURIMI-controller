namespace SURIMI_controller.Services
{
    public abstract class OptionalGrpcServiceClientBase<T>(ILogger<T> logger, string excludeEnvVar)
        : GrpcServiceClientBase<T>(logger)
    {
        protected readonly bool IsEnabled = Environment.GetEnvironmentVariable(excludeEnvVar)?.ToLower() != "true";
    }
}
