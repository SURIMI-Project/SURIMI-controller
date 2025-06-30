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
        private static Dictionary<string, Models.Simulation> _simulations = new Dictionary<string, Models.Simulation>();
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;
        private readonly MarketService.MarketServiceClient _poseidonMarketClient;
        private readonly FisheryService.FisheryServiceClient _poseidonFisheryClient;
        private readonly EcologyService.EcologyServiceClient _poseidonEcologyClient;

        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;
        private readonly MarketService.MarketServiceClient _marketClient;

        private readonly WorkflowService.WorkflowServiceClient _cmsyWorkflowClient;
        private readonly EcologyService.EcologyServiceClient _cmsyEcologyClient;
        private readonly FisheryService.FisheryServiceClient _cmsyFisheryClient;

        private readonly StockAssessmentService.StockAssessmentServiceClient _cmsyStockAssessmentClient;

        private readonly ILogger<SurimiControllerService> _logger;
        private readonly ActivitySource _activitySource;
        private readonly SimulationDispatcher _ecopathSimDispatcher;

        public SurimiControllerService(GrpcClientFactory clientFactory, ActivitySource activitySource, MarketService.MarketServiceClient marketClient, ILogger<SurimiControllerService> logger, StockAssessmentService.StockAssessmentServiceClient stockAssessmentClient, SimulationDispatcher simulationDispatcher)
        {
            _activitySource = activitySource;
            _logger = logger;
            _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
            _poseidonMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("PoseidonMarket");
            _poseidonEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("PoseidonEcology");
            _poseidonFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("PoseidonFishery");

            _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");
            _marketClient = marketClient;

            _cmsyWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow");
            _cmsyEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("CmsyEcology");
            _cmsyFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("CmsyFishery");

            _cmsyStockAssessmentClient = stockAssessmentClient;
            _ecopathSimDispatcher = simulationDispatcher;
        }

        public override async Task<InitSimulationResponse> InitSimulation(InitSimulationRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("InitSimulation");
            activity?.SetTag("ScenarioId", request.Simulation.ScenarioId);

            _logger.LogInformation($"Initializing scenario {request.Simulation.ScenarioId}... SimulationId = {request.Simulation.SimulationId}");
            var initRequest = new InitialiseRequest
            {
                ScenarioId = request.Simulation.ScenarioId,
                StartDateTime = request.Simulation.StartDateTime,
                StepSize = request.Simulation.StepSize,
                SimulationId = request.Simulation.SimulationId
            };

            var marketResponse = _marketWorkflowClient.InitialiseAsync(initRequest);
            var ecopathResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, InitialiseRequest, InitialiseResponse>(initRequest, initRequest.SimulationId,
                (client, req) => client.InitialiseAsync(req));

            var poseidonResponse = _poseidonWorkflowClient.InitialiseAsync(initRequest);
            var cmsyResponse = _cmsyWorkflowClient.InitialiseAsync(initRequest);

            activity?.AddEvent(new ActivityEvent("Start Init ecopath and poseidon"));
            await marketResponse;
            await ecopathResponse;
            await poseidonResponse;
            await cmsyResponse;

            // Await the response headers
            var headers = await ecopathResponse.ResponseHeadersAsync;

            // Find the header by key (case-insensitive)
            var hostValue = headers.GetValue("host"); // returns null if not found

            var createStockAssessmentresponse = _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = request.Simulation.SimulationId });

            _simulations[request.Simulation.SimulationId] = new Models.Simulation
            {
                ScenarioId = request.Simulation.ScenarioId,
                StartDateTime = request.Simulation.StartDateTime.ToDateTime(),
                StepSize = request.Simulation.StepSize,
                Status = "Initialised",
                EcologyHost = hostValue ?? string.Empty,
                SimulationCreated = DateTime.UtcNow
            };

            activity?.AddEvent(new ActivityEvent("Finished ecopoath and poseidon"));
            return new InitSimulationResponse() { SimulationId = request.Simulation.SimulationId };
        }

        public override async Task<RunSimulationResponse> RunSimulation(RunSimulationRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("RunSimulation");
            _logger.LogInformation($"Running simulation...");

            _simulations[request.SimulationId].Status = "Running";

            var current = request.StartDateTime.ToDateTime();   // Start at startdatetime
            var end = current.Add(XmlConvert.ToTimeSpan(request.SimulationDuration));

            while (current <= end)
            {
                await ProcessSimulationStep(request, current);

                current = AddStepSize(current, request.StepSize);
            }
            var createStockAssessmentresponse = _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = request.SimulationId });

            // Finalise the simulation
            var finaliseRequest = new FinaliseRequest
            {
                SimulationId = request.SimulationId,
            };
            var ecopathFinaliseResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, FinaliseRequest, FinaliseResponse>(finaliseRequest, request.SimulationId,
                (client, req) => client.FinaliseAsync(req));
            _ecopathSimDispatcher.ReleasePodFromSimulation(request.SimulationId);

            var marketFinaliseResponse = _marketWorkflowClient.FinaliseAsync(finaliseRequest);
            //var poseidonFinaliseResponse = _poseidonWorkflowClient.FinaliseAsync(finaliseRequest);
            var cmsyFinaliseResponse = _cmsyWorkflowClient.FinaliseAsync(finaliseRequest);

            await ecopathFinaliseResponse;
            await marketFinaliseResponse;
            //await poseidonFinaliseResponse;
            await cmsyFinaliseResponse;

            _simulations[request.SimulationId].Status = "Finished";
            return new RunSimulationResponse();
        }

        public override Task<GetAllSimulationsResponse> GetAllSimulations(GetAllSimulationsRequest get, ServerCallContext context)
        {
            return Task.FromResult(new GetAllSimulationsResponse
            {
                Simulations =
                {
                    _simulations.Select(sim => new Grpc.Surimi.Simulation
                    {
                        SimulationId = sim.Key,
                        ScenarioId = sim.Value.ScenarioId,
                        StartDateTime = Timestamp.FromDateTime(sim.Value.StartDateTime),
                        StepSize = sim.Value.StepSize,
                        Status = sim.Value.Status,
                        SimulationCurrent = sim.Value.SimulationCurrent == default ? null : Timestamp.FromDateTime(sim.Value.SimulationCurrent),
                        SimulationCreated = sim.Value.SimulationCreated == default ? null : Timestamp.FromDateTime(sim.Value.SimulationCreated),
                        EcologyHost = sim.Value.EcologyHost
                    }).OrderByDescending(x => x.SimulationCreated).ToList()
                }
            });
        }

        private async Task ProcessSimulationStep(RunSimulationRequest request, DateTime current)
        {
            LogStep(current, "Market.GetSpeciesPrices");
            _simulations[request.SimulationId].SimulationCurrent = current;

            var speciesPriceResponse = await _marketClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest() { SimulationId = request.SimulationId });

            var updatePriceRequest = new UpdateSpeciesPricesRequest
            {
                SimulationId = request.SimulationId
            };
            updatePriceRequest.Prices.AddRange(speciesPriceResponse.Prices);

            var ecopathUpdatePricesResponse = _ecopathSimDispatcher.DispatchAsync<MarketService.MarketServiceClient, UpdateSpeciesPricesRequest, UpdateSpeciesPricesResponse>(updatePriceRequest, request.SimulationId,
                (client, req) => client.UpdateSpeciesPricesAsync(req));

            var poseidonUpdatePricesResponse = _poseidonMarketClient.UpdateSpeciesPricesAsync(updatePriceRequest);

            LogStep(current, "Ecopath.UpdatePrices");
            await ecopathUpdatePricesResponse;
            LogStep(current, "Poseidon.UpdatePrices");
            await poseidonUpdatePricesResponse;

            var simulationStepRequest = new SimulateStepRequest()
            {
                SimulationId = request.SimulationId
            };

            LogStep(current, "Ecopath.SimulateStep");
            await _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, SimulateStepRequest, SimulateStepResponse>(simulationStepRequest, request.SimulationId,
                (client, req) => client.SimulateStepAsync(req));

            LogStep(current, "Ecopath GetBiomass (intermediate)");
            var getBiomassResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(new GetBiomassRequest() { SimulationId = request.SimulationId }, request.SimulationId,
                (client, req) => client.GetBiomassAsync(req));

            getBiomassResponse = GetTestBiomassIntermediate(getBiomassResponse);

            var updateBiomassIntermediateRequest = new UpdateBiomassRequest()
            {
                SimulationId = request.SimulationId,
                BiomassSummary = getBiomassResponse.BiomassSummary
            };

            LogStep(current, "Poseidon.UpdateBiomass  (intermediate)");
            var poseidonUpdateBiomassResponse = await _poseidonEcologyClient.UpdateBiomassAsync(updateBiomassIntermediateRequest);

            LogStep(current, "Poseidon.SimulateStep");
            var poseidonSimulateStepResponse = await _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest);

            var getCatchDispositionRequest = new GetCatchDispositionRequest()
            {
                SimulationId = request.SimulationId,
                StartDateTime = Timestamp.FromDateTime(current),
                EndDateTime = Timestamp.FromDateTime(AddStepSize(current, request.StepSize))
            };

            LogStep(current, "Poseidon.GetCatchDisposition");
            var poseidonCatchDisposition = await _poseidonFisheryClient.GetCatchDispositionAsync(getCatchDispositionRequest);

            var updateCatchDispositionIntermediateRequest = new UpdateCatchDispositionRequest()
            {
                SimulationId = request.SimulationId,
                CatchDispositionSummary = poseidonCatchDisposition.CatchDispositionSummary
            };

            LogStep(current, "Ecopath.UpdateCatchDisposition Summary");
            var catchDispositionResponse = await _ecopathSimDispatcher.DispatchAsync<FisheryService.FisheryServiceClient, UpdateCatchDispositionRequest, UpdateCatchDispositionResponse>(updateCatchDispositionIntermediateRequest, request.SimulationId,
                (client, req) => client.UpdateCatchDispositionAsync(req));

            LogStep(current, "Ecopath GetBiomass (total)");
            getBiomassResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(new GetBiomassRequest() { SimulationId = request.SimulationId }, request.SimulationId,
                (client, req) => client.GetBiomassAsync(req));

            var updateBiomassTotalRequest = new UpdateBiomassRequest()
            {
                SimulationId = request.SimulationId,
                BiomassSummary = getBiomassResponse.BiomassSummary
            };

            LogStep(current, "CMSY++.UpdateBiomass (total)");
            var cmsyUpdateBiomassResponse = await _cmsyEcologyClient.UpdateBiomassAsync(updateBiomassTotalRequest);

            LogStep(current, "Ecopath.GetCatchDisposition");
            var ecopathCatchDispositionSummary = await _ecopathSimDispatcher.DispatchAsync<FisheryService.FisheryServiceClient, GetCatchDispositionRequest, GetCatchDispositionResponse>(getCatchDispositionRequest, request.SimulationId,
                (client, req) => client.GetCatchDispositionAsync(req));

            var updateCatchDispositionTotalRequest = new UpdateCatchDispositionRequest()
            {
                SimulationId = request.SimulationId,
                CatchDispositionSummary = poseidonCatchDisposition.CatchDispositionSummary
            };

            LogStep(current, "CMSY UpdateCatchDisposition (total)");
            _cmsyFisheryClient.UpdateCatchDisposition(updateCatchDispositionTotalRequest);

            var getSalesRequest = new GetSalesRequest()
            {
                SimulationId = request.SimulationId,
                StartDateTime = Timestamp.FromDateTime(current),
                EndDateTime = Timestamp.FromDateTime(AddStepSize(current, request.StepSize))
            };

            LogStep(current, "Ecopath.GetSales");
            var ecopathGetSalesResponse = await _ecopathSimDispatcher.DispatchAsync<MarketService.MarketServiceClient, GetSalesRequest, GetSalesResponse>(getSalesRequest, request.SimulationId,
                (client, req) => client.GetSalesAsync(req));

            // Update Sales to Market
            LogStep(current, "Market.UpdateSales from Ecopath");
            var marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(CreateUpdateSalesRequest(ecopathGetSalesResponse, request.SimulationId));

            LogStep(current, "Poseidon.GetSalesSummary");
            var poseidonGetSalesResponse = await _poseidonMarketClient.GetSalesAsync(getSalesRequest);


            LogStep(current, "Market.UpdateSales from Poseidon");
            marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(CreateUpdateSalesRequest(poseidonGetSalesResponse, request.SimulationId));

            LogStep(current, "CMSY.SimulateStep");
            var cmsySimulateStepResponse = await _cmsyWorkflowClient.SimulateStepAsync(simulationStepRequest);

            LogStep(current, "Market.SimulateStep");
            var marketSimulateStepResponse = await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest);
        }

        private GetBiomassResponse GetTestBiomassIntermediate(GetBiomassResponse getBiomassResponse)
        {
            var response = new GetBiomassResponse
            {
                SimulationId = getBiomassResponse.SimulationId,
                BiomassSummary = new BiomassSummary()
                {
                    MeasurementUnit = getBiomassResponse.BiomassSummary.MeasurementUnit,
                    BiomassGrids =
                    {
                        new BiomassGrid()
                        {
                            SpeciesCode = getBiomassResponse.BiomassSummary.BiomassGrids.First().SpeciesCode
                        }
                    }
                }
            };
            response.BiomassSummary.BiomassGrids.First().BiomassCells.Add(new BiomassCell() 
            {
                Longitude = 3.854550141823501,
                Latitude = 43.4696509055585,
                Biomass = 234.58345446064777
            });
            response.BiomassSummary.BiomassGrids.First().BiomassCells.Add(new BiomassCell()
            {
                Longitude = 3.9378501437425006,
                Latitude = 43.4696509055585,
                Biomass = 234.49611350085252
            });
            response.BiomassSummary.BiomassGrids.First().BiomassCells.Add(new BiomassCell()
            {
                Longitude = 4.021150145661501,
                Latitude = 43.4696509055585,
                Biomass = 234.31994639839104
            });
            response.BiomassSummary.BiomassGrids.First().BiomassCells.Add(new BiomassCell()
            {
                Longitude = 4.104450147580501,
                Latitude = 43.4696509055585,
                Biomass = 234.03908407601193
            });
            return response;
        }

        private void LogStep(DateTime current, string step)
        {
            _logger.LogInformation($"Processing step {current}. {step}");
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

        public static UpdateSalesRequest CreateUpdateSalesRequest(GetSalesResponse response, string simulationId)
        {
            return new UpdateSalesRequest
            {
                SimulationId = simulationId,
                SalesSummaries = { response.SalesSummaries }
            };
        }
    }
}
