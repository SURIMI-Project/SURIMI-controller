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
        private readonly ILogger<SurimiControllerService> _logger;
        private readonly ActivitySource _activitySource;
        private readonly WorkflowService.WorkflowServiceClient _ecopathWorkflowClient;
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;
        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;

        public SurimiControllerService(GrpcClientFactory clientFactory, ActivitySource activitySource, MarketService.MarketServiceClient marketClient, ILogger<SurimiControllerService> logger)
        {
            _activitySource = activitySource;
            _marketClient = marketClient;
            _logger = logger;
            _ecopathWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow");
            _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
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

            var marketReply = _marketWorkflowClient.InitAsync(initRequest);
            var ecopathReply = _ecopathWorkflowClient.InitAsync(initRequest);
            var poseidonReply = _poseidonWorkflowClient.InitAsync(initRequest);


            activity?.AddEvent(new ActivityEvent("Start Init ecopath and poseidon"));
            await marketReply;
            await ecopathReply;
            await poseidonReply;

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

                var speciesPriceResponse = await _marketClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest());

                var marketReply = new UpdatePricesRequest
                {
                    SimulationId = request.SimulationId
                };

                marketReply.Prices.AddRange(speciesPriceResponse.Prices);

                var ecopathUpdatePricesReply = _ecopathWorkflowClient.UpdatePricesAsync(marketReply);
                var poseidonUpdatePricesReply = _poseidonWorkflowClient.UpdatePricesAsync(marketReply);
                await ecopathUpdatePricesReply;
                await poseidonUpdatePricesReply;

                var simulationStepRequest = new SimulateStepRequest()
                {
                    SimulationId = request.SimulationId
                };

                var ecopathSimulateStelReply = _ecopathWorkflowClient.SimulateStepAsync(simulationStepRequest);
                var poseidonSimulateStelReply = _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest);
                var marketSimulateStelReply = _marketWorkflowClient.SimulateStepAsync(simulationStepRequest);

                await ecopathSimulateStelReply;
                await poseidonSimulateStelReply;
                await marketSimulateStelReply;

                current = current.Add(stepSize);
            }
            return new RunSimulationResponse();
        }
    }
}
