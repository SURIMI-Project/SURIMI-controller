using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using System.Diagnostics;
using System.Xml;

namespace SurimiController.Services
{
    public class SurimiControllerService : ControllerService.ControllerServiceBase
    {
        private readonly WorkflowService.WorkflowServiceClient _ecopathWorkflowClient;
        private readonly FisheryService.FisheryServiceClient _ecopathFisheryClient;
        private readonly EcologyService.EcologyServiceClient _ecopathEcologyClient;
        private readonly MarketService.MarketServiceClient _ecopathMarketClient;

        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;
        private readonly FisheryService.FisheryServiceClient _poseidonFisheryClient;
        private readonly EcologyService.EcologyServiceClient _poseidonEcologyClient;
        private readonly MarketService.MarketServiceClient _poseidonMarketClient;

        private readonly WorkflowService.WorkflowServiceClient _cmsyWorkflowClient;
        private readonly FisheryService.FisheryServiceClient _cmsyFisheryClient;
        private readonly EcologyService.EcologyServiceClient _cmsyEcologyClient;
        private readonly StockAssessmentService.StockAssessmentServiceClient _cmsyStockAssessmentClient;

        private readonly ILogger<SurimiControllerService> _logger;
        private readonly ActivitySource _activitySource;
        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;
        private readonly MarketService.MarketServiceClient _marketClient;

        public SurimiControllerService(GrpcClientFactory clientFactory, ActivitySource activitySource, MarketService.MarketServiceClient marketClient, ILogger<SurimiControllerService> logger, StockAssessmentService.StockAssessmentServiceClient stockAssessmentClient)
        {
            _activitySource = activitySource;
            _marketClient = marketClient;
            _logger = logger;
            _ecopathWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow");
            _ecopathFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("EcopathFishery");
            _ecopathEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("EcopathEcology");
            _ecopathMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("EcopathMarket");

            _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
            _poseidonFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("PoseidonFishery");
            _poseidonEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("PoseidonEcology");
            _poseidonMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("PoseidonMarket");


            _cmsyWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow");
            _cmsyFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("CmsyFishery");
            _cmsyEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("CmsyEcology");
            _cmsyStockAssessmentClient = stockAssessmentClient;

            _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");
        }

        public override async Task<InitSimulationResponse> InitSimulation(InitSimulationRequest init, ServerCallContext context)
        {
            if (init.ScenarioId.ToLower().Equals("testcontroller"))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ScenarioId cannot be 'testcontroller'"));
            }
            if (init.ScenarioId.ToLower().Equals("testcontroller2"))
            {
                string tst = init.ScenarioId.Substring(4, 12);       // Will throw exception
            }

            using var activity = _activitySource.StartActivity("InitSimulation");
            activity?.SetTag("ScenarioId", init.ScenarioId);
            var simulationId = Guid.NewGuid();

            _logger.LogInformation($"Initializing scenario {init.ScenarioId}... SimulationId = {simulationId}");
            var initRequest = new InitRequest
            {
                ScenarioId = init.ScenarioId,
                StartDateTime = init.StartDateTime,
                StepSize = init.StepSize,
                SimulationId = simulationId.ToString(),
            };

            var marketResponse = _marketWorkflowClient.InitAsync(initRequest);
            var ecopathResponse = _ecopathWorkflowClient.InitAsync(initRequest);
            var poseidonResponse = _poseidonWorkflowClient.InitAsync(initRequest);
            var cmsyResponse = _cmsyWorkflowClient.InitAsync(initRequest);


            activity?.AddEvent(new ActivityEvent("Start Init ecopath and poseidon"));
            await marketResponse;
            await ecopathResponse;
            await poseidonResponse;
            await cmsyResponse;

            var createStockAssessmentresponse = _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = simulationId.ToString() });

            activity?.AddEvent(new ActivityEvent("Finished ecopoath and poseidon"));
            return new InitSimulationResponse() { SimulationId = simulationId.ToString() };
        }

        public override async Task<RunSimulationResponse> RunSimulation(RunSimulationRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("RunSimulation");
            _logger.LogInformation($"Running simulation...");

            var current = request.StartDateTime.ToDateTime();   // Start at startdatetime
            var end = current.Add(XmlConvert.ToTimeSpan(request.SimulationDuration));

            while (current <= end)
            {
                _logger.LogInformation($"Processing step {current}. Market.GetSpeciesPrices");

                var speciesPriceResponse = await _marketClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest() { SimulationId = request.SimulationId });

                var updatePriceRequest = CreateUpdateSpeciesPricesRequest(speciesPriceResponse, request.SimulationId);

                var ecopathUpdatePricesResponse = _ecopathMarketClient.UpdateSpeciesPricesAsync(updatePriceRequest);
                var poseidonUpdatePricesResponse = _poseidonMarketClient.UpdateSpeciesPricesAsync(updatePriceRequest);

                _logger.LogInformation($"Processing step {current}. Ecopath.UpdatePrices");
                await ecopathUpdatePricesResponse;
                _logger.LogInformation($"Processing step {current}. Poseidon.UpdatePrices");
                await poseidonUpdatePricesResponse;

                var simulationStepRequest = CreateSimulateStepRequest(request.SimulationId);

                _logger.LogInformation($"Processing step {current}. Ecopath.SimulateStep");
                await _ecopathWorkflowClient.SimulateStepAsync(simulationStepRequest);

                _logger.LogInformation($"Processing step {current}. Ecopath GetBiomass (intermediate)");
                var getBiomassResponse = await _ecopathEcologyClient.GetBiomassAsync(new GetBiomassRequest() { SimulationId = request.SimulationId });

                var updateBiomassRequest = CreateUpdateBiomassRequest(getBiomassResponse, request.SimulationId);

                _logger.LogInformation($"Processing step {current}. Poseidon.UpdateBiomass  (intermediate)");
                var poseidonUpdateBiomassResponse = await _poseidonEcologyClient.UpdateBiomassAsync(updateBiomassRequest);

                _logger.LogInformation($"Processing step {current}. Poseidon.SimulateStep");
                var poseidonSimulateStepResponse = await _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest);

                var getCatchDispositionRequest = CreateGetCatchDispositionRequest(request, current);

                _logger.LogInformation($"Processing step {current}. Poseidon.GetCatchDisposition");
                var poseidonCatchDisposition = await _poseidonFisheryClient.GetCatchDispositionAsync(getCatchDispositionRequest);

                var updateCatchDispositionRequest = GetUpdateCatchDispositionRequest(poseidonCatchDisposition, request.SimulationId);

                _logger.LogInformation($"Processing step {current}. Ecopath.UpdateCatchDisposition");
                var catchDispositionResponse = await _ecopathFisheryClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest);

                _logger.LogInformation($"Processing step {current}. Ecopath GetBiomass (total)");
                var totalGetBiomassResponse = await _ecopathEcologyClient.GetBiomassAsync(new GetBiomassRequest() { SimulationId = request.SimulationId });

                var totalUpdateBiomassRequest = CreateUpdateBiomassRequest(totalGetBiomassResponse, request.SimulationId);
                _logger.LogInformation($"Processing step {current}. CMSY++.UpdateBiomass (total)");
                var cmsyUpdateBiomassResponse = await _cmsyEcologyClient.UpdateBiomassAsync(totalUpdateBiomassRequest);

                _logger.LogInformation($"Processing step {current}. Ecopath.GetCatchDisposition");
                var ecopathCatchDisposition = await _ecopathFisheryClient.GetCatchDispositionAsync(getCatchDispositionRequest);

                var totalUpdateCatchDispositionRequest = GetUpdateCatchDispositionRequest(ecopathCatchDisposition, request.SimulationId);

                _logger.LogInformation($"Processing step {current}. CMSY UpdateCatchDisposition (total)");
                _cmsyFisheryClient.UpdateCatchDisposition(totalUpdateCatchDispositionRequest);

                var getSalesRequest = CreateGetSalesRequest(request, current);

                _logger.LogInformation($"Processing step {current}. Ecopath.GetSalesSummary");
                var ecopathGetSalesResponse = await _ecopathMarketClient.GetSalesAsync(getSalesRequest);

                // Update Sales to Market
                var updateSalesRequestEcoPath = CreateUpdateSalesRequest(ecopathGetSalesResponse, request.SimulationId);

                _logger.LogInformation($"Processing step {current}. Market.UpdateSales from Ecopath");
                var marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(updateSalesRequestEcoPath);

                _logger.LogInformation($"Processing step {current}. Poseidon.GetSalesSummary");
                var poseidonGetSalesResponse = await _poseidonMarketClient.GetSalesAsync(getSalesRequest);

                var updateSalesRequestPoseidon = CreateUpdateSalesRequest(poseidonGetSalesResponse, request.SimulationId);

                _logger.LogInformation($"Processing step {current}. Market.UpdateSales from Poseidon");
                marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(updateSalesRequestPoseidon);

                _logger.LogInformation($"Processing step {current}. CMSY.SimulateStep");
                var cmsySimulateStepResponse = await _cmsyWorkflowClient.SimulateStepAsync(simulationStepRequest);

                _logger.LogInformation($"Processing step {current}. Market.SimulateStep");
                var marketSimulateStepResponse = await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest);

                current = AddStepSize(current, request.StepSize);
            }
            var createStockAssessmentresponse = _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = request.SimulationId });

            return new RunSimulationResponse();
        }

        private static DateTime AddStepSize(DateTime current, string input)
        {
            if (input.Length >= 2 && input.StartsWith('P') && input.EndsWith('M') && int.TryParse((input.Substring(1, input.Length - 2)), out int result))
            {
                return current.AddMonths(result);
            }
            else
            {
                return current + XmlConvert.ToTimeSpan(input);
            }
        }

        public static UpdateSpeciesPricesRequest CreateUpdateSpeciesPricesRequest(GetSpeciesPricesResponse response, string simulationId)
        {
            return new UpdateSpeciesPricesRequest
            {
                SimulationId = simulationId,
                Prices = { response.Prices }
            };
        }

        public static UpdateBiomassRequest CreateUpdateBiomassRequest(GetBiomassResponse response, string simulationId)
        {
            return new UpdateBiomassRequest
            {
                SimulationId = simulationId,
                BiomassSummary = response.BiomassSummary
            };
        }

        public static UpdateCatchDispositionRequest GetUpdateCatchDispositionRequest(GetCatchDispositionResponse response, string simulationId)
        {
            return new UpdateCatchDispositionRequest
            {
                SimulationId = simulationId,
                CatchDispositionSummary = response.CatchDispositionSummary
            };
        }

        public static GetSalesRequest CreateGetSalesRequest(RunSimulationRequest request, DateTime current)
        {
            return new GetSalesRequest
            {
                SimulationId = request.SimulationId,
                StartDateTime = Timestamp.FromDateTime(current),
                EndDateTime = Timestamp.FromDateTime(AddStepSize(current, request.StepSize))
            };
        }

        public static UpdateSalesRequest CreateUpdateSalesRequest(GetSalesResponse response, string simulationId)
        {
            return new UpdateSalesRequest
            {
                SimulationId = simulationId,
                SalesSummaries = { response.SalesSummaries }
            };
        }

        public static SimulateStepRequest CreateSimulateStepRequest(string simulationId)
        {
            return new SimulateStepRequest
            {
                SimulationId = simulationId
            };
        }

        public static GetCatchDispositionRequest CreateGetCatchDispositionRequest(RunSimulationRequest request, DateTime current)
        {
            return new GetCatchDispositionRequest
            {
                SimulationId = request.SimulationId,
                StartDateTime = Timestamp.FromDateTime(current),
                EndDateTime = Timestamp.FromDateTime(AddStepSize(current, request.StepSize))
            };
        }
    }
}
