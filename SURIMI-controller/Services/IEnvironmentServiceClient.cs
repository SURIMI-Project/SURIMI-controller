using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public interface IEnvironmentServiceClient : IWorkflowService
    {
        Task<GetEnvironmentVariablesResponse> GetEnvironmentVariables(GetEnvironmentVariablesRequest getEnvironmentVariablesRequest, DateTime current, CancellationToken cancellationToken);
    }
}