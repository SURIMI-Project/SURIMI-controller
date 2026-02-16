using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class AggregatorServiceClient : IAggregatorServiceClient
    {
        private readonly ILogger<AggregatorServiceClient> _logger;
        private readonly WorkflowService.WorkflowServiceClient _aggregatorWorkflowClient;
        private readonly EcologyService.EcologyServiceClient _aggregatorEcologyClient;
        private readonly FisheryService.FisheryServiceClient _aggregatorFisheryClient;
        private readonly bool _includeAggregator = Environment.GetEnvironmentVariable("EXCLUDE_AGGREGATOR")?.ToLower() != "true";

        public AggregatorServiceClient(GrpcClientFactory clientFactory, ILogger<AggregatorServiceClient> logger)
        {
            _aggregatorWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("AggregatorWorkflow");
            _aggregatorEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("AggregatorEcology");
            _aggregatorFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("AggregatorFishery");

            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_includeAggregator)
            {
                var initialiseResponse = _aggregatorWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken);
                initializationTasks.Add(initialiseResponse.ResponseAsync);
                return initialiseResponse;
            }
            return null;
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            if (_includeAggregator)
            {
                return await _aggregatorWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelResponse();
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            if (_includeAggregator)
            {
                return await _aggregatorWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseResponse();
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeAggregator)
            {
                LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
                return await _aggregatorWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
            }
            return new SimulateStepResponse();
        }

        public async Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeAggregator)
            {
                LogStep(updateBiomassRequest.SimulationId, current, "UpdateBiomass");
                return await _aggregatorEcologyClient.UpdateBiomassAsync(updateBiomassRequest, cancellationToken: cancellationToken);
            }
            return new UpdateBiomassResponse();
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, CancellationToken cancellationToken)
        {
            if (_includeAggregator)
            {
                LogStep(updateCatchDispositionRequest.SimulationId, updateCatchDispositionRequest.StartDateTime.ToDateTime(), "UpdateCatchDisposition");
                return await _aggregatorFisheryClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken);
            }
            return new UpdateCatchDispositionResponse();
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Aggregator.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
