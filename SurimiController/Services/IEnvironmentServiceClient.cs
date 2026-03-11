using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IEnvironmentServiceClient : IWorkflowService
    {
        Task<GetEnvironmentVariablesResponse> GetEnvironmentVariables(GetEnvironmentVariablesRequest getEnvironmentVariablesRequest, DateTime current, CancellationToken cancellationToken);
    }
}