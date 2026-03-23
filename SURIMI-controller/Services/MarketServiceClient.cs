using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class MarketServiceClient : IMarketServiceClient
    {
        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;
        private readonly MarketProviderService.MarketProviderServiceClient _marketMarketProviderClient;
        private readonly ILogger<MarketServiceClient> _logger;

        public MarketServiceClient(GrpcClientFactory clientFactory, ILogger<MarketServiceClient> logger)
        {
            _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");
            _marketMarketProviderClient = clientFactory.CreateClient<MarketProviderService.MarketProviderServiceClient>("MarketMarketProvider");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default)
        {
            var initialiseResponse = _marketWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken);
            initializationTasks.Add(initialiseResponse.ResponseAsync);
            return initialiseResponse;
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            return await _marketWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            return await _marketWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
        }

        public async Task<GetSpeciesPricesResponse> GetSpeciesPricesAsync(GetSpeciesPricesRequest getSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSpeciesPricesRequest.SimulationId, current, "GetSpeciesPrices");
            return await _marketMarketProviderClient.GetSpeciesPricesAsync(getSpeciesPricesRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSalesRequest.SimulationId, current, "UpdateSales");
            return await _marketMarketProviderClient.UpdateSalesAsync(updateSalesRequest, cancellationToken: cancellationToken);
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Market.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
