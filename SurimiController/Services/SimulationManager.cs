using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Initialises a new simulation
        /// </summary>
        /// <param name="simulationId">The unique identifier for the simulation</param>
        /// <param name="scenarioId">The identifier for the scenario</param>
        /// <param name="endDateTime">An optional end date and time for the simulation. </param>
        /// <param name="simulation">The simulation details</param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public Task InitSimulationAsync(string simulationId, string scenarioId, DateTime? endDateTime, Grpc.Surimi.Simulation simulation)
        {
            if (_simulations.ContainsKey(simulationId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} is already initialised"));
            }

            var initRequest = new InitialiseRequest
            {
                SimulationId = simulationId,
                ScenarioId = scenarioId,
                Simulation = simulation
            };

            var ecopathResponse = _ecopathSimDispatcher.DispatchAsync<WorkflowService.WorkflowServiceClient, InitialiseRequest, InitialiseResponse>(initRequest, initRequest.SimulationId,
                (client, req) => client.InitialiseAsync(req));

            var poseidonResponse = _poseidonWorkflowClient.InitialiseAsync(initRequest);
            var cmsyResponse = _cmsyWorkflowClient.InitialiseAsync(initRequest);
            var marketResponse = _marketWorkflowClient.InitialiseAsync(initRequest);
            var valueChainResponse = _valueChainWorkflowClient.InitialiseAsync(initRequest);

            _simulations[simulationId] = new Models.Simulation
            {
                ScenarioId = scenarioId,
                StartDateTime = simulation.StartDateTime.ToDateTime(),
                StepSize = simulation.TimeStep,
                // If an endDateTime is provided, use the minimum of that and the MaximumEndDateTime from the simulation details
                EndDateTime = endDateTime.HasValue
                    ? (endDateTime.Value > simulation.MaximumEndDateTime.ToDateTime() ? simulation.MaximumEndDateTime.ToDateTime() : endDateTime.Value)
                    : simulation.MaximumEndDateTime.ToDateTime(),
                Status = "Initializing",
                EcologyHost = string.Empty,
                Order = _simulations.Count + 1
            };

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

                _simulations[simulationId].Status = "Initialised";
                _simulations[simulationId].EcologyHost = hostValue ?? string.Empty;

                if(simulationId.Equals(ecopathResponse.ResponseAsync.Result.SimulationId) == false)
                {
                    _logger.LogWarning("SimulationId mismatch after initialisation for simulation {SimulationId}", simulationId);
                }
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
            _simulations[simulationId].SimulationStarted = DateTime.UtcNow;
            _simulations[simulationId].SimulationDuration = TimeSpan.FromMilliseconds(10);    // so you immediately see a duration, instead of nothing
            var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var task = Task.Run(async () =>
            {
                try
                {
                    while (current <= end)
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        await ProcessSimulationStep(simulationId, current, AddStepSize(current, _simulations[simulationId].StepSize), cts.Token);
                        _simulations[simulationId].SimulationDuration = DateTime.UtcNow - _simulations[simulationId].SimulationStarted;

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
                    _simulations[simulationId].SimulationDuration = DateTime.UtcNow - _simulations[simulationId].SimulationStarted;
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
            try
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
                var ecopathUpdateSalesRequest = CreateUpdateSalesRequest(ecopathGetSalesResponse, current, endStepDateTime);
                LogStep(simulationId, current, "Market.UpdateSales from Ecopath");
                var marketUpdateSalesResponse = await _marketMarketClient.UpdateSalesAsync(ecopathUpdateSalesRequest, cancellationToken: token);

                LogStep(simulationId, current, "ValueChain.UpdateSales from Ecopath");
                var valueChainUpdateSalesResponse = await _valueChainMarketClient.UpdateSalesAsync(ecopathUpdateSalesRequest, cancellationToken: token);

                LogStep(simulationId, current, "Poseidon.GetSalesSummary");
                var poseidonGetSalesResponse = await _poseidonMarketClient.GetSalesAsync(getSalesRequest, cancellationToken: token);

                var poseidonUpdateSalesRequest = CreateUpdateSalesRequest(poseidonGetSalesResponse, current, endStepDateTime);
                LogStep(simulationId, current, "Market.UpdateSales from Poseidon");
                marketUpdateSalesResponse = await _marketMarketClient.UpdateSalesAsync(poseidonUpdateSalesRequest, cancellationToken: token);

                LogStep(simulationId, current, "ValueChain.UpdateSales from Poseidon");
                valueChainUpdateSalesResponse = await _valueChainMarketClient.UpdateSalesAsync(poseidonUpdateSalesRequest, cancellationToken: token);

                LogStep(simulationId, current, "CMSY.SimulateStep");
                var cmsySimulateStepResponse = await _cmsyWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: token);

                LogStep(simulationId, current, "Market.SimulateStep");
                var marketSimulateStepResponse = await _marketWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: token);

                LogStep(simulationId, current, "ValueChain.SimulateStep");
                var valueChainSimulateStepResponse = await _valueChainWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: token);
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error in ProcessSimulationStep. Error: {ex.Message}"));
            }
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
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("input must not be null or whitespace.", nameof(input));

            // ISO 8601 months only: "P<n>M" (period with months, without a time component)
            // Example: "P1M" = add 1 month
            // NOTE: Do NOT confuse with "PT<n>M" which means minutes.
            var monthsMatch = Regex.Match(input, @"^P(?<m>\d+)M$");
            if (monthsMatch.Success)
            {
                int months = int.Parse(monthsMatch.Groups["m"].Value);

                // Add months. When adding months, day-of-month can drift if the current day
                // doesn't exist in the target month (e.g., starting on the 31st).
                // To guarantee sequences like 01-01 → 01-02 → 01-03 → 01-04,
                // normalize to the first day of the resulting month.
                var next = current.AddMonths(months);
                return new DateTime(
                    next.Year, next.Month, 1,
                    current.Hour, current.Minute, current.Second,
                    current.Kind
                );
            }

            // ISO 8601 years only: "P<n>Y"
            // Example: "P2Y" = add 2 years
            var yearsMatch = Regex.Match(input, @"^P(?<y>\d+)Y$");
            if (yearsMatch.Success)
            {
                int years = int.Parse(yearsMatch.Groups["y"].Value);

                // Same normalization as for months: set day to 1 to avoid day drift across months/years.
                var next = current.AddYears(years);
                return new DateTime(
                    next.Year, next.Month, 1,
                    current.Hour, current.Minute, current.Second,
                    current.Kind
                );
            }

            // For all other ISO 8601 durations supported by XmlConvert.ToTimeSpan:
            // - "P<n>D"  => days
            // - "PT<n>H" => hours
            // - "PT<n>M" => minutes
            // - "PT<n>S" => seconds
            //
            // IMPORTANT: XmlConvert.ToTimeSpan does NOT support months or years,
            // which is why those are handled explicitly above.
            var ts = XmlConvert.ToTimeSpan(input);
            return current + ts;
        }

        public static UpdateSalesRequest CreateUpdateSalesRequest(GetSalesResponse response, DateTime startDateTime, DateTime endDateTime)
        {
            return new UpdateSalesRequest
            {
                SimulationId = response.SimulationId,
                StartDateTime = Timestamp.FromDateTime(startDateTime),
                EndDateTime = Timestamp.FromDateTime(endDateTime),
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
                        ScenarioId = sim.Value.ScenarioId,
                        StartDateTime = Timestamp.FromDateTime(sim.Value.StartDateTime),
                        //StepSize = sim.Value.StepSize,
                        EndDateTime = Timestamp.FromDateTime(sim.Value.EndDateTime),
                        Status = sim.Value.Status,
                        SimulationCurrent = sim.Value.SimulationCurrent == default ? null : Timestamp.FromDateTime(sim.Value.SimulationCurrent),
                        SimulationStarted = sim.Value.SimulationStarted == default ? null : Timestamp.FromDateTime(sim.Value.SimulationStarted),
                        SimulationDuration = sim.Value.SimulationDuration == default ? null : Duration.FromTimeSpan(sim.Value.SimulationDuration),
                        EcologyHost = sim.Value.EcologyHost,
                        Order = sim.Value.Order
                    }).OrderByDescending(sim => sim.Order).ToList()
                }
            });
        }
    }
}
