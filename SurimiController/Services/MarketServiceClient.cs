using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface IMarketServiceClient : IWorkflowService, IMarketService
    {
    }

    public class MarketServiceClient : IMarketServiceClient
    {
        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;
        private readonly MarketService.MarketServiceClient _marketMarketClient;
        private readonly ILogger<MarketServiceClient> _logger;

        public MarketServiceClient(GrpcClientFactory clientFactory, ILogger<MarketServiceClient> logger)
        {
            _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");
            _marketMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("MarketMarket");
            _logger = logger;
        }

        public void AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default)
        {
            initializationTasks.Add(_marketWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken).ResponseAsync);
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            return await _marketWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            return await _marketWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
        }

        public Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<GetSpeciesPricesResponse> GetSpeciesPricesAsync(GetSpeciesPricesRequest getSpeciesPricesRequest, CancellationToken cancellationToken)
        {
            return await _marketMarketClient.GetSpeciesPricesAsync(getSpeciesPricesRequest, cancellationToken: cancellationToken);
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, CancellationToken cancellationToken)
        {
            return await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
        }

        public async Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, CancellationToken cancellationToken)
        {
            return await _marketMarketClient.UpdateSalesAsync(updateSalesRequest, cancellationToken: cancellationToken);
        }

        public Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, CancellationToken cancellationToken)
        {
            return _marketMarketClient.UpdateSpeciesPricesAsync(updateSpeciesPricesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }
    }
}
