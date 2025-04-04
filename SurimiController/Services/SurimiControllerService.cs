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

            Console.WriteLine($"Initializing scenario {init.ScenarioId}...");

            var ecopathReply = _ecopathWorkflowClient.InitAsync(new InitRequest { ScenarioId = init.ScenarioId });

            var poseidonReply = _poseidonWorkflowClient.InitAsync(new InitRequest { ScenarioId = init.ScenarioId });

            activity?.AddEvent(new ActivityEvent("Start Init ecopath and poseidon"));
            await ecopathReply;
            await poseidonReply;
            activity?.AddEvent(new ActivityEvent("Finished ecopoath and poseidon"));
            return new InitSimulationResponse() { SimulationId = Guid.NewGuid().ToString() };
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
                                new SpeciesPrice { SpeciesId = "WHA", Price = 4.5, Currency = "EUR", MeasurementUnit = "kg", PortId = "ESARN", Timestamp =Timestamp.FromDateTime(DateTime.UtcNow)  }
                        }
                };

                var ecopathUpdatePricesReply = await _ecopathWorkflowClient.UpdatePricesAsync(marketReply);
                var poseidonUpdatePricesReply = await _poseidonWorkflowClient.UpdatePricesAsync(marketReply);

                var ecopathSimulateStelReply = await _ecopathWorkflowClient.SimulateStepAsync(new SimulateStepRequest() { SimulationId = request.SimulationId });

                current = current.Add(stepSize);
            }
            return new RunSimulationResponse();
        }
    }
}
