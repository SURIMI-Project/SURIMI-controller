using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class PoseidonServiceClient : IPoseidonServiceClient
    {
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;
        private readonly SalesProviderService.SalesProviderServiceClient _poseidonSalesProviderClient;
        private readonly SpeciesPriceConsumerService.SpeciesPriceConsumerServiceClient _poseidonSpeciesPriceConsumerClient;
        private readonly CatchProviderService.CatchProviderServiceClient _poseidonCatchProviderClient;
        private readonly EcologyConsumerService.EcologyConsumerServiceClient _poseidonEcologyConsumerClient;
        private readonly RegulationsConsumerService.RegulationsConsumerServiceClient _poseidonRegulationsConsumerClient;

        private readonly ILogger<PoseidonServiceClient> _logger;

        public PoseidonServiceClient(GrpcClientFactory clientFactory, ILogger<PoseidonServiceClient> logger)
        {
            _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
            _poseidonSalesProviderClient = clientFactory.CreateClient<SalesProviderService.SalesProviderServiceClient>("PoseidonSalesProvider");
            _poseidonSpeciesPriceConsumerClient = clientFactory.CreateClient<SpeciesPriceConsumerService.SpeciesPriceConsumerServiceClient>("PoseidonSpeciesPriceConsumer");
            _poseidonEcologyConsumerClient = clientFactory.CreateClient<EcologyConsumerService.EcologyConsumerServiceClient>("PoseidonEcologyConsumer");
            _poseidonCatchProviderClient = clientFactory.CreateClient<CatchProviderService.CatchProviderServiceClient>("PoseidonCatchProvider");
            _poseidonRegulationsConsumerClient = clientFactory.CreateClient<RegulationsConsumerService.RegulationsConsumerServiceClient>("PoseidonRegulationsConsumer");
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
            return _poseidonCatchProviderClient.GetCatchDispositionAsync(getCatchDispositionRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetFishingActivityResponse> GetFishingActivityAsync(GetFishingActivityRequest fishingActivityRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(fishingActivityRequest.SimulationId, current, "GetFishingActivity");
            return _poseidonRegulationsConsumerClient.GetFishingActivityAsync(fishingActivityRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSalesRequest.SimulationId, current, "GetSalesSummary");
            return _poseidonSalesProviderClient.GetSalesAsync(getSalesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
            return _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateBiomassRequest.SimulationId, current, "UpdateBiomass");
            return _poseidonEcologyConsumerClient.UpdateBiomassAsync(updateBiomassRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateRegulationsResponse> UpdateRegulationsAsync(UpdateRegulationsRequest updateRegulationsRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateRegulationsRequest.SimulationId, current, "UpdateRegulations");
            return _poseidonRegulationsConsumerClient.UpdateRegulationsAsync(updateRegulationsRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        public Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSpeciesPricesRequest.SimulationId, current, "UpdatePrices");
            return _poseidonSpeciesPriceConsumerClient.UpdateSpeciesPricesAsync(updateSpeciesPricesRequest, cancellationToken: cancellationToken).ResponseAsync;
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Poseidon.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
