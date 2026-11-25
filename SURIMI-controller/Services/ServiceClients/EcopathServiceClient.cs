using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI.Datamodel;

namespace SURIMI_controller.Services
{
    public class EcopathServiceClient(GrpcClientFactory clientFactory, ILogger<EcopathServiceClient> logger)
        : GrpcServiceClientBase<EcopathServiceClient>(logger), IEcopathServiceClient
    {
        private readonly EcologyService.EcologyServiceClient _ecologyClient =
            clientFactory.CreateClient<EcologyService.EcologyServiceClient>("Ecopath");

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initialisationTasks, InitialiseSimulationRequest initialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var initialiseSimulationResponse = _ecologyClient.InitialiseSimulationAsync(initialiseSimulationRequest, cancellationToken: cancellationToken);
            initialisationTasks.Add(initialiseSimulationResponse.ResponseAsync);
            return initialiseSimulationResponse;
        }

        public Task<GetInitialHCRDataResponse> GetInitialHCRDataAsync(GetInitialHCRDataRequest request, CancellationToken cancellationToken)
        {
            return _ecologyClient.GetInitialHCRDataAsync(request, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<CancelSimulationResponse> CancelSimulationAsync(CancelSimulationRequest cancelRequest, CancellationToken token)
        {
            return _ecologyClient.CancelSimulationAsync(cancelRequest, cancellationToken: token).ResponseAsync;
        }

        public Task<FinaliseSimulationResponse> FinaliseSimulationAsync(FinaliseSimulationRequest finaliseSimulationRequest, CancellationToken cancellationToken = default)
        {
            return _ecologyClient.FinaliseSimulationAsync(finaliseSimulationRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getCatchDispositionRequest.SimulationId, current);
            return _ecologyClient.GetCatchDispositionAsync(getCatchDispositionRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSalesRequest.SimulationId, current);
            return _ecologyClient.GetSalesAsync(getSalesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current);
            return _ecologyClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateCatchDispositionRequest.SimulationId, current);
            return _ecologyClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSpeciesPricesRequest.SimulationId, current);
            return _ecologyClient.UpdateSpeciesPricesAsync(updateSpeciesPricesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetBiomassResponse> GetBiomassAsync(GetBiomassRequest getBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getBiomassRequest.SimulationId, current);
            return _ecologyClient.GetBiomassAsync(getBiomassRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateRegulationsResponse> UpdateRegulationsAsync(UpdateRegulationsRequest updateRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateRegulationsRequest.SimulationId, current);
            return _ecologyClient.UpdateRegulationsAsync(updateRegulationsRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetFishingActivityResponse> GetFishingActivityAsync(GetFishingActivityRequest getFishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getFishingActivityRequest.SimulationId, current);
            return _ecologyClient.GetFishingActivityAsync(getFishingActivityRequest, cancellationToken: cancellationToken).ResponseAsync;
        }
    }
}
