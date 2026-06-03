using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class PoseidonServiceClient : IPoseidonServiceClient
    {
        private readonly FisheryService.FisheryServiceClient _fisheryClient;

        private readonly ILogger<PoseidonServiceClient> _logger;

        public PoseidonServiceClient(GrpcClientFactory clientFactory, ILogger<PoseidonServiceClient> logger)
        {
            _fisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("Poseidon");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initializationTasks, InitialiseSimulationRequest InitialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var InitialiseSimulationResponse = _fisheryClient.InitialiseSimulationAsync(InitialiseSimulationRequest, cancellationToken: cancellationToken);
            initializationTasks.Add(InitialiseSimulationResponse.ResponseAsync);
            return InitialiseSimulationResponse;
        }

        public Task<CancelSimulationResponse> CancelSimulationAsync(CancelSimulationRequest cancelRequest, CancellationToken token)
        {
            return _fisheryClient.CancelSimulationAsync(cancelRequest, cancellationToken: token).ResponseAsync;
        }

        public Task<FinaliseSimulationResponse> FinaliseSimulationAsync(FinaliseSimulationRequest finaliseSimulationRequest, CancellationToken cancellationToken = default)
        {
            return _fisheryClient.FinaliseSimulationAsync(finaliseSimulationRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getCatchDispositionRequest.SimulationId, current, "GetCatchDisposition");
            return _fisheryClient.GetCatchDispositionAsync(getCatchDispositionRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetFishingActivityResponse> GetFishingActivityAsync(GetFishingActivityRequest fishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(fishingActivityRequest.SimulationId, current, "GetFishingActivity");
            return _fisheryClient.GetFishingActivityAsync(fishingActivityRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSalesRequest.SimulationId, current, "GetSalesSummary");
            return _fisheryClient.GetSalesAsync(getSalesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return _fisheryClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateBiomassRequest.SimulationId, current, "UpdateBiomass");
            return _fisheryClient.UpdateBiomassAsync(updateBiomassRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateRegulationsResponse> UpdateRegulationsAsync(UpdateRegulationsRequest updateRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateRegulationsRequest.SimulationId, current, "UpdateRegulations");
            return _fisheryClient.UpdateRegulationsAsync(updateRegulationsRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSpeciesPricesRequest.SimulationId, current, "UpdatePrices");
            return _fisheryClient.UpdateSpeciesPricesAsync(updateSpeciesPricesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Poseidon.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
