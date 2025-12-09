using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using System.Collections.Concurrent;
using System.Xml;

namespace SurimiController.Services
{

    public class SimulationManager : ISimulationManager
    {
        private readonly ConcurrentDictionary<string, Models.Simulation> _simulations = new();
        private readonly SimulationDispatcher _ecopathSimDispatcher;
        private readonly ILogger<SimulationManager> _logger;

        private readonly WorkflowService.WorkflowServiceClient _poseidonWorkflowClient;
        private readonly MarketService.MarketServiceClient _poseidonMarketClient;
        private readonly FisheryService.FisheryServiceClient _poseidonFisheryClient;
        private readonly EcologyService.EcologyServiceClient _poseidonEcologyClient;

        private readonly WorkflowService.WorkflowServiceClient _marketWorkflowClient;
        private readonly MarketService.MarketServiceClient _marketMarketClient;

        private readonly WorkflowService.WorkflowServiceClient _cmsyWorkflowClient;
        private readonly EcologyService.EcologyServiceClient _cmsyEcologyClient;
        private readonly FisheryService.FisheryServiceClient _cmsyFisheryClient;

        private readonly WorkflowService.WorkflowServiceClient _valueChainWorkflowClient;
        private readonly MarketService.MarketServiceClient _valueChainMarketClient;

        public SimulationManager(GrpcClientFactory clientFactory, SimulationDispatcher ecopathSimDispatcher, ILogger<SimulationManager> logger)
        {
            _poseidonWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("PoseidonWorkflow");
            _poseidonMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("PoseidonMarket");
            _poseidonEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("PoseidonEcology");
            _poseidonFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("PoseidonFishery");

            _marketWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("MarketWorkflow");
            _marketMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("MarketMarket");

            _cmsyWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow");
            _cmsyEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("CmsyEcology");
            _cmsyFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("CmsyFishery");

            _ecopathSimDispatcher = ecopathSimDispatcher;

            _valueChainWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("ValueChainWorkflow");
            _valueChainMarketClient = clientFactory.CreateClient<MarketService.MarketServiceClient>("ValueChainMarket");

            _logger = logger;
        }

        public Task InitSimulationAsync(string simulationId, string scenarioId, DateTime endDateTime, Grpc.Surimi.Simulation simulation)
        {
            if (_simulations.ContainsKey(simulationId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} is already initialised"));
            }

            var initRequest = new InitialiseRequest
            {
                ScenarioId = scenarioId,
                Simulation = simulation,
                SimulationId = simulationId,
            };

            var ecopathResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, InitialiseRequest, InitialiseResponse>(initRequest, initRequest.SimulationId,
                (client, req) => client.InitialiseAsync(req));

            var poseidonResponse = _poseidonWorkflowClient.InitialiseAsync(initRequest);
            var cmsyResponse = _cmsyWorkflowClient.InitialiseAsync(initRequest);
            var marketResponse = _marketWorkflowClient.InitialiseAsync(initRequest);
            var valueChainResponse = _valueChainWorkflowClient.InitialiseAsync(initRequest);

            // Run the rest of the logic in a background task after all initialisation calls complete
            _ = Task.Run(async () =>
            {
                await Task.WhenAll(
                    ecopathResponse.ResponseAsync,
                    poseidonResponse.ResponseAsync,
                    cmsyResponse.ResponseAsync,
                    marketResponse.ResponseAsync,
                    valueChainResponse.ResponseAsync);

                // Await the response headers
                var headers = await ecopathResponse.ResponseHeadersAsync;

                // Find the header by key (case-insensitive)
                var hostValue = headers.GetValue("host"); // returns null if not found

                _simulations[simulationId] = new Models.Simulation
                {
                    SimulationCreated = DateTime.UtcNow,
                    ScenarioId = scenarioId,
                    StartDateTime = simulation.StartDateTime.ToDateTime(),
                    StepSize = simulation.TimeStep,
                    EndDateTime = endDateTime,
                    Status = "Created",
                    EcologyHost = hostValue ?? string.Empty,
                };

                _logger.LogInformation("Simulation {SimulationId} is created and initialised", simulationId);
            });

            // Return promptly, do not await the initialisation calls
            return Task.CompletedTask;
        }

        public Task RunSimulationAsync(string simulationId, CancellationToken externalToken)
        {
            if (_simulations.ContainsKey(simulationId) == false)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} can not be started. It is not found"));
            }

            if (_simulations[simulationId].Status == "Running")
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} is already running"));
            }

            //if (_simulations[simulationId].CancelIsCalled == true)
            //{
            //    throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} is canceled and can not be started"));
            //}

            var current = _simulations[simulationId].StartDateTime;
            var end = _simulations[simulationId].EndDateTime;

            _simulations[simulationId].Status = "Running";
            var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var task = Task.Run(async () =>
            {
                try
                {
                    while (current <= end)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        await ProcessSimulationStep(simulationId, current, AddStepSize(current, _simulations[simulationId].StepSize), cts.Token);

                        current = AddStepSize(current, _simulations[simulationId].StepSize);

                        //Console.WriteLine($"[Simulation {simulationId}] Running...");
                        //await Task.Delay(1000, cts.Token); // Simulate work
                    }

                    // Finalise the simulation
                    var finaliseRequest = CreateFinaliseRequest(simulationId);

                    var ecopathFinaliseResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, FinaliseRequest, FinaliseResponse>(finaliseRequest, simulationId,
                        (client, req) => client.FinaliseAsync(req));
                    _ecopathSimDispatcher.ReleasePodFromSimulation(simulationId);

                    var marketFinaliseResponse = _marketWorkflowClient.FinaliseAsync(finaliseRequest);
                    var poseidonFinaliseResponse = _poseidonWorkflowClient.FinaliseAsync(finaliseRequest);
                    var cmsyFinaliseResponse = _cmsyWorkflowClient.FinaliseAsync(finaliseRequest);
                    var valueChainFinaliseResponse = _valueChainWorkflowClient.FinaliseAsync(finaliseRequest);

                    await ecopathFinaliseResponse;
                    await marketFinaliseResponse;
                    await poseidonFinaliseResponse;
                    await cmsyFinaliseResponse;
                    await valueChainFinaliseResponse;

                    _simulations[simulationId].Status = "Finished";
                    _logger.LogInformation("{SimulationId} is finished", simulationId);
                }
                catch (RpcException ex) when (ex.InnerException is OperationCanceledException)
                {
                    // cancel the simulation
                    var cancelRequest = CreateCancelRequest(simulationId);

                    var ecopathCancelResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, CancelRequest, CancelResponse>(cancelRequest, simulationId,
                        (client, req) => client.CancelAsync(req));
                    _ecopathSimDispatcher.ReleasePodFromSimulation(simulationId);

                    var marketCancelResponse = _marketWorkflowClient.CancelAsync(cancelRequest);
                    var poseidonCancelResponse = _poseidonWorkflowClient.CancelAsync(cancelRequest);
                    var cmsyCancelResponse = _cmsyWorkflowClient.CancelAsync(cancelRequest);
                    var valueChainCancelResponse = _valueChainWorkflowClient.CancelAsync(cancelRequest);

                    await ecopathCancelResponse;
                    await marketCancelResponse;
                    await poseidonCancelResponse;
                    await cmsyCancelResponse;
                    await valueChainCancelResponse;

                    _simulations[simulationId].Status = "Canceled";
                    _logger.LogInformation("{SimulationId} is canceled", simulationId);
                }
            }, cts.Token);

            _simulations[simulationId].Task = task;
            _simulations[simulationId].Cts = cts;
            return task;
        }

        public void CancelSimulation(string simulationId)
        {
            if (_simulations.ContainsKey(simulationId) == false)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} can not be canceled. It is not found"));
            }

            if (_simulations[simulationId].Cts == null)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} can not be canceled. It is not started"));
            }

            if (_simulations[simulationId].Status != "Running")
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} can not be canceled. Its status is '{_simulations[simulationId].Status}'"));
            }
            _simulations[simulationId].Status = "Canceled";
            _simulations[simulationId].Cts?.Cancel();
        }

        private async Task ProcessSimulationStep(string simulationId, DateTime current, DateTime endStepDateTime, CancellationToken token)
        {
            LogStep(simulationId, current, "Market.GetSpeciesPrices");
            _simulations[simulationId].SimulationCurrent = current;

            var speciesPriceResponse = await _marketMarketClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest() { SimulationId = simulationId }, cancellationToken: token);

            var updatePriceRequest = CreateUpdateSpeciesPricesRequest(speciesPriceResponse);
//            var xx = GetProtoString<UpdateSpeciesPricesRequest>(updatePriceRequest);

            var ecopathUpdatePricesResponse = _ecopathSimDispatcher.DispatchAsync<MarketService.MarketServiceClient, UpdateSpeciesPricesRequest, UpdateSpeciesPricesResponse>(updatePriceRequest, simulationId,
                (client, req) => client.UpdateSpeciesPricesAsync(req, cancellationToken: token));

            var poseidonUpdatePricesResponse = _poseidonMarketClient.UpdateSpeciesPricesAsync(updatePriceRequest, cancellationToken: token);

            LogStep(simulationId, current, "Ecopath.UpdatePrices");
            await ecopathUpdatePricesResponse;
            LogStep(simulationId, current, "Poseidon.UpdatePrices");
            await poseidonUpdatePricesResponse;

            var simulationStepRequest = CreateSimulateStepRequest(simulationId);

            LogStep(simulationId, current, "Ecopath.SimulateStep");
            await _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, SimulateStepRequest, SimulateStepResponse>(simulationStepRequest, simulationId,
                (client, req) => client.SimulateStepAsync(req, cancellationToken: token));

            LogStep(simulationId, current, "Ecopath GetBiomass (intermediate)");
            var getBiomassResponseIntermediate = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(new GetBiomassRequest() { SimulationId = simulationId }, simulationId,
                (client, req) => client.GetBiomassAsync(req, cancellationToken: token));

            // For testing purposes, we can use a fixed biomass response
//            getBiomassResponseIntermediate = GetTestBiomassIntermediate(getBiomassResponseIntermediate);

            var updateBiomassIntermediateRequest = CreateUpdateBiomassRequest(getBiomassResponseIntermediate);

            LogStep(simulationId, current, "Poseidon.UpdateBiomass  (intermediate)");

//            xx = GetProtoString<UpdateBiomassRequest>(updateBiomassIntermediateRequest);

            var poseidonUpdateBiomassResponse = await _poseidonEcologyClient.UpdateBiomassAsync(updateBiomassIntermediateRequest, cancellationToken: token);

            LogStep(simulationId, current, "Poseidon.SimulateStep");
            var poseidonSimulateStepResponse = await _poseidonWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: token);

            var getCatchDispositionRequest = CreateGetCatchDispositionRequest(simulationId, current, endStepDateTime);

            LogStep(simulationId, current, "Poseidon.GetCatchDisposition");
            var poseidonCatchDisposition = await _poseidonFisheryClient.GetCatchDispositionAsync(getCatchDispositionRequest, cancellationToken: token);

            var updateCatchDispositionIntermediateRequest = CreateUpdateCatchDispositionRequest(poseidonCatchDisposition);

            LogStep(simulationId, current, "Ecopath.UpdateCatchDisposition Summary");
            var catchDispositionResponse = await _ecopathSimDispatcher.DispatchAsync<FisheryService.FisheryServiceClient, UpdateCatchDispositionRequest, UpdateCatchDispositionResponse>(updateCatchDispositionIntermediateRequest, simulationId,
                (client, req) => client.UpdateCatchDispositionAsync(req, cancellationToken: token));

            LogStep(simulationId, current, "Ecopath GetBiomass (total)");
            var getBiomassResponseTotal = await _ecopathSimDispatcher.DispatchAsync<EcologyService.EcologyServiceClient, GetBiomassRequest, GetBiomassResponse>(new GetBiomassRequest() { SimulationId = simulationId }, simulationId,
                (client, req) => client.GetBiomassAsync(req, cancellationToken: token));

            var updateBiomassTotalRequest = CreateUpdateBiomassRequest(getBiomassResponseTotal);

            LogStep(simulationId, current, "CMSY++.UpdateBiomass (total)");
            var cmsyUpdateBiomassResponse = await _cmsyEcologyClient.UpdateBiomassAsync(updateBiomassTotalRequest, cancellationToken: token);

            LogStep(simulationId, current, "Ecopath.GetCatchDisposition");
            var ecopathCatchDispositionSummary = await _ecopathSimDispatcher.DispatchAsync<FisheryService.FisheryServiceClient, GetCatchDispositionRequest, GetCatchDispositionResponse>(getCatchDispositionRequest, simulationId,
                (client, req) => client.GetCatchDispositionAsync(req, cancellationToken: token));

            var updateCatchDispositionTotalRequest = CreateUpdateCatchDispositionRequest(ecopathCatchDispositionSummary);

            LogStep(simulationId, current, "CMSY UpdateCatchDisposition (total)");
            await _cmsyFisheryClient.UpdateCatchDispositionAsync(updateCatchDispositionTotalRequest, cancellationToken: token);

            var getSalesRequest = CreateGetSalesRequest(simulationId, current, endStepDateTime);

            LogStep(simulationId, current, "Ecopath.GetSales");
            var ecopathGetSalesResponse = await _ecopathSimDispatcher.DispatchAsync<MarketService.MarketServiceClient, GetSalesRequest, GetSalesResponse>(getSalesRequest, simulationId,
                (client, req) => client.GetSalesAsync(req, cancellationToken: token));

            // Update Sales to Market
            LogStep(simulationId, current, "Market.UpdateSales from Ecopath");
            var marketUpdateSalesResponse = await _marketMarketClient.UpdateSalesAsync(CreateUpdateSalesRequest(ecopathGetSalesResponse), cancellationToken: token);

            LogStep(simulationId, current, "ValueChain.UpdateSales from Ecopath");
            var valueChainUpdateSalesResponse = await _valueChainMarketClient.UpdateSalesAsync(CreateUpdateSalesRequest(ecopathGetSalesResponse), cancellationToken: token);

            LogStep(simulationId, current, "Poseidon.GetSalesSummary");
            var poseidonGetSalesResponse = await _poseidonMarketClient.GetSalesAsync(getSalesRequest, cancellationToken: token);

            LogStep(simulationId, current, "Market.UpdateSales from Poseidon");
            marketUpdateSalesResponse = await _marketMarketClient.UpdateSalesAsync(CreateUpdateSalesRequest(poseidonGetSalesResponse), cancellationToken: token);

            LogStep(simulationId, current, "ValueChain.UpdateSales from Poseidon");
            valueChainUpdateSalesResponse = await _valueChainMarketClient.UpdateSalesAsync(CreateUpdateSalesRequest(poseidonGetSalesResponse), cancellationToken: token);

            LogStep(simulationId, current, "CMSY.SimulateStep");
            var cmsySimulateStepResponse = await _cmsyWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: token);

            LogStep(simulationId, current, "Market.SimulateStep");
            var marketSimulateStepResponse = await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: token);

            LogStep(simulationId, current, "ValueChain.SimulateStep");
            var valueChainSimulateStepResponse = await _valueChainWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: token);
        }

        private string GetProtoString<T>(object obj)
        {
            System.Text.Json.JsonSerializerOptions jsonOptions = new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
            };

            string resultaat = System.Text.Json.JsonSerializer.Serialize(obj, jsonOptions);
            return resultaat;
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

        private UpdateBiomassRequest CreateUpdateBiomassRequest(GetBiomassResponse getBiomassResponse)
        {
            var updateRequest = new UpdateBiomassRequest
            {
                SimulationId = getBiomassResponse.SimulationId,
                BiomassSummary = getBiomassResponse.BiomassSummary
            };

            // TODO: Remove this when the issue is fixed in Ecopath. This is a workaround to TEST
            foreach (var grid in updateRequest.BiomassSummary.BiomassGrids)
            {
                foreach (var cell in grid.BiomassCells)
                {
                    if (cell.Biomass < 0)
                    {
                        _logger.LogWarning("GetBiomassResponse of Simulation {SimulationId} has a cell with a negative Biomass {Biomass}. It is set to 0. Fix this.", getBiomassResponse.SimulationId, cell.Biomass);
                        cell.Biomass = 0;
                    }
                }
            }
            return updateRequest;
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

        public Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetAllSimulationStatusesResponse
            {
                SimulationStatuses =
                {
                    _simulations.Select(sim => new Grpc.Surimi.SimulationStatus
                    {
                        SimulationId = sim.Key,
                        //ScenarioId = sim.Value.ScenarioId,
                        StartDateTime = Timestamp.FromDateTime(sim.Value.StartDateTime),
                        //StepSize = sim.Value.StepSize,
                        EndDateTime = Timestamp.FromDateTime(sim.Value.EndDateTime),
                        Status = sim.Value.Status,
                        SimulationCurrent = sim.Value.SimulationCurrent == default ? null : Timestamp.FromDateTime(sim.Value.SimulationCurrent),
                        SimulationCreated = sim.Value.SimulationCreated == default ? null : Timestamp.FromDateTime(sim.Value.SimulationCreated),
                        EcologyHost = sim.Value.EcologyHost
                    }).OrderByDescending(x => x.SimulationCreated).ToList()
                }
            });
        }
    }
}
