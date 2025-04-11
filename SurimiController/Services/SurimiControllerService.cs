using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using System.Diagnostics;
using System.Xml;

namespace SurimiController.Services
{
    public class SurimiControllerService(GrpcClientFactory clientFactory, ActivitySource activitySource) : ControllerService.ControllerServiceBase
    {
        private readonly WorkflowService.WorkflowServiceClient _ecopathWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow");
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
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

            var ecopathReply = _ecopathWorkflowClient.InitAsync(initRequest);

            var poseidonReply = _poseidonWorkflowClient.InitAsync(initRequest);

            activity?.AddEvent(new ActivityEvent("Start Init ecopath and poseidon"));
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

                var marketReply = new UpdatePricesRequest
                {
                    Prices = { new SpeciesPrice { SpeciesId= "BOG", Price = 1.03, Currency = "EUR", MeasurementUnit = "kg", PortId = "ESARN", Timestamp = Timestamp.FromDateTime(DateTime.UtcNow) },
                                new SpeciesPrice { SpeciesId = "PIL", Price = 4.5, Currency = "EUR", MeasurementUnit = "kg", PortId = "ESARN", Timestamp =Timestamp.FromDateTime(DateTime.UtcNow)  }
                        },
                    SimulationId = request.SimulationId
                };

                var ecopathUpdatePricesReply = await _ecopathWorkflowClient.UpdatePricesAsync(marketReply);
                var poseidonUpdatePricesReply = await _poseidonWorkflowClient.UpdatePricesAsync(marketReply);

                var simulationStepRequest = new SimulateStepRequest()
                {
                    SimulationId = request.SimulationId
                };

                var ecopathSimulateStelReply = await _ecopathWorkflowClient.SimulateStepAsync(simulationStepRequest);
                var poseidonSimulateStelReply = await _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest);

                current = current.Add(stepSize);
            }
            return new RunSimulationResponse();
        }
    }
}
