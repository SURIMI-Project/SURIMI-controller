using Grpc.Core;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class EcopathServiceClient(ILogger<EcopathServiceClient> logger, SimulationDispatcher ecopathSimDispatcher)
        : GrpcServiceClientBase<EcopathServiceClient>(logger), IEcopathServiceClient
    {
        private readonly SimulationDispatcher _ecopathSimDispatcher = ecopathSimDispatcher;

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initialisationTasks, InitialiseSimulationRequest InitialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var InitialiseSimulationResponse = _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, InitialiseSimulationRequest, InitialiseSimulationResponse>(InitialiseSimulationRequest, InitialiseSimulationRequest.SimulationId,
                (client, req) => client.InitialiseSimulationAsync(req));
            initialisationTasks.Add(InitialiseSimulationResponse.ResponseAsync);
            return InitialiseSimulationResponse;
        }

        public async Task<CancelSimulationResponse> CancelSimulationAsync(CancelSimulationRequest cancelRequest, CancellationToken token)
        {
            var ecopathCancelSimulationResponse = await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, CancelSimulationRequest, CancelSimulationResponse>(cancelRequest, cancelRequest.SimulationId,
                (client, req) => client.CancelSimulationAsync(req));

            _ecopathSimDispatcher.ReleasePodFromSimulation(cancelRequest.SimulationId);
            return ecopathCancelSimulationResponse;
        }
        public async Task<FinaliseSimulationResponse> FinaliseSimulationAsync(FinaliseSimulationRequest finaliseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var _finaliseResponse = await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, FinaliseSimulationRequest, FinaliseSimulationResponse>(finaliseSimulationRequest, finaliseSimulationRequest.SimulationId,
                (client, req) => client.FinaliseSimulationAsync(req));
            _ecopathSimDispatcher.ReleasePodFromSimulation(finaliseSimulationRequest.SimulationId);
            return _finaliseResponse;
        }

        public async Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getCatchDispositionRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, GetCatchDispositionRequest, GetCatchDispositionResponse>(getCatchDispositionRequest, getCatchDispositionRequest.SimulationId,
                (client, req) => client.GetCatchDispositionAsync(req, cancellationToken: cancellationToken));
        }

        public async Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSalesRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, GetSalesRequest, GetSalesResponse>(getSalesRequest, getSalesRequest.SimulationId,
                (client, req) => client.GetSalesAsync(req, cancellationToken: cancellationToken));
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, SimulateStepRequest, SimulateStepResponse>(simulationStepRequest, simulationStepRequest.SimulationId,
                (client, req) => client.SimulateStepAsync(req, cancellationToken: cancellationToken));
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateCatchDispositionRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, UpdateCatchDispositionRequest, UpdateCatchDispositionResponse>(updateCatchDispositionRequest, updateCatchDispositionRequest.SimulationId,
                (client, req) => client.UpdateCatchDispositionAsync(req, cancellationToken: cancellationToken));
        }

        public async Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSpeciesPricesRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, UpdateSpeciesPricesRequest, UpdateSpeciesPricesResponse>(updateSpeciesPricesRequest, updateSpeciesPricesRequest.SimulationId,
                (client, req) => client.UpdateSpeciesPricesAsync(req, cancellationToken: cancellationToken));
        }

        public async Task<GetBiomassResponse> GetBiomassAsync(GetBiomassRequest getBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getBiomassRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(getBiomassRequest, getBiomassRequest.SimulationId,
                (client, req) => client.GetBiomassAsync(req, cancellationToken: cancellationToken));
        }

        public async Task<UpdateEnvironmentVariablesResponse> UpdateEnvironmentVariablesAsync(UpdateEnvironmentVariablesRequest updateEnvironmentVariablesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateEnvironmentVariablesRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, UpdateEnvironmentVariablesRequest, UpdateEnvironmentVariablesResponse>(updateEnvironmentVariablesRequest, updateEnvironmentVariablesRequest.SimulationId,
                (client, req) => client.UpdateEnvironmentVariablesAsync(req, cancellationToken: cancellationToken));
        }

        public async Task<UpdateRegulationsResponse> UpdateRegulationsAsync(UpdateRegulationsRequest updateRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateRegulationsRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, UpdateRegulationsRequest, UpdateRegulationsResponse>(updateRegulationsRequest, updateRegulationsRequest.SimulationId,
                (client, req) => client.UpdateRegulationsAsync(req, cancellationToken: cancellationToken));
        }

        public async Task<GetFishingActivityResponse> GetFishingActivityAsync(GetFishingActivityRequest getFishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getFishingActivityRequest.SimulationId, current);
            return await _ecopathSimDispatcher.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, GetFishingActivityRequest, GetFishingActivityResponse>(getFishingActivityRequest, getFishingActivityRequest.SimulationId,
                (client, req) => client.GetFishingActivityAsync(req, cancellationToken: cancellationToken));
        }
    }
}
