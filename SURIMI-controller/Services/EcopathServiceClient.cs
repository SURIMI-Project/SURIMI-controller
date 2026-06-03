using Grpc.Core;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class EcopathServiceClient : IEcopathServiceClient
    {
        private readonly ILogger<MarketServiceClient> _logger;
        private readonly SimulationDispatcher _ecopathSimDispatcher;

        public EcopathServiceClient(ILogger<MarketServiceClient> logger, SimulationDispatcher ecopathSimDispatcher)
        {
            _logger = logger;
            _ecopathSimDispatcher = ecopathSimDispatcher;
        }

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initializationTasks, InitialiseSimulationRequest InitialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var InitialiseSimulationResponse = _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, InitialiseSimulationRequest, InitialiseSimulationResponse>(InitialiseSimulationRequest, InitialiseSimulationRequest.SimulationId,
                (client, req) => client.InitialiseSimulationAsync(req));
            initializationTasks.Add(InitialiseSimulationResponse.ResponseAsync);
            return InitialiseSimulationResponse;
        }

        public async Task<CancelSimulationResponse> CancelSimulationAsync(CancelSimulationRequest cancelRequest, CancellationToken token)
        {
            var ecopathCancelSimulationResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, CancelSimulationRequest, CancelSimulationResponse>(cancelRequest, cancelRequest.SimulationId,
                (client, req) => client.CancelSimulationAsync(req));

            _ecopathSimDispatcher.ReleasePodFromSimulation(cancelRequest.SimulationId);
            return ecopathCancelSimulationResponse;
        }
        public async Task<FinaliseSimulationResponse> FinaliseSimulationAsync(FinaliseSimulationRequest finaliseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var _finaliseResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, FinaliseSimulationRequest, FinaliseSimulationResponse>(finaliseSimulationRequest, finaliseSimulationRequest.SimulationId,
                (client, req) => client.FinaliseSimulationAsync(req));
            return _finaliseResponse;
        }

        public async Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getCatchDispositionRequest.SimulationId, current, "GetCatchDisposition");
            var ecopathCatchDispositionSummary = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetCatchDispositionRequest, GetCatchDispositionResponse>(getCatchDispositionRequest, getCatchDispositionRequest.SimulationId,
            (client, req) => client.GetCatchDispositionAsync(req, cancellationToken: cancellationToken));
            return ecopathCatchDispositionSummary;
        }

        public async Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSalesRequest.SimulationId, current, "GetSales");
            var ecopathGetSalesResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetSalesRequest, GetSalesResponse>(getSalesRequest, getSalesRequest.SimulationId,
                (client, req) => client.GetSalesAsync(req, cancellationToken: cancellationToken));
            return ecopathGetSalesResponse;
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "Ecopath.SimulateStep");
            var simulateStep = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, SimulateStepRequest, SimulateStepResponse>(simulationStepRequest, simulationStepRequest.SimulationId,
                (client, req) => client.SimulateStepAsync(req, cancellationToken: cancellationToken));
            return simulateStep;
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateCatchDispositionRequest.SimulationId, current, "UpdateCatchDisposition Summary");
            var catchDispositionResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, UpdateCatchDispositionRequest, UpdateCatchDispositionResponse>(updateCatchDispositionRequest, updateCatchDispositionRequest.SimulationId,
              (client, req) => client.UpdateCatchDispositionAsync(req, cancellationToken: cancellationToken));
            return catchDispositionResponse;
        }

        public async Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSpeciesPricesRequest.SimulationId, current, "UpdateSpeciesPrices");
            var ecopathUpdatePricesResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, UpdateSpeciesPricesRequest, UpdateSpeciesPricesResponse>(updateSpeciesPricesRequest, updateSpeciesPricesRequest.SimulationId,
               (client, req) => client.UpdateSpeciesPricesAsync(req, cancellationToken: cancellationToken));
            return ecopathUpdatePricesResponse;
        }

        public async Task<GetBiomassResponse> GetBiomassAsync(GetBiomassRequest getBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getBiomassRequest.SimulationId, current, "GetBiomass");
            var getBiomassResponseIntermediate = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(getBiomassRequest, getBiomassRequest.SimulationId,
                (client, req) => client.GetBiomassAsync(req, cancellationToken: cancellationToken));
            return getBiomassResponseIntermediate;
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Ecopath.{Step}. {DateTime}", simulationId, step, current);
        }

        public async Task<UpdateEnvironmentVariablesResponse> UpdateEnvironmentVariablesAsync(UpdateEnvironmentVariablesRequest updateEnvironmentVariablesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateEnvironmentVariablesRequest.SimulationId, current, "UpdateEnvironmentVariables");
            var response = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, UpdateEnvironmentVariablesRequest, UpdateEnvironmentVariablesResponse>(updateEnvironmentVariablesRequest, updateEnvironmentVariablesRequest.SimulationId,
                (client, req) => client.UpdateEnvironmentVariablesAsync(req, cancellationToken: cancellationToken));
            return response;
        }

        public async Task<UpdateRegulationsResponse> UpdateRegulationsAsync(UpdateRegulationsRequest updateRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateRegulationsRequest.SimulationId, current, "UpdateRegulations");
            var response = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, UpdateRegulationsRequest, UpdateRegulationsResponse>(updateRegulationsRequest, updateRegulationsRequest.SimulationId,
                (client, req) => client.UpdateRegulationsAsync(req, cancellationToken: cancellationToken));
            return response;
        }

        public async Task<GetFishingActivityResponse> GetFishingActivityAsync(GetFishingActivityRequest getFishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getFishingActivityRequest.SimulationId, current, "GetFishingActivity");
            var response = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetFishingActivityRequest, GetFishingActivityResponse>(getFishingActivityRequest, getFishingActivityRequest.SimulationId,
                (client, req) => client.GetFishingActivityAsync(req, cancellationToken: cancellationToken));
            return response;
        }
    }
}
