using Grpc.Core;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class EcopathServiceClient : IEcopathServiceClient
    {
        private readonly ILogger<MarketServiceClient> _logger;
        private readonly SimulationDispatcher _ecopathSimDispatcher;

        public EcopathServiceClient(ILogger<MarketServiceClient> logger, SimulationDispatcher ecopathSimDispatcher)
        {
            _logger = logger;
            _ecopathSimDispatcher = ecopathSimDispatcher;
        }

        public AsyncUnaryCall<InitialiseResponse>? AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default)
        {
            var initialiseResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, InitialiseRequest, InitialiseResponse>(initialiseRequest, initialiseRequest.SimulationId,
                (client, req) => client.InitialiseAsync(req));
            initializationTasks.Add(initialiseResponse.ResponseAsync);
            return initialiseResponse;
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            var ecopathCancelResponse = await _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, CancelRequest, CancelResponse>(cancelRequest, cancelRequest.SimulationId,
                (client, req) => client.CancelAsync(req));

            _ecopathSimDispatcher.ReleasePodFromSimulation(cancelRequest.SimulationId);
            return ecopathCancelResponse;
        }
        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            var _finaliseResponse = await _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, FinaliseRequest, FinaliseResponse>(finaliseRequest, finaliseRequest.SimulationId,
                (client, req) => client.FinaliseAsync(req));
            return _finaliseResponse;
        }

        public async Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getCatchDispositionRequest.SimulationId, current, "GetCatchDisposition");
            var ecopathCatchDispositionSummary = await _ecopathSimDispatcher.DispatchAsync<CatchProviderService.CatchProviderServiceClient, GetCatchDispositionRequest, GetCatchDispositionResponse>(getCatchDispositionRequest, getCatchDispositionRequest.SimulationId,
            (client, req) => client.GetCatchDispositionAsync(req, cancellationToken: cancellationToken));
            return ecopathCatchDispositionSummary;
        }

        public async Task<GetSalesResponse> GetSalesAsync(GetSalesRequest getSalesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getSalesRequest.SimulationId, current, "GetSales");
            var ecopathGetSalesResponse = await _ecopathSimDispatcher.DispatchAsync<SalesProviderService.SalesProviderServiceClient, GetSalesRequest, GetSalesResponse>(getSalesRequest, getSalesRequest.SimulationId,
                (client, req) => client.GetSalesAsync(req, cancellationToken: cancellationToken));
            return ecopathGetSalesResponse;
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(simulationStepRequest.SimulationId, current, "Ecopath.SimulateStep");
            var simulateStep = await _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, SimulateStepRequest, SimulateStepResponse>(simulationStepRequest, simulationStepRequest.SimulationId,
                (client, req) => client.SimulateStepAsync(req, cancellationToken: cancellationToken));
            return simulateStep;
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateCatchDispositionRequest.SimulationId, current, "UpdateCatchDisposition Summary");
            var catchDispositionResponse = await _ecopathSimDispatcher.DispatchAsync<CatchConsumerService.CatchConsumerServiceClient, UpdateCatchDispositionRequest, UpdateCatchDispositionResponse>(updateCatchDispositionRequest, updateCatchDispositionRequest.SimulationId,
              (client, req) => client.UpdateCatchDispositionAsync(req, cancellationToken: cancellationToken));
            return catchDispositionResponse;
        }

        public async Task<UpdateSpeciesPricesResponse> UpdateSpeciesPricesAsync(UpdateSpeciesPricesRequest updateSpeciesPricesRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(updateSpeciesPricesRequest.SimulationId, current, "UpdatePrices");
            var ecopathUpdatePricesResponse = await _ecopathSimDispatcher.DispatchAsync<SpeciesPriceConsumerService.SpeciesPriceConsumerServiceClient, UpdateSpeciesPricesRequest, UpdateSpeciesPricesResponse>(updateSpeciesPricesRequest, updateSpeciesPricesRequest.SimulationId,
               (client, req) => client.UpdateSpeciesPricesAsync(req, cancellationToken: cancellationToken));
            return ecopathUpdatePricesResponse;
        }

        public async Task<GetBiomassResponse> GetBiomassAsync(GetBiomassRequest getBiomassRequest, DateTime current, CancellationToken cancellationToken)
        {
            LogStep(getBiomassRequest.SimulationId, current, "GetBiomass");
            var getBiomassResponseIntermediate = await _ecopathSimDispatcher.DispatchAsync<EcologyProviderService.EcologyProviderServiceClient, GetBiomassRequest, GetBiomassResponse>(getBiomassRequest, getBiomassRequest.SimulationId,
                (client, req) => client.GetBiomassAsync(req, cancellationToken: cancellationToken));
            return getBiomassResponseIntermediate;
        }

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step Ecopath.{Step}. {DateTime}", simulationId, step, current);
        }
    }
}
