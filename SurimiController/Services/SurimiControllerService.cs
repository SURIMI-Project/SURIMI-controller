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
        private readonly MarketService.MarketServiceClient _marketClient;
        private readonly EcologyService.EcologyServiceClient _ecologyClient;
        private readonly ILogger<SurimiControllerService> _logger;
        private readonly ActivitySource _activitySource;
        private readonly WorkflowService.WorkflowServiceClient _ecopathWorkflowClient;
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;
        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;
        private readonly WorkflowService.WorkflowServiceClient _cmsyWorkflowClient;
        private readonly AgentsService.AgentsServiceClient _poseidonAgentsClient;
        private readonly StockAssessmentService.StockAssessmentServiceClient _cmsyStockAssessmentClient;

        public SurimiControllerService(GrpcClientFactory clientFactory, ActivitySource activitySource, MarketService.MarketServiceClient marketClient, ILogger<SurimiControllerService> logger, EcologyService.EcologyServiceClient ecologyClient, AgentsService.AgentsServiceClient poseidonAgentsClient, StockAssessmentService.StockAssessmentServiceClient stockAssessmentClient)
        {
            _activitySource = activitySource;
            _marketClient = marketClient;
            _logger = logger;
            _ecopathWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow");
            _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
            _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");
            _cmsyWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow");
            _ecologyClient = ecologyClient;
            _poseidonAgentsClient = poseidonAgentsClient;
            _cmsyStockAssessmentClient = stockAssessmentClient;
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

            var createStockAssessmentresponse = await _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = simulationId.ToString() });

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

                var updatePriceRequest = new UpdatePricesRequest
                {
                    SimulationId = request.SimulationId
                };
                updatePriceRequest.Prices.AddRange(speciesPriceResponse.Prices);

                var ecopathUpdatePricesResponse = _ecopathWorkflowClient.UpdatePricesAsync(updatePriceRequest);
                var poseidonUpdatePricesResponse = _poseidonWorkflowClient.UpdatePricesAsync(updatePriceRequest);

                _logger.LogInformation($"Processing step {current}. Ecopath.UpdatePrices");
                await ecopathUpdatePricesResponse;
                _logger.LogInformation($"Processing step {current}. Poseidon.UpdatePrices");
                await poseidonUpdatePricesResponse;

                var simulationStepRequest = new SimulateStepRequest()
                {
                    SimulationId = request.SimulationId
                };

                _logger.LogInformation($"Processing step {current}. Ecopath.SimulateStep");
                await _ecopathWorkflowClient.SimulateStepAsync(simulationStepRequest);

                _logger.LogInformation($"Processing step {current}. GetBiomass");
                var getBiomassResponse = await _ecologyClient.GetBiomassAsync(new GetBiomassRequest() { SimulationId = request.SimulationId });

                var updateBiomassRequest = new UpdateBiomassRequest()
                {
                    SimulationId = request.SimulationId,
                    MeasurementUnit = getBiomassResponse.MeasurementUnit
                };
                updateBiomassRequest.BiomassGrids.AddRange(getBiomassResponse.BiomassGrids);

                _logger.LogInformation($"Processing step {current}. Poseidon.UpdateBiomass");
                var poseidonUpdateBiomassResponse = await _poseidonWorkflowClient.UpdateBiomassAsync(updateBiomassRequest);

                _logger.LogInformation($"Processing step {current}. Poseidon.SimulateStep");
                var poseidonSimulateStepResponse = await _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest);

                var poseidonGetCatchDispositionRequest = new GetCatchDispositionSummaryRequest()
                {
                    SimulationId = request.SimulationId,
                    StartDateTime = Timestamp.FromDateTime(current),
                    EndDateTime = Timestamp.FromDateTime(AddStepSize(current, request.StepSize))
                };

                _logger.LogInformation($"Processing step {current}. Poseidon.GetCatchDisposition");
                var poseidonGetSalesResponse = await _poseidonAgentsClient.GetCatchDispositionSummaryAsync(poseidonGetCatchDispositionRequest);




                var cmsyUpdateBiomassResponse = _cmsyWorkflowClient.UpdateBiomassAsync(updateBiomassRequest);


                _logger.LogInformation($"Processing step {current}. CMSY++.UpdateBiomass");
                await cmsyUpdateBiomassResponse;





                var poseidonGetSalesRequest = new GetSalesSummaryRequest()
                {
                    SimulationId = request.SimulationId,
                    StartDateTime = Timestamp.FromDateTime(current),
                    EndDateTime = Timestamp.FromDateTime(AddStepSize(current, request.StepSize))
                };

                _logger.LogInformation($"Processing step {current}. Poseidon.GetSalesSummary");
                var poseidonGetSalesResponse = await _poseidonAgentsClient.GetSalesSummaryAsync(poseidonGetSalesRequest);

                // Request catches of Poseidon

                // Update catches to EwE

                // UpdateCatches to CMSY

                // Request discards of Poseidon

                // Update discards to EwE


                // Update Sales to Market
                var updateSalesRequest = new UpdateSalesRequest()
                {
                    SimulationId = request.SimulationId,
                };
                updateSalesRequest.SalesSummaries.AddRange(poseidonGetSalesResponse.SalesSummaries);
                _logger.LogInformation($"Processing step {current}. Market.UpdateSales");
                var marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(updateSalesRequest);

                _logger.LogInformation($"Processing step {current}. Market.SimulateStep");
                var marketSimulateStepResponse = await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest);

                current = AddStepSize(current, request.StepSize);
            }
            var createStockAssessmentresponse = await _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = request.SimulationId });

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
    }
}
