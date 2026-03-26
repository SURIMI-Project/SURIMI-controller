using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class AggregatorServiceClient : IAggregatorServiceClient
    {
        private readonly ILogger<AggregatorServiceClient> _logger;
        private readonly WorkflowService.WorkflowServiceClient _aggregatorWorkflowClient;
        private readonly EcologyConsumerService.EcologyConsumerServiceClient _aggregatorEcologyConsumerClient;
        private readonly CatchConsumerService.CatchConsumerServiceClient _aggregatorCatchConsumerClient;
        private readonly MarketProviderService.MarketProviderServiceClient _aggregatorMarketProviderClient;
        private readonly SpeciesPriceConsumerService.SpeciesPriceConsumerServiceClient _aggregatorSpeciesPriceConsumerClient;

        private readonly bool _includeAggregator = Environment.GetEnvironmentVariable("EXCLUDE_AGGREGATOR")?.ToLower() != "true";

        public AggregatorServiceClient(GrpcClientFactory clientFactory, ILogger<AggregatorServiceClient> logger)
        {
            _aggregatorWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("AggregatorWorkflow");
            _aggregatorEcologyConsumerClient = clientFactory.CreateClient<EcologyConsumerService.EcologyConsumerServiceClient>("AggregatorEcologyConsumer");
            _aggregatorCatchConsumerClient = clientFactory.CreateClient<CatchConsumerService.CatchConsumerServiceClient>("AggregatorCatchConsumer");
            _aggregatorMarketProviderClient = clientFactory.CreateClient<MarketProviderService.MarketProviderServiceClient>("AggregatorMarketProvider"); ;
            _aggregatorSpeciesPriceConsumerClient = clientFactory.CreateClient<SpeciesPriceConsumerService.SpeciesPriceConsumerServiceClient>("AggregatorSpeciesPriceConsumer");
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
            return new CancelResponse() { SimulationId = cancelRequest.SimulationId };
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            if (_includeAggregator)
            {
                return await _aggregatorWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseResponse() { SimulationId = finaliseRequest.SimulationId };
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeAggregator)
            {
                LogStep(simulationStepRequest.SimulationId, current, "SimulateStep");
                return await _aggregatorWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
            }
            return new SimulateStepResponse() { SimulationId = simulationStepRequest.SimulationId };
        }

        public async Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeAggregator)
            {
                LogStep(updateBiomassRequest.SimulationId, current, "UpdateBiomass");
                return await _aggregatorEcologyConsumerClient.UpdateBiomassAsync(updateBiomassRequest, cancellationToken: cancellationToken);
            }
            return new UpdateBiomassResponse() { SimulationId = updateBiomassRequest.SimulationId };
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeAggregator)
            {
                LogStep(updateCatchDispositionRequest.SimulationId, current, "UpdateCatchDisposition");
                return await _aggregatorCatchConsumerClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken);
            }
            return new UpdateCatchDispositionResponse() { SimulationId = updateCatchDispositionRequest.SimulationId };
        }

        public async Task<UpdateSalesResponse> UpdateSalesAsync(UpdateSalesRequest updateSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeAggregator)
            {
                LogStep(updateSalesRequest.SimulationId, updateSalesRequest.StartDateTime.ToDateTime(), "UpdateSales");
                return await _aggregatorMarketProviderClient.UpdateSalesAsync(updateSalesRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSalesResponse() { SimulationId = updateSalesRequest.SimulationId };
        }

        public async Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeAggregator)
            {
                LogStep(updateSpeciesPricesRequest.SimulationId, current, "UpdateSpeciesPrices");
                return await _aggregatorSpeciesPriceConsumerClient.UpdateSpeciesPricesAsync(updateSpeciesPricesRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSpeciesPricesResponse() { SimulationId = updateSpeciesPricesRequest.SimulationId };
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Aggregator.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
