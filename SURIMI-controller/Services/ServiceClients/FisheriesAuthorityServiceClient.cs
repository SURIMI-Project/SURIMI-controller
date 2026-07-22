using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class FisheriesAuthorityServiceClient(GrpcClientFactory clientFactory, ILogger<FisheriesAuthorityServiceClient> logger)
        : GrpcServiceClientBase<FisheriesAuthorityServiceClient>(logger), IFisheriesAuthorityServiceClient
    {
        private readonly FisheriesAuthorityService.FisheriesAuthorityServiceClient _fisheriesAuthorityServiceClient =
            clientFactory.CreateClient<FisheriesAuthorityService.FisheriesAuthorityServiceClient>("FisheriesAuthority");

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initialisationTasks, InitialiseSimulationRequest InitialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var InitialiseSimulationResponse = _fisheriesAuthorityServiceClient.InitialiseSimulationAsync(InitialiseSimulationRequest, cancellationToken: cancellationToken);
            initialisationTasks.Add(InitialiseSimulationResponse.ResponseAsync);
            return InitialiseSimulationResponse;
        }

        public async Task<CancelSimulationResponse> CancelSimulationAsync(CancelSimulationRequest cancelRequest, CancellationToken token)
        {
            return await _fisheriesAuthorityServiceClient.CancelSimulationAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<CreateRegulationsResponse> CreateRegulationsAsync(CreateRegulationsRequest createRegulationsRequest, CancellationToken cancellationToken)
        {
            LogStep(createRegulationsRequest.SimulationId, DateTime.UtcNow);
            return await _fisheriesAuthorityServiceClient.CreateRegulationsAsync(createRegulationsRequest, cancellationToken: cancellationToken);
        }

        public async Task<FinaliseSimulationResponse> FinaliseSimulationAsync(FinaliseSimulationRequest finaliseSimulationRequest, CancellationToken cancellationToken = default)
        {
            LogStep(finaliseSimulationRequest.SimulationId, DateTime.UtcNow);
            return await _fisheriesAuthorityServiceClient.FinaliseSimulationAsync(finaliseSimulationRequest, cancellationToken: cancellationToken);
        }

        public async Task<GetRegulationsResponse> GetRegulationsAsync(GetRegulationsRequest getRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getRegulationsRequest.SimulationId, current);
            return await _fisheriesAuthorityServiceClient.GetRegulationsAsync(getRegulationsRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current);
            return await _fisheriesAuthorityServiceClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassTotalRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateBiomassTotalRequest.SimulationId, current);
            return await _fisheriesAuthorityServiceClient.UpdateBiomassAsync(updateBiomassTotalRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateCatchDispositionRequest.SimulationId, current);
            return await _fisheriesAuthorityServiceClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateFishingActivityResponse> UpdateFishingActivityAsync(UpdateFishingActivityRequest updateFishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateFishingActivityRequest.SimulationId, current);
            return await _fisheriesAuthorityServiceClient.UpdateFishingActivityAsync(updateFishingActivityRequest, cancellationToken: cancellationToken);
        }
    }
}
