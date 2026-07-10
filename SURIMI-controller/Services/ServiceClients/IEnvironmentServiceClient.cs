using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IEnvironmentServiceClient : IExperimentService
    {
        Task<GetEnvironmentVariablesResponse> GetEnvironmentVariables(GetEnvironmentVariablesRequest getEnvironmentVariablesRequest, DateTime current, CancellationToken cancellationToken);
    }
}