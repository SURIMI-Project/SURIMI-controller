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

            if (_simulations.ContainsKey(request.Simulation.SimulationId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {request.Simulation.SimulationId} is already initialised"));
            }

            _logger.LogInformation("{SimulationId} Initializing scenario {ScenarioId}", request.Simulation.SimulationId, request.Simulation.ScenarioId);
            var initRequest = new InitialiseRequest
            {
                ScenarioId = request.Simulation.ScenarioId,
                StartDateTime = request.Simulation.StartDateTime,
                StepSize = request.Simulation.StepSize,
                SimulationId = request.Simulation.SimulationId
            };

            var ecopathResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, InitialiseRequest, InitialiseResponse>(initRequest, initRequest.SimulationId,
                (client, req) => client.InitialiseAsync(req));

            var poseidonResponse = _poseidonWorkflowClient.InitialiseAsync(initRequest);
            var cmsyResponse = _cmsyWorkflowClient.InitialiseAsync(initRequest);
            var marketResponse = _marketWorkflowClient.InitialiseAsync(initRequest);

            activity?.AddEvent(new ActivityEvent("Start Init ecopath and poseidon"));
            await ecopathResponse;
            await poseidonResponse;
            await cmsyResponse;
            await marketResponse;

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
                Duration = request.Simulation.SimulationDuration,
                Status = "Initialised",
                EcologyHost = hostValue ?? string.Empty,
                SimulationCreated = DateTime.UtcNow,
                CancelIsCalled = false,
            };

            activity?.AddEvent(new ActivityEvent("Finished ecopoath and poseidon"));
            return new InitSimulationResponse() { SimulationId = request.Simulation.SimulationId };
        }

        public override async Task<RunSimulationResponse> RunSimulation(RunSimulationRequest request, ServerCallContext context)
        {
            using var activity = _activitySource.StartActivity("RunSimulation");
            _logger.LogInformation("{SimulationId} Running simulation...", request.SimulationId);

            if (_simulations.ContainsKey(request.SimulationId) == false)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {request.SimulationId} can not be started. It is not found"));
            }

            if (_simulations[request.SimulationId].Status == "Running")
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {request.SimulationId} is already running"));
            }

            if (_simulations[request.SimulationId].CancelIsCalled == true)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {request.SimulationId} is canceled and can not be started"));
            }

            _simulations[request.SimulationId].Status = "Running";

            var current = _simulations[request.SimulationId].StartDateTime;
            var end = current.Add(XmlConvert.ToTimeSpan(_simulations[request.SimulationId].Duration));

            while (current <= end && _simulations[request.SimulationId].CancelIsCalled == false)
            {
                await ProcessSimulationStep(request.SimulationId, current, AddStepSize(current, _simulations[request.SimulationId].StepSize));

                current = AddStepSize(current, _simulations[request.SimulationId].StepSize);
            }

            if (_simulations[request.SimulationId].CancelIsCalled == false)
            {
                var createStockAssessmentresponse = _cmsyStockAssessmentClient.CreateStockAssessmentAsync(new CreateStockAssessmentRequest() { SimulationId = request.SimulationId });

                // Finalise the simulation
                var finaliseRequest = CreateFinaliseRequest(request.SimulationId);

                var ecopathFinaliseResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, FinaliseRequest, FinaliseResponse>(finaliseRequest, request.SimulationId,
                    (client, req) => client.FinaliseAsync(req));
                _ecopathSimDispatcher.ReleasePodFromSimulation(request.SimulationId);

                var marketFinaliseResponse = _marketWorkflowClient.FinaliseAsync(finaliseRequest);
                var poseidonFinaliseResponse = _poseidonWorkflowClient.FinaliseAsync(finaliseRequest);
                var cmsyFinaliseResponse = _cmsyWorkflowClient.FinaliseAsync(finaliseRequest);

                await ecopathFinaliseResponse;
                await marketFinaliseResponse;
                await poseidonFinaliseResponse;
                await cmsyFinaliseResponse;
                _simulations[request.SimulationId].Status = "Finished";
                _logger.LogInformation("{SimulationId} is finished", request.SimulationId);
            }
            else
            {
                // cancel the simulation
                var cancelRequest = CreateCancelRequest(request.SimulationId);

                var ecopathCancelResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, CancelRequest, CancelResponse>(cancelRequest, request.SimulationId,
                    (client, req) => client.CancelAsync(req));
                _ecopathSimDispatcher.ReleasePodFromSimulation(request.SimulationId);

                var marketCancelResponse = _marketWorkflowClient.CancelAsync(cancelRequest);
                var poseidonCancelResponse = _poseidonWorkflowClient.CancelAsync(cancelRequest);
                var cmsyCancelResponse = _cmsyWorkflowClient.CancelAsync(cancelRequest);

                await ecopathCancelResponse;
                await marketCancelResponse;
                await poseidonCancelResponse;
                await cmsyCancelResponse;
                _simulations[request.SimulationId].Status = "Canceled";
                _logger.LogInformation("{SimulationId} is canceled", request.SimulationId);
            }

            return new RunSimulationResponse();
        }

        public override Task<CancelSimulationResponse> CancelSimulation(CancelSimulationRequest request, ServerCallContext context)
        {
            if (_simulations.ContainsKey(request.SimulationId) == false)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {request.SimulationId} can not be canceled. It is not found"));
            }

            if (_simulations[request.SimulationId].Status != "Running")
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {request.SimulationId} can not be canceled. Its status is '{_simulations[request.SimulationId].Status}'"));
            }

            _logger.LogInformation("{SimulationId} Cancel Simulation", request.SimulationId);
            _simulations[request.SimulationId].CancelIsCalled = true;

            return Task.FromResult(new CancelSimulationResponse());
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

        private async Task ProcessSimulationStep(string simulationId, DateTime current, DateTime endStepDateTime)
        {
            try
            {
                LogStep(simulationId, current, "Market.GetSpeciesPrices");
                _simulations[simulationId].SimulationCurrent = current;

                var speciesPriceResponse = await _marketClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest() { SimulationId = simulationId });

                var updatePriceRequest = CreateUpdateSpeciesPricesRequest(speciesPriceResponse);

                var ecopathUpdatePricesResponse = _ecopathSimDispatcher.DispatchAsync<MarketService.MarketServiceClient, UpdateSpeciesPricesRequest, UpdateSpeciesPricesResponse>(updatePriceRequest, simulationId,
                    (client, req) => client.UpdateSpeciesPricesAsync(req));

                var poseidonUpdatePricesResponse = _poseidonMarketClient.UpdateSpeciesPricesAsync(updatePriceRequest);

                LogStep(simulationId, current, "Ecopath.UpdatePrices");
                await ecopathUpdatePricesResponse;
                LogStep(simulationId, current, "Poseidon.UpdatePrices");
                await poseidonUpdatePricesResponse;

                var simulationStepRequest = CreateSimulateStepRequest(simulationId);

                LogStep(simulationId, current, "Ecopath.SimulateStep");
                await _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, SimulateStepRequest, SimulateStepResponse>(simulationStepRequest, simulationId,
                    (client, req) => client.SimulateStepAsync(req));

                LogStep(simulationId, current, "Ecopath GetBiomass (intermediate)");
                var getBiomassResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(new GetBiomassRequest() { SimulationId = simulationId }, simulationId,
                    (client, req) => client.GetBiomassAsync(req));

                getBiomassResponse = GetTestBiomassIntermediate(getBiomassResponse);

                var updateBiomassIntermediateRequest = CreateUpdateBiomassRequest(getBiomassResponse);

                LogStep(simulationId, current, "Poseidon.UpdateBiomass  (intermediate)");
                var poseidonUpdateBiomassResponse = await _poseidonEcologyClient.UpdateBiomassAsync(updateBiomassIntermediateRequest);

                LogStep(simulationId, current, "Poseidon.SimulateStep");
                var poseidonSimulateStepResponse = await _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest);

                var getCatchDispositionRequest = CreateGetCatchDispositionRequest(simulationId, current, endStepDateTime);

                LogStep(simulationId, current, "Poseidon.GetCatchDisposition");
                var poseidonCatchDisposition = await _poseidonFisheryClient.GetCatchDispositionAsync(getCatchDispositionRequest);

                var updateCatchDispositionIntermediateRequest = CreateUpdateCatchDispositionRequest(poseidonCatchDisposition);

                LogStep(simulationId, current, "Ecopath.UpdateCatchDisposition Summary");
                var catchDispositionResponse = await _ecopathSimDispatcher.DispatchAsync<FisheryService.FisheryServiceClient, UpdateCatchDispositionRequest, UpdateCatchDispositionResponse>(updateCatchDispositionIntermediateRequest, simulationId,
                    (client, req) => client.UpdateCatchDispositionAsync(req));

                LogStep(simulationId, current, "Ecopath GetBiomass (total)");
                getBiomassResponse = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(new GetBiomassRequest() { SimulationId = simulationId }, simulationId,
                    (client, req) => client.GetBiomassAsync(req));

                var updateBiomassTotalRequest = CreateUpdateBiomassRequest(getBiomassResponse);

                LogStep(simulationId, current, "CMSY++.UpdateBiomass (total)");
                var cmsyUpdateBiomassResponse = await _cmsyEcologyClient.UpdateBiomassAsync(updateBiomassTotalRequest);

                LogStep(simulationId, current, "Ecopath.GetCatchDisposition");
                var ecopathCatchDispositionSummary = await _ecopathSimDispatcher.DispatchAsync<FisheryService.FisheryServiceClient, GetCatchDispositionRequest, GetCatchDispositionResponse>(getCatchDispositionRequest, simulationId,
                    (client, req) => client.GetCatchDispositionAsync(req));

                var updateCatchDispositionTotalRequest = CreateUpdateCatchDispositionRequest(ecopathCatchDispositionSummary);

                LogStep(simulationId, current, "CMSY UpdateCatchDisposition (total)");
                await _cmsyFisheryClient.UpdateCatchDispositionAsync(updateCatchDispositionTotalRequest);

                var getSalesRequest = CreateGetSalesRequest(simulationId, current, endStepDateTime);

                LogStep(simulationId, current, "Ecopath.GetSales");
                var ecopathGetSalesResponse = await _ecopathSimDispatcher.DispatchAsync<MarketService.MarketServiceClient, GetSalesRequest, GetSalesResponse>(getSalesRequest, simulationId,
                    (client, req) => client.GetSalesAsync(req));

                // Update Sales to Market
                LogStep(simulationId, current, "Market.UpdateSales from Ecopath");
                var marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(CreateUpdateSalesRequest(ecopathGetSalesResponse));

                LogStep(simulationId, current, "Poseidon.GetSalesSummary");
                var poseidonGetSalesResponse = await _poseidonMarketClient.GetSalesAsync(getSalesRequest);

                LogStep(simulationId, current, "Market.UpdateSales from Poseidon");
                marketUpdateSalesResponse = await _marketClient.UpdateSalesAsync(CreateUpdateSalesRequest(poseidonGetSalesResponse));

                LogStep(simulationId, current, "CMSY.SimulateStep");
                var cmsySimulateStepResponse = await _cmsyWorkflowClient.SimulateStepAsync(simulationStepRequest);

                LogStep(simulationId, current, "Market.SimulateStep");
                var marketSimulateStepResponse = await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest);
            }
            catch (Exception ex)
            {

                throw;
            }
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
                            SpeciesCode = "PIL"
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

        private void LogStep(string simulationId, DateTime current, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step {Step}. {DateTime}", simulationId, step, current);
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

        public static UpdateSalesRequest CreateUpdateSalesRequest(GetSalesResponse response)
        {
            return new UpdateSalesRequest
            {
                SimulationId = response.SimulationId,
                SalesSummaries = { response.SalesSummaries }
            };
        }

        public static GetSalesRequest CreateGetSalesRequest(string simulationId, DateTime startDateTime, DateTime endDateTime)
        {
            return new GetSalesRequest
            {
                SimulationId = simulationId,
                StartDateTime = Timestamp.FromDateTime(startDateTime),
                EndDateTime = Timestamp.FromDateTime(endDateTime)
            };
        }

        private static UpdateCatchDispositionRequest CreateUpdateCatchDispositionRequest(GetCatchDispositionResponse catchDisposition)
        {
            return new UpdateCatchDispositionRequest()
            {
                SimulationId = catchDisposition.SimulationId,
                CatchDispositionSummary = catchDisposition.CatchDispositionSummary
            };
        }

        private static GetCatchDispositionRequest CreateGetCatchDispositionRequest(string simulationId, DateTime startDateTime, DateTime endDateTime)
        {
            return new GetCatchDispositionRequest
            {
                SimulationId = simulationId,
                StartDateTime = Timestamp.FromDateTime(startDateTime),
                EndDateTime = Timestamp.FromDateTime(endDateTime)
            };
        }

        private static UpdateBiomassRequest CreateUpdateBiomassRequest(GetBiomassResponse getBiomassResponse)
        {
            return new UpdateBiomassRequest
            {
                SimulationId = getBiomassResponse.SimulationId,
                BiomassSummary = getBiomassResponse.BiomassSummary
            };
        }

        private static SimulateStepRequest CreateSimulateStepRequest(string simulationId)
        {
            return new SimulateStepRequest
            {
                SimulationId = simulationId
            };
        }

        private static CancelRequest CreateCancelRequest(string simulationId)
        {
            return new CancelRequest
            {
                SimulationId = simulationId
            };
        }

        private static FinaliseRequest CreateFinaliseRequest(string simulationId)
        {
            return new FinaliseRequest
            {
                SimulationId = simulationId
            };
        }

        private static UpdateSpeciesPricesRequest CreateUpdateSpeciesPricesRequest(GetSpeciesPricesResponse speciesPriceResponse)
        {
            return new UpdateSpeciesPricesRequest()
            {
                SimulationId = speciesPriceResponse.SimulationId,
                Prices = { speciesPriceResponse.Prices }
            };
        }
    }
}
