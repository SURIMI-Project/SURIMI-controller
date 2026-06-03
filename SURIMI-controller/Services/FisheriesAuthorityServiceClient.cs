using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class FisheriesAuthorityServiceClient : IFisheriesAuthorityServiceClient
    {
        private readonly FisheriesAuthorityService.FisheriesAuthorityServiceClient _fisheriesAuthorityServiceClient;
        private readonly ILogger<FisheriesAuthorityServiceClient> _logger;

        public FisheriesAuthorityServiceClient(GrpcClientFactory clientFactory, ILogger<FisheriesAuthorityServiceClient> logger)
        {
            _fisheriesAuthorityServiceClient = clientFactory.CreateClient<FisheriesAuthorityService.FisheriesAuthorityServiceClient>("FisheriesAuthority");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseSimulationResponse>? AddInitialise(List<Task<InitialiseSimulationResponse>> initializationTasks, InitialiseSimulationRequest InitialiseSimulationRequest, CancellationToken cancellationToken = default)
        {
            var InitialiseSimulationResponse = _fisheriesAuthorityServiceClient.InitialiseSimulationAsync(InitialiseSimulationRequest, cancellationToken: cancellationToken);
            initializationTasks.Add(InitialiseSimulationResponse.ResponseAsync);
            return InitialiseSimulationResponse;
        }

        public async Task<CancelSimulationResponse> CancelSimulationAsync(CancelSimulationRequest cancelRequest, CancellationToken token)
        {
            return await _fisheriesAuthorityServiceClient.CancelSimulationAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<CreateRegulationsResponse> CreateRegulationsAsync(CreateRegulationsRequest createRegulationsRequest, CancellationToken cancellationToken)
        {
            LogStep(createRegulationsRequest.SimulationId, DateTime.UtcNow, "CreateRegulations");
            return await _fisheriesAuthorityServiceClient.CreateRegulationsAsync(createRegulationsRequest, cancellationToken: cancellationToken);
        }

        public async Task<FinaliseSimulationResponse> FinaliseSimulationAsync(FinaliseSimulationRequest finaliseSimulationRequest, CancellationToken cancellationToken = default)
        {
            LogStep(finaliseSimulationRequest.SimulationId, DateTime.UtcNow, "FinaliseSimulation");
            return await _fisheriesAuthorityServiceClient.FinaliseSimulationAsync(finaliseSimulationRequest, cancellationToken: cancellationToken);
        }

        public async Task<GetRegulationsResponse> GetRegulationsAsync(GetRegulationsRequest getRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getRegulationsRequest.SimulationId, current, "GetRegulations");
            return await _fisheriesAuthorityServiceClient.GetRegulationsAsync(getRegulationsRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return await _fisheriesAuthorityServiceClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateCatchDispositionRequest.SimulationId, current, "UpdateCatchDisposition");
            return await _fisheriesAuthorityServiceClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateFishingActivityResponse> UpdateFishingActivityAsync(UpdateFishingActivityRequest updateFishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateFishingActivityRequest.SimulationId, current, "UpdateFishingActivity");
            return await _fisheriesAuthorityServiceClient.UpdateFishingActivityAsync(updateFishingActivityRequest, cancellationToken: cancellationToken);
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step FisheriesAuthority.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
