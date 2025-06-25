using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using Microsoft.AspNetCore.Identity.Data;
using System.Diagnostics;
using System.Xml;

namespace SurimiController.Services
{
    public class SurimiControllerService : ControllerService.ControllerServiceBase
    {
        private static Dictionary<string, Models.Simulation> _simulations = new Dictionary<string, Models.Simulation>();
        private readonly MarketService.MarketServiceClient _marketClient;
        private readonly EcologyService.EcologyServiceClient _cmsyEcologyClient;
        private readonly ILogger<SurimiControllerService> _logger;
        private readonly ActivitySource _activitySource;
        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;
        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;
        private readonly WorkflowService.WorkflowServiceClient _cmsyWorkflowClient;
        private readonly FisheryService.FisheryServiceClient _poseidonFisheryClient;
        private readonly StockAssessmentService.StockAssessmentServiceClient _cmsyStockAssessmentClient;
        private readonly SimulationDispatcher _ecopathSimDispatcher;

        public SurimiControllerService(GrpcClientFactory clientFactory, ActivitySource activitySource, MarketService.MarketServiceClient marketClient, ILogger<SurimiControllerService> logger, StockAssessmentService.StockAssessmentServiceClient stockAssessmentClient, SimulationDispatcher simulationDispatcher)
        {
            _activitySource = activitySource;
            _marketClient = marketClient;
            _logger = logger;
            _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
            _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");
            _cmsyWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow");
            _cmsyEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("CmsyEcology");
            _poseidonFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("PoseidonFishery"); ;
            _cmsyStockAssessmentClient = stockAssessmentClient;
            _ecopathSimDispatcher = simulationDispatcher;
        }

        public override async Task<InitSimulationResponse> InitSimulation(InitSimulationRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("InitSimulation");
            activity?.SetTag("ScenarioId", request.Simulation.ScenarioId);

            _logger.LogInformation($"Initializing scenario {request.Simulation.ScenarioId}... SimulationId = {request.Simulation.SimulationId}");
            var initRequest = new InitRequest
            {
                ScenarioId = request.Simulation.ScenarioId,
                StartDateTime = request.Simulation.StartDateTime,
                StepSize = request.Simulation.StepSize,
                SimulationId = request.Simulation.SimulationId
            };

            var marketResponse = _marketWorkflowClient.InitAsync(initRequest);
            var ecopathResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, InitRequest, InitResponse>(initRequest, initRequest.SimulationId,
                (client, req) => client.InitAsync(req));

            var poseidonResponse = _poseidonWorkflowClient.InitAsync(initRequest);
            var cmsyResponse = _cmsyWorkflowClient.InitAsync(initRequest);

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
                Status = "Initialized",
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

            // Finalize the simulation
            var finalizeRequest = new FinalizeRequest
            {
                SimulationId = request.SimulationId,
            };
            var ecopathFinalizeResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, FinalizeRequest, FinalizeResponse>(finalizeRequest, request.SimulationId,
                (client, req) => client.FinalizeAsync(req));
            _ecopathSimDispatcher.ReleasePodFromSimulation(request.SimulationId);

            //var marketFinalizeResponse = _marketWorkflowClient.FinalizeAsync(finalizeRequest);
            //var poseidonFinalizeResponse = _poseidonWorkflowClient.FinalizeAsync(finalizeRequest);
            //var cmsyFinalizeResponse = _cmsyWorkflowClient.FinalizeAsync(finalizeRequest);

            await ecopathFinalizeResponse;
            //await marketFinalizeResponse;
            //await poseidonFinalizeResponse;
            //await cmsyFinalizeResponse;

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

            var updatePriceRequest = new UpdatePricesRequest
            {
                SimulationId = request.SimulationId
            };
            updatePriceRequest.Prices.AddRange(speciesPriceResponse.Prices);

            var ecopathUpdatePricesResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, UpdatePricesRequest, UpdatePricesResponse>(updatePriceRequest, request.SimulationId,
                (client, req) => client.UpdatePricesAsync(req));

            var poseidonUpdatePricesResponse = _poseidonWorkflowClient.UpdatePricesAsync(updatePriceRequest);

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

            var updateBiomassRequest = new UpdateBiomassRequest()
            {
                SimulationId = request.SimulationId,
                MeasurementUnit = getBiomassResponse.MeasurementUnit
            };
            updateBiomassRequest.BiomassGrids.AddRange(getBiomassResponse.BiomassGrids);

            LogStep(current, "Poseidon.UpdateBiomass  (intermediate)");
            var poseidonUpdateBiomassResponse = await _poseidonWorkflowClient.UpdateBiomassAsync(updateBiomassRequest);

            LogStep(current, "Poseidon.SimulateStep");
            var poseidonSimulateStepResponse = await _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest);

            var getCatchDispositionRequest = new GetCatchDispositionSummaryRequest()
            {
                SimulationId = request.SimulationId,
                StartDateTime = Timestamp.FromDateTime(current),
                EndDateTime = Timestamp.FromDateTime(AddStepSize(current, request.StepSize))
            };

            LogStep(current, "Poseidon.GetCatchDisposition");
            var poseidonCatchDispositionSummary = await _poseidonFisheryClient.GetCatchDispositionSummaryAsync(getCatchDispositionRequest);

            var updateCatchDispositionSummaryRequest = new UpdateCatchDispositionSummaryRequest()
            {
                SimulationId = request.SimulationId,
                MeasurementUnit = poseidonCatchDispositionSummary.MeasurementUnit
            };
            updateCatchDispositionSummaryRequest.DispositionGrids.AddRange(poseidonCatchDispositionSummary.DispositionGrids);

            LogStep(current, "Ecopath.UpdateCatchDisposition Summary");
            var catchDispositionResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, UpdateCatchDispositionSummaryRequest, UpdateCatchDispositionSummaryResponse>(updateCatchDispositionSummaryRequest, request.SimulationId,
                (client, req) => client.UpdateCatchDispositionSummaryAsync(req));

            LogStep(current, "Ecopath GetBiomass (total)");
            getBiomassResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(new GetBiomassRequest() { SimulationId = request.SimulationId }, request.SimulationId,
                (client, req) => client.GetBiomassAsync(req));

            updateBiomassRequest.BiomassGrids.Clear();
            updateBiomassRequest.BiomassGrids.AddRange(getBiomassResponse.BiomassGrids);
            LogStep(current, "CMSY++.UpdateBiomass (total)");
            var cmsyUpdateBiomassResponse = await _cmsyWorkflowClient.UpdateBiomassAsync(updateBiomassRequest);

            LogStep(current, "Ecopath.GetCatchDisposition");
            var ecopathCatchDispositionSummary = await _ecopathSimDispatcher.DispatchAsync<FisheryService.FisheryServiceClient, GetCatchDispositionSummaryRequest, GetCatchDispositionSummaryResponse>(getCatchDispositionRequest, request.SimulationId,
                (client, req) => client.GetCatchDispositionSummaryAsync(req));

            updateCatchDispositionSummaryRequest.DispositionGrids.Clear();
            updateCatchDispositionSummaryRequest.DispositionGrids.AddRange(ecopathCatchDispositionSummary.DispositionGrids);
            LogStep(current, "CMSY UpdateCatchDisposition (total)");
            _cmsyEcologyClient.UpdateCatchDispositionSummary(updateCatchDispositionSummaryRequest);

            var getSalesRequest = new GetSalesSummaryRequest()
            {
                SimulationId = request.SimulationId,
                StartDateTime = Timestamp.FromDateTime(current),
                EndDateTime = Timestamp.FromDateTime(AddStepSize(current, request.StepSize))
            };

            LogStep(current, "Ecopath.GetSalesSummary");
            var ecopathGetSalesResponse = await _ecopathSimDispatcher.DispatchAsync<FisheryService.FisheryServiceClient, GetSalesSummaryRequest, GetSalesSummaryResponse>(getSalesRequest, request.SimulationId,
                (client, req) => client.GetSalesSummaryAsync(req));

            // Update Sales to Market
            var updateSalesRequest = new UpdateSalesRequest()
            {
                SimulationId = request.SimulationId,
            };
            updateSalesRequest.SalesSummaries.AddRange(ecopathGetSalesResponse.SalesSummaries);
            LogStep(current, "Market.UpdateSales from Ecopath");
            var marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(updateSalesRequest);

            LogStep(current, "Poseidon.GetSalesSummary");
            var poseidonGetSalesResponse = await _poseidonFisheryClient.GetSalesSummaryAsync(getSalesRequest);

            updateSalesRequest.SalesSummaries.Clear();
            updateSalesRequest.SalesSummaries.AddRange(poseidonGetSalesResponse.SalesSummaries);
            LogStep(current, "Market.UpdateSales from Poseidon");
            marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(updateSalesRequest);

            LogStep(current, "CMSY.SimulateStep");
            var cmsySimulateStepResponse = await _cmsyWorkflowClient.SimulateStepAsync(simulationStepRequest);

            LogStep(current, "Market.SimulateStep");
            var marketSimulateStepResponse = await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest);
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
    }
}
