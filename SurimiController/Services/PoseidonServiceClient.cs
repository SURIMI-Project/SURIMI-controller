using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class PoseidonServiceClient : IPoseidonServiceClient
    {
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;
        private readonly MarketService.MarketServiceClient _poseidonMarketClient;
        private readonly FisheryService.FisheryServiceClient _poseidonFisheryClient;
        private readonly EcologyService.EcologyServiceClient _poseidonEcologyClient;

        private readonly ILogger<PoseidonServiceClient> _logger;

        public PoseidonServiceClient(GrpcClientFactory clientFactory, ILogger<PoseidonServiceClient> logger)
        {
            _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
            _poseidonMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("PoseidonMarket");
            _poseidonEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("PoseidonEcology");
            _poseidonFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("PoseidonFishery");

            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default)
        {
            var initialiseResponse = _poseidonWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken);
            initializationTasks.Add(initialiseResponse.ResponseAsync);
            return initialiseResponse;
        }

        public Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            return _poseidonWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token).ResponseAsync;
        }

        public Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            return _poseidonWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getCatchDispositionRequest.SimulationId, current, "GetCatchDisposition");
            return _poseidonFisheryClient.GetCatchDispositionAsync(getCatchDispositionRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSalesRequest.SimulationId, current, "GetSalesSummary");
            return _poseidonMarketClient.GetSalesAsync(getSalesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateBiomassRequest.SimulationId, current, "UpdateBiomass");
            return _poseidonEcologyClient.UpdateBiomassAsync(updateBiomassRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, CancellationToken cancellationToken)
        {
            return _poseidonFisheryClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken).ResponseAsync;
        }


        public Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSpeciesPricesRequest.SimulationId, current, "UpdatePrices");
            return _poseidonMarketClient.UpdateSpeciesPricesAsync(updateSpeciesPricesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Poseidon.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
