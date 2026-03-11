using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI.Datamodel;

namespace SurimiController.Services
{
    public class FisheriesAuthorityServiceClient : IFisheriesAuthorityServiceClient
    {
        private readonly WorkflowService.WorkflowServiceClient _workflowClient;
        private readonly RegulationsProviderService.RegulationsProviderServiceClient _regulationsProviderClient;
        private readonly CatchConsumerService.CatchConsumerServiceClient _catchConsumerClient;
        private readonly ILogger<FisheriesAuthorityServiceClient> _logger;

        public FisheriesAuthorityServiceClient(GrpcClientFactory clientFactory, ILogger<FisheriesAuthorityServiceClient> logger)
        {
            _workflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("FisheriesAuthorityWorkflow");
            _regulationsProviderClient = clientFactory.CreateClient<RegulationsProviderService.RegulationsProviderServiceClient>("FisheriesAuthorityRegulationsProvider");
            _catchConsumerClient = clientFactory.CreateClient<CatchConsumerService.CatchConsumerServiceClient>("FisheriesAuthorityCatchConsumer");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default)
        {
            var initialiseResponse = _workflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken);
            initializationTasks.Add(initialiseResponse.ResponseAsync);
            return initialiseResponse;
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            return await _workflowClient.CancelAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<CreateRegulationsResponse> CreateRegulationsAsync(CreateRegulationsRequest createRegulationsRequest, CancellationToken cancellationToken)
        {
            LogStep(createRegulationsRequest.SimulationId, DateTime.UtcNow, "CreateRegulations");
            return await _regulationsProviderClient.CreateRegulationsAsync(createRegulationsRequest, cancellationToken: cancellationToken);
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            return await _workflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
        }

        public async Task<GetRegulationsResponse> GetRegulationsAsync(GetRegulationsRequest getRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getRegulationsRequest.SimulationId, current, "GetRegulations");
            return await _regulationsProviderClient.GetRegulationsAsync(getRegulationsRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return await _workflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateCatchDispositionRequest.SimulationId, current, "UpdateCatchDisposition");
            return await _catchConsumerClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateFishingActivityResponse> UpdateFishingActivityAsync(UpdateFishingActivityRequest updateFishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateFishingActivityRequest.SimulationId, current, "UpdateFishingActivity");
            return await _regulationsProviderClient.UpdateFishingActivityAsync(updateFishingActivityRequest, cancellationToken: cancellationToken);
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step FisheriesAuthority.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
