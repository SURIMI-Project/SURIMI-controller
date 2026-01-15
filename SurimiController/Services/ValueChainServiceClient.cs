using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class ValueChainServiceClient : IValueChainServiceClient
    {
        private readonly ILogger<ValueChainServiceClient> _logger;
        private readonly WorkflowService.WorkflowServiceClient _valueChainWorkflowClient;
        private readonly MarketService.MarketServiceClient _valueChainMarketClient;
        private readonly bool _includeValueChain = Environment.GetEnvironmentVariable("EXCLUDE_VALUECHAIN")?.ToLower() != "true";

        public ValueChainServiceClient(GrpcClientFactory clientFactory, ILogger<ValueChainServiceClient> logger)
        {
            _valueChainWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("ValueChainWorkflow");
            _valueChainMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("ValueChainMarket");

            _logger = logger;
        }

        public void AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_includeValueChain)
            {
                initializationTasks.Add(_valueChainWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken).ResponseAsync);
            }
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            if (_includeValueChain)
            {
                return await _valueChainWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelResponse();
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            if (_includeValueChain)
            {
                return await _valueChainWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseResponse();
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeValueChain)
            {
                LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
                return await _valueChainWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
            }
            return new SimulateStepResponse();
        }

        public async Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeValueChain)
            {
                LogStep(updateSalesRequest.SimulationId, current, "UpdateSales");
                var valueChainUpdateSalesResponse = await _valueChainMarketClient.UpdateSalesAsync(updateSalesRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSalesResponse();
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step ValueChain.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
