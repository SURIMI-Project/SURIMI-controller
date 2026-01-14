using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI.Datamodel;
using SurimiController.Models;

namespace SurimiController.Services
{
    public class MarketServiceClient : IMarketServiceClient
    {
        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;
        private readonly MarketService.MarketServiceClient _marketMarketClient;
        private readonly ILogger<MarketServiceClient> _logger;

        public MarketServiceClient(GrpcClientFactory clientFactory, ILogger<MarketServiceClient> logger)
        {
            _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");
            _marketMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("MarketMarket");
            _logger = logger;
        }

        public void AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default)
        {
            initializationTasks.Add(_marketWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken).ResponseAsync);
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
            return await _marketMarketClient.GetSpeciesPricesAsync(getSpeciesPricesRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSalesRequest.SimulationId, current, "UpdateSales");
            return await _marketMarketClient.UpdateSalesAsync(updateSalesRequest, cancellationToken: cancellationToken);
        }

        public Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSpeciesPricesRequest.SimulationId, current, "UpdateSpeciesPrices");
            return _marketMarketClient.UpdateSpeciesPricesAsync(updateSpeciesPricesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Market.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
