using System.Collections.Concurrent;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class PoseidonServiceClient : IPoseidonServiceClient
    {
        private readonly FisheryService.FisheryServiceClient _fisheryClient;
        private readonly ILogger<PoseidonServiceClient> _logger;
        private readonly ConcurrentDictionary<string, bool> _usePoseidon = new();

        public PoseidonServiceClient(GrpcClientFactory clientFactory, ILogger<PoseidonServiceClient> logger)
        {
            _fisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("Poseidon");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initializationTasks, InitialiseSimulationRequest InitialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var hasPoseidon = InitialiseSimulationRequest.Simulation?.Items?.FleetSegments
                .Any(fs => fs.Model == "POSEIDON") ?? false;

            _usePoseidon[InitialiseSimulationRequest.SimulationId] = hasPoseidon;

            if (hasPoseidon)
            {
                var InitialiseSimulationResponse = _fisheryClient.InitialiseSimulationAsync(InitialiseSimulationRequest, cancellationToken: cancellationToken);
                initializationTasks.Add(InitialiseSimulationResponse.ResponseAsync);
                LogStep(InitialiseSimulationRequest.SimulationId, DateTime.UtcNow, "AddInitialise (InitialiseSimulation)");
                return InitialiseSimulationResponse;
            }

            _logger.LogInformation("{SimulationId} has no POSEIDON fleet segments, skipping Poseidon initialisation", InitialiseSimulationRequest.SimulationId);
            return null;
        }

        public Task<CancelSimulationResponse> CancelSimulationAsync(CancelSimulationRequest cancelRequest, CancellationToken token)
        {
            if (!IsPoseidonEnabled(cancelRequest.SimulationId))
                return Task.FromResult(new CancelSimulationResponse() { SimulationId = cancelRequest.SimulationId });
            LogStep(cancelRequest.SimulationId, DateTime.UtcNow, "CancelSimulation");
            return _fisheryClient.CancelSimulationAsync(cancelRequest, cancellationToken: token).ResponseAsync;
        }

        public Task<FinaliseSimulationResponse> FinaliseSimulationAsync(FinaliseSimulationRequest finaliseSimulationRequest, CancellationToken cancellationToken = default)
        {
            if (!IsPoseidonEnabled(finaliseSimulationRequest.SimulationId))
                return Task.FromResult(new FinaliseSimulationResponse() { SimulationId = finaliseSimulationRequest.SimulationId });
            LogStep(finaliseSimulationRequest.SimulationId, DateTime.UtcNow, "FinaliseSimulation");
            return _fisheryClient.FinaliseSimulationAsync(finaliseSimulationRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (!IsPoseidonEnabled(getCatchDispositionRequest.SimulationId))
                return Task.FromResult(new GetCatchDispositionResponse() { SimulationId = getCatchDispositionRequest.SimulationId });
            LogStep(getCatchDispositionRequest.SimulationId, current, "GetCatchDisposition");
            return _fisheryClient.GetCatchDispositionAsync(getCatchDispositionRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetFishingActivityResponse> GetFishingActivityAsync(GetFishingActivityRequest fishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (!IsPoseidonEnabled(fishingActivityRequest.SimulationId))
                return Task.FromResult(new GetFishingActivityResponse() { SimulationId = fishingActivityRequest.SimulationId });
            LogStep(fishingActivityRequest.SimulationId, current, "GetFishingActivity");
            return _fisheryClient.GetFishingActivityAsync(fishingActivityRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (!IsPoseidonEnabled(getSalesRequest.SimulationId))
                return Task.FromResult(new GetSalesResponse() { SimulationId = getSalesRequest.SimulationId });
            LogStep(getSalesRequest.SimulationId, current, "GetSalesSummary");
            return _fisheryClient.GetSalesAsync(getSalesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (!IsPoseidonEnabled(simulationStepRequest.SimulationId))
                return Task.FromResult(new SimulateStepResponse() { SimulationId = simulationStepRequest.SimulationId });
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return _fisheryClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (!IsPoseidonEnabled(updateBiomassRequest.SimulationId))
                return Task.FromResult(new UpdateBiomassResponse() { SimulationId = updateBiomassRequest.SimulationId });
            LogStep(updateBiomassRequest.SimulationId, current, "UpdateBiomass");
            return _fisheryClient.UpdateBiomassAsync(updateBiomassRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateRegulationsResponse> UpdateRegulationsAsync(UpdateRegulationsRequest updateRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (!IsPoseidonEnabled(updateRegulationsRequest.SimulationId))
                return Task.FromResult(new UpdateRegulationsResponse() { SimulationId = updateRegulationsRequest.SimulationId });
            LogStep(updateRegulationsRequest.SimulationId, current, "UpdateRegulations");
            return _fisheryClient.UpdateRegulationsAsync(updateRegulationsRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (!IsPoseidonEnabled(updateSpeciesPricesRequest.SimulationId))
                return Task.FromResult(new UpdateSpeciesPricesResponse() { SimulationId = updateSpeciesPricesRequest.SimulationId });
            LogStep(updateSpeciesPricesRequest.SimulationId, current, "UpdatePrices");
            return _fisheryClient.UpdateSpeciesPricesAsync(updateSpeciesPricesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        private bool IsPoseidonEnabled(string simulationId) =>
            _usePoseidon.TryGetValue(simulationId, out var enabled) && enabled;

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Poseidon.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
