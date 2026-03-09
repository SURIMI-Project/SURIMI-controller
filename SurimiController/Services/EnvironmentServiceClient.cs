using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class EnvironmentServiceClient : IEnvironmentServiceClient
    {
        private readonly WorkflowService.WorkflowServiceClient _environmentWorkflowClient;
        private readonly EnvironmentProviderService.EnvironmentProviderServiceClient _environmentEnvironmentProviderClient;
        private readonly ILogger<EnvironmentServiceClient> _logger;

        public EnvironmentServiceClient(GrpcClientFactory clientFactory, ILogger<EnvironmentServiceClient> logger)
        {
            _environmentWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EnvironmentWorkflow");
            _environmentEnvironmentProviderClient = clientFactory.CreateClient<EnvironmentProviderService.EnvironmentProviderServiceClient>("EnvironmentEnvironmentProvider");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default)
        {
            var initialiseResponse = _environmentWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken);
            initializationTasks.Add(initialiseResponse.ResponseAsync);
            return initialiseResponse;
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            return await _environmentWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            return await _environmentWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
        }

        public async Task<GetEnvironmentVariablesResponse> GetEnvironmentVariables(GetEnvironmentVariablesRequest getEnvironmentVariablesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getEnvironmentVariablesRequest.SimulationId, current, "GetEnvironmentVariables");
            return await _environmentEnvironmentProviderClient.GetEnvironmentVariablesAsync(getEnvironmentVariablesRequest, cancellationToken: cancellationToken);
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Environment.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
