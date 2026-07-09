using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class MarketServiceClient : IMarketServiceClient
    {
        private readonly MarketService.MarketServiceClient _marketClient;
        private readonly ILogger<MarketServiceClient> _logger;

        public MarketServiceClient(GrpcClientFactory clientFactory, ILogger<MarketServiceClient> logger)
        {
            _marketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("Market");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initializationTasks, InitialiseSimulationRequest InitialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var InitialiseSimulationResponse = _marketClient.InitialiseSimulationAsync(InitialiseSimulationRequest, cancellationToken: cancellationToken);
            initializationTasks.Add(InitialiseSimulationResponse.ResponseAsync);
            return InitialiseSimulationResponse;
        }

        public async Task<CancelSimulationResponse> CancelSimulationAsync(CancelSimulationRequest cancelRequest, CancellationToken token)
        {
            return await _marketClient.CancelSimulationAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<FinaliseSimulationResponse> FinaliseSimulationAsync(FinaliseSimulationRequest finaliseSimulationRequest, CancellationToken cancellationToken = default)
        {
            return await _marketClient.FinaliseSimulationAsync(finaliseSimulationRequest, cancellationToken: cancellationToken);
        }

        public async Task<GetSpeciesPricesResponse> GetSpeciesPricesAsync(GetSpeciesPricesRequest getSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSpeciesPricesRequest.SimulationId, current, "GetSpeciesPrices");
            return await _marketClient.GetSpeciesPricesAsync(getSpeciesPricesRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return await _marketClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSalesRequest.SimulationId, current, "UpdateSales");
            return await _marketClient.UpdateSalesAsync(updateSalesRequest, cancellationToken: cancellationToken);
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Market.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
