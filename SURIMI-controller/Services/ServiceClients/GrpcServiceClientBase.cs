using System.Runtime.CompilerServices;

namespace SURIMI_controller.Services
{
    public abstract class GrpcServiceClientBase<T>(ILogger<T> logger)
    {
        protected readonly ILogger<T> Logger = logger;

        protected string ServiceName { get; } = typeof(T).Name.Replace("ServiceClient", string.Empty);

        protected void LogStep(string id, DateTime current, [CallerMemberName] string step = "")
            => Logger.LogInformation("{Id} Processing step {ServiceName}.{Step}. {DateTime}", id, ServiceName, step, current);
    }
}
