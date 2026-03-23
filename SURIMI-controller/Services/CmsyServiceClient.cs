using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class CmsyServiceClient : ICmsyServiceClient
    {
        private readonly ILogger<CmsyServiceClient> _logger;
        private readonly WorkflowService.WorkflowServiceClient _cmsyWorkflowClient;
        private readonly EcologyConsumerService.EcologyConsumerServiceClient _cmsyEcologyConsumerClient;
        private readonly CatchConsumerService.CatchConsumerServiceClient _cmsyCatchConsumerClient;
        private readonly bool _includeCmsy = Environment.GetEnvironmentVariable("EXCLUDE_CMSY")?.ToLower() != "true";

        public CmsyServiceClient(GrpcClientFactory clientFactory, ILogger<CmsyServiceClient> logger)
        {
            _cmsyWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow");
            _cmsyEcologyConsumerClient = clientFactory.CreateClient<EcologyConsumerService.EcologyConsumerServiceClient>("CmsyEcologyConsumer");
            _cmsyCatchConsumerClient = clientFactory.CreateClient<CatchConsumerService.CatchConsumerServiceClient>("CmsyCatchConsumer");

            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_includeCmsy)
            {
                var initialiseResponse = _cmsyWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken);
                initializationTasks.Add(initialiseResponse.ResponseAsync);
                return initialiseResponse;
            }
            return null;
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            if (_includeCmsy)
            {
                return await _cmsyWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelResponse() { SimulationId = cancelRequest.SimulationId };
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            if (_includeCmsy)
            {
                return await _cmsyWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseResponse() { SimulationId = finaliseRequest.SimulationId };
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeCmsy)
            {
                LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
                return await _cmsyWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
            }
            return new SimulateStepResponse() { SimulationId = simulationStepRequest.SimulationId };
        }

        public async Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeCmsy)
            {
                LogStep(updateBiomassRequest.SimulationId, current, "UpdateBiomass");
                return await _cmsyEcologyConsumerClient.UpdateBiomassAsync(updateBiomassRequest, cancellationToken: cancellationToken);
            }
            return new UpdateBiomassResponse() { SimulationId = updateBiomassRequest.SimulationId };
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeCmsy)
            {
                LogStep(updateCatchDispositionRequest.SimulationId, current, "UpdateCatchDisposition");
                return await _cmsyCatchConsumerClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken);
            }
            return new UpdateCatchDispositionResponse() { SimulationId = updateCatchDispositionRequest.SimulationId };
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step CMSY.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
