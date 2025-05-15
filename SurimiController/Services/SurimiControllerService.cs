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

            var marketReply = _marketWorkflowClient.InitAsync(initRequest);
            var ecopathReply = _ecopathWorkflowClient.InitAsync(initRequest);
            var poseidonReply = _poseidonWorkflowClient.InitAsync(initRequest);
            var cmsyReply = _cmsyWorkflowClient.InitAsync(initRequest);


            activity?.AddEvent(new ActivityEvent("Start Init ecopath and poseidon"));
            await marketReply;
            await ecopathReply;
            await poseidonReply;
            await cmsyReply;

            var createStockAssessmentresponse = await _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = simulationId.ToString() });

            activity?.AddEvent(new ActivityEvent("Finished ecopoath and poseidon"));
            return new InitSimulationResponse() { SimulationId = simulationId.ToString() };
        }

        public override async Task<RunSimulationResponse> RunSimulation(RunSimulationRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("RunSimulation");
            _logger.LogInformation($"Running simulation...");

            var current = request.StartDateTime.ToDateTime();   // Start at startdatetime
            var stepSize = XmlConvert.ToTimeSpan(request.StepSize);
            var end = current.Add(XmlConvert.ToTimeSpan(request.SimulationDuration));

            while (current <= end)
            {
                _logger.LogInformation($"Processing step {current}. Start with SpeciesPrices");

                var speciesPriceResponse = await _marketClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest() { SimulationId = request.SimulationId });

                var marketResponse = new UpdatePricesRequest
                {
                    SimulationId = request.SimulationId
                };
                marketResponse.Prices.AddRange(speciesPriceResponse.Prices);

                var ecopathUpdatePricesReply = _ecopathWorkflowClient.UpdatePricesAsync(marketResponse);
                var poseidonUpdatePricesReply = _poseidonWorkflowClient.UpdatePricesAsync(marketResponse);
                await ecopathUpdatePricesReply;
                await poseidonUpdatePricesReply;

                _logger.LogInformation($"Processing step {current}. Continue with Biomass");
                var getBiomassResponse = await _ecologyClient.GetBiomassAsync(new GetBiomassRequest(){ SimulationId = request.SimulationId });

                var updateBiomassRequest = new UpdateBiomassRequest()
                {
                    SimulationId = request.SimulationId,
                    MeasurementUnit = getBiomassResponse.MeasurementUnit
                };
                updateBiomassRequest.BiomassGrids.AddRange(getBiomassResponse.BiomassGrids);
                var poseidonUpdateBiomassResponse = _poseidonWorkflowClient.UpdateBiomassAsync(updateBiomassRequest);
                var cmsyUpdateBiomassResponse = _cmsyWorkflowClient.UpdateBiomassAsync(updateBiomassRequest);

                await poseidonUpdateBiomassResponse;
                await cmsyUpdateBiomassResponse;

                _logger.LogInformation($"Processing step {current}. Move on with the SimulateStep");
                var simulationStepRequest = new SimulateStepRequest()
                {
                    SimulationId = request.SimulationId
                };

                var poseidonSimulateStepResponse = await _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest);

                var poseidonGetSalesRequest = new GetSalesSummaryRequest()
                {
                    SimulationId = request.SimulationId,
                    StartDateTime = Timestamp.FromDateTime(current),
                    EndDateTime = Timestamp.FromDateTime(current.Add(stepSize))
                };
                var poseidonGetSalesResponse = await _poseidonAgentsClient.GetSalesSummaryAsync(poseidonGetSalesRequest);

                // Request catches of Poseidon

                // Update catches to EwE

                // UpdateCatches to CMSY

                // Request discards of Poseidon

                // Update discards to EwE

                var ecopathSimulateStepResponse = await _ecopathWorkflowClient.SimulateStepAsync(simulationStepRequest);

                // Update Sales to Market
               var updateSalesRequest = new UpdateSalesRequest()
                {
                    SimulationId = request.SimulationId,
                };
                updateSalesRequest.SalesSummaries.AddRange(poseidonGetSalesResponse.SalesSummaries);
                var marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(updateSalesRequest);


                var marketSimulateStepResponse = await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest);

                current = current.Add(stepSize);
            }
            var createStockAssessmentresponse = await _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = request.SimulationId });

            return new RunSimulationResponse();
        }
    }
}
