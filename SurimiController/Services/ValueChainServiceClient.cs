using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class ValueChainServiceClient : IValueChainServiceClient
    {
        private readonly ILogger<ValueChainServiceClient> _logger;
        private readonly WorkflowService.WorkflowServiceClient _valueChainWorkflowClient;
        private readonly MarketProviderService.MarketProviderServiceClient _valueChainMarketProviderClient;
        private readonly bool _includeValueChain = Environment.GetEnvironmentVariable("EXCLUDE_VALUECHAIN")?.ToLower() != "true";

        public ValueChainServiceClient(GrpcClientFactory clientFactory, ILogger<ValueChainServiceClient> logger)
        {
            _valueChainWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("ValueChainWorkflow");
            _valueChainMarketProviderClient = clientFactory.CreateClient<MarketProviderService.MarketProviderServiceClient>("ValueChainMarketProvider");

            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_includeValueChain)
            {
                var initialiseResponse = _valueChainWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken);
                initializationTasks.Add(initialiseResponse.ResponseAsync);
                return initialiseResponse;
            }
            return null;
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            if (_includeValueChain)
            {
                return await _valueChainWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelResponse() { SimulationId = cancelRequest.SimulationId };
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            if (_includeValueChain)
            {
                return await _valueChainWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseResponse() { SimulationId = finaliseRequest.SimulationId };
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeValueChain)
            {
                LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
                return await _valueChainWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
            }
            return new SimulateStepResponse() { SimulationId = simulationStepRequest.SimulationId };
        }

        public async Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeValueChain)
            {
                LogStep(updateSalesRequest.SimulationId, current, "UpdateSales");
                var valueChainUpdateSalesResponse = await _valueChainMarketProviderClient.UpdateSalesAsync(updateSalesRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSalesResponse() { SimulationId = updateSalesRequest.SimulationId };
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step ValueChain.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
