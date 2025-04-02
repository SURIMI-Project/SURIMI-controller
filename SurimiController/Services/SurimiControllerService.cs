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

            for (int year = request.FirstYear; year <= request.LastYear; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    Console.WriteLine($"Processing step month {month} of year {year}...");

                    // retrieve marketprice
                    //var marketReply = await marketClient.GetPricesAsync(new Empty());
                    var marketReply = new UpdatePricesRequest
                    {
                        Prices = { new SpeciesPrice { SpeciesId= "BOG", Price = 1.03, Currency = "EUR", MeasurementUnit = "kg", PortId = "ESARN", Timestamp = Timestamp.FromDateTime(DateTime.UtcNow) },
                            new SpeciesPrice { SpeciesId = "WHA", Price = 4.5, Currency = "EUR", MeasurementUnit = "kg", PortId = "ESARN", Timestamp =Timestamp.FromDateTime(DateTime.UtcNow)  }
                    }
                    };

                    var ecopathUpdatePricesReply = await _ecopathWorkflowClient.UpdatePricesAsync(marketReply);
                    var poseidonUpdatePricesReply = await _poseidonWorkflowClient.UpdatePricesAsync(marketReply);

                    // call RunSimulation on Poseidon

                    var ecopathSimulateStelReply = await _ecopathWorkflowClient.SimulateStepAsync(new SimulateStepRequest() { Year = year, Month = month});
                }
            }
            return new RunSimulationResponse();
        }
    }
}
