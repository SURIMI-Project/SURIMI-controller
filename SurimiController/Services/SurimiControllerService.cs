using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using System.Diagnostics;
using System.Xml;

namespace SurimiController.Services
{
    public class SurimiControllerService(GrpcClientFactory clientFactory, ActivitySource activitySource, MarketService.MarketServiceClient marketClient) : ControllerService.ControllerServiceBase
    {
        private readonly WorkflowService.WorkflowServiceClient _ecopathWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow");
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");

        private readonly MarketService.MarketServiceClient _marketClient = marketClient;

        private readonly ActivitySource _activitySource = activitySource;

        public override async Task<InitSimulationResponse> InitSimulation(InitSimulationRequest init, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("InitSimulation");
            activity?.SetTag("ScenarioId", init.ScenarioId);
            var simulationId = Guid.NewGuid();

            Console.WriteLine($"Initializing scenario {init.ScenarioId}... SimulationId = {simulationId}");
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
            Console.WriteLine($"Running simulation...");

            var current = request.StartDateTime.ToDateTime();   // Start at startdatetime
            var stepSize = XmlConvert.ToTimeSpan(request.StepSize);
            var end = current.Add(XmlConvert.ToTimeSpan(request.SimulationDuration));

            while (current <= end)
            {
                Console.WriteLine($"Processing step {current}...");

                var speciesPriceResponse = await _marketClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest());

                var marketReply = new UpdatePricesRequest
                {
                    SimulationId = request.SimulationId
                };

                marketReply.Prices.AddRange(speciesPriceResponse.Prices.Select(price => new SpeciesPrice
                {
                    SpeciesId = price.SpeciesId,
                    Price = price.Price,
                    Currency = price.Currency,
                    MeasurementUnit = price.MeasurementUnit,
                    PortId = price.PortId,
                    Timestamp = Timestamp.FromDateTime(current)
                }).ToList());

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
