using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public class SurimiControllerService(GrpcClientFactory clientFactory) : ControllerService.ControllerServiceBase
    {
        private readonly WorkflowService.WorkflowServiceClient _ecopathWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("EcopathWorkflow");
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");

        public override async Task<RunSimulationResponse> RunSimulation(RunSimulationRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Running simulation...");

            //var marketClient = GetClient<Market.MarketClient>("MARKET_URL");

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"Processing step {i}...");

                // retrieve marketprice
                //var marketReply = await marketClient.GetPricesAsync(new Empty());
                var marketReply = new UpdatePricesRequest
                {
                    Prices = { new SpeciesPrice { SpeciesId= "BOG", Price = 1.03, Currency = "EUR", MeasurementUnit = "kg", PortId = "ESARN", Timestamp = Timestamp.FromDateTime(DateTime.UtcNow) },
                            new SpeciesPrice { SpeciesId = "WHA", Price = 4.5, Currency = "EUR", MeasurementUnit = "kg", PortId = "ESARN", Timestamp =Timestamp.FromDateTime(DateTime.UtcNow)  }
                    }
                };

                var ecopathReply = await _ecopathWorkflowClient.UpdatePricesAsync(marketReply);
                var poseidonReply = await _poseidonWorkflowClient.UpdatePricesAsync(marketReply);

                // call RunSimulation on Poseidon

                //                ecopathReply = await poseidonClient.RunSimulationAsync(new SimulationRequest { ExperimentId = request.ExperimentId });
            }
            return new RunSimulationResponse();
        }
    }
}
