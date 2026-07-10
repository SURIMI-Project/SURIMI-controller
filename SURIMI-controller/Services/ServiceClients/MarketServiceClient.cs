using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class MarketServiceClient(GrpcClientFactory clientFactory, ILogger<MarketServiceClient> logger)
        : GrpcServiceClientBase<MarketServiceClient>(logger), IMarketServiceClient
    {
        private readonly MarketService.MarketServiceClient _marketClient =
            clientFactory.CreateClient<MarketService.MarketServiceClient>("Market");

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initialisationTasks, InitialiseSimulationRequest InitialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var InitialiseSimulationResponse = _marketClient.InitialiseSimulationAsync(InitialiseSimulationRequest, cancellationToken: cancellationToken);
            initialisationTasks.Add(InitialiseSimulationResponse.ResponseAsync);
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
            LogStep(getSpeciesPricesRequest.SimulationId, current);
            return await _marketClient.GetSpeciesPricesAsync(getSpeciesPricesRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current);
            return await _marketClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSalesRequest.SimulationId, current);
            return await _marketClient.UpdateSalesAsync(updateSalesRequest, cancellationToken: cancellationToken);
        }
    }
}
