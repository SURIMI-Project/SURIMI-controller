using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI.Datamodel;

namespace SurimiController.Services
{
    public class EnvironmentServiceClient : IEnvironmentServiceClient
    {
        private readonly WorkflowService.WorkflowServiceClient _workflowClient;
        private readonly EnvironmentProviderService.EnvironmentProviderServiceClient _environmentProviderClient;
        private readonly ILogger<EnvironmentServiceClient> _logger;

        public EnvironmentServiceClient(GrpcClientFactory clientFactory, ILogger<EnvironmentServiceClient> logger)
        {
            _workflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EnvironmentWorkflow");
            _environmentProviderClient = clientFactory.CreateClient<EnvironmentProviderService.EnvironmentProviderServiceClient>("EnvironmentEnvironmentProvider");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default)
        {
            var initialiseResponse = _workflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken);
            initializationTasks.Add(initialiseResponse.ResponseAsync);
            return initialiseResponse;
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            return await _workflowClient.CancelAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            return await _workflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            return await _workflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<GetEnvironmentVariablesResponse> GetEnvironmentVariables(GetEnvironmentVariablesRequest getEnvironmentVariablesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getEnvironmentVariablesRequest.SimulationId, current, "GetEnvironmentVariables");
            return await _environmentProviderClient.GetEnvironmentVariablesAsync(getEnvironmentVariablesRequest, cancellationToken: cancellationToken);
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Environment.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
