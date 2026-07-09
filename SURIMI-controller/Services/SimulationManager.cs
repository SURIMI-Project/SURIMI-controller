using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Surimi;
using SURIMI_controller.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Xml;

namespace SURIMI_controller.Services
{

    public class SimulationManager : ISimulationManager
    {
        private readonly ConcurrentDictionary<string, Models.Simulation> _simulations = new();
        private readonly ILogger<SimulationManager> _logger;

        private readonly IEcopathServiceClient _ecopathServiceClient;
        private readonly IPoseidonServiceClient _poseidonServiceClient;
        private readonly IMarketServiceClient _marketServiceClient;
        private readonly IFisheriesAuthorityServiceClient _fisheriesAuthorityServiceClient;
        private readonly IEnvironmentServiceClient _environmentServiceClient;

        // Event declaration
        public event EventHandler<BiomassEventArgs>? BiomassUpdated;
        public event EventHandler<CatchDispositionEventArgs>? CatchDispositionUpdated;
        public event EventHandler<SalesEventArgs>? SalesUpdated;
        public event EventHandler<FishingActivityEventArgs>? FishingActivityUpdated;
        public event EventHandler<SpeciesPriceEventArgs>? SpeciesPriceUpdated;
        public event EventHandler<ExperimentEventArgs>? SimulateStep;
        public event EventHandler<ExperimentEventArgs>? SimulationFinalised;
        public event EventHandler<ExperimentEventArgs>? SimulationCancelled;

        public SimulationManager(ILogger<SimulationManager> logger, IMarketServiceClient marketServiceClient, IPoseidonServiceClient poseidonServiceClient, IEcopathServiceClient ecopathServiceClient, IFisheriesAuthorityServiceClient fisheriesAuthorityServiceClient, IEnvironmentServiceClient environmentServiceClient)
        {
            _logger = logger;
            _marketServiceClient = marketServiceClient;
            _poseidonServiceClient = poseidonServiceClient;
            _ecopathServiceClient = ecopathServiceClient;
            _fisheriesAuthorityServiceClient = fisheriesAuthorityServiceClient;
            _environmentServiceClient = environmentServiceClient;
        }

        /// <summary>
        /// Initialise a new simulation
        /// </summary>
        /// <param name="simulationId">The unique identifier for the simulation</param>
        /// <param name="scenarioName">The identifier for the scenario</param>
        /// <param name="endDateTime">An optional end date and time for the simulation. </param>
        /// <param name="simulation">The simulation details</param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public async Task InitSimulationAsync(string simulationId, string experimentId, string scenarioName, DateTime? endDateTime, Grpc.Surimi.Simulation simulation, CancellationToken cancellationToken)
        {
            if (_simulations.ContainsKey(simulationId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} is already running"));
            }

            var initRequest = new InitialiseSimulationRequest
            {
                SimulationId = simulationId,
                ScenarioName = scenarioName,
                EndDateTime = endDateTime.HasValue ? Timestamp.FromDateTime(endDateTime.Value) : null,
                Simulation = simulation
            };

            var xx = GetProtoString<InitialiseSimulationRequest>(initRequest);

            var simulationInitialisationTasks = new List<Task<InitialiseSimulationResponse>>();

            var ecopathInitialiseSimulationResponse = _ecopathServiceClient.AddInitialise(simulationInitialisationTasks, initRequest, cancellationToken);
            _ = _poseidonServiceClient.AddInitialise(simulationInitialisationTasks, initRequest, cancellationToken);
            _ = _marketServiceClient.AddInitialise(simulationInitialisationTasks, initRequest, cancellationToken);
            _ = _fisheriesAuthorityServiceClient.AddInitialise(simulationInitialisationTasks, initRequest, cancellationToken);
            _simulations[simulationId] = new Models.Simulation
            {
                ScenarioName = scenarioName,
                ExperimentId = experimentId,
                StartDateTime = simulation.StartDateTime.ToDateTime(),
                StepSize = simulation.TimeStep,
                // If an endDateTime is provided, use the minimum of that and the MaximumEndDateTime from the simulation details
                EndDateTime = endDateTime.HasValue
                    ? (endDateTime.Value > simulation.MaximumEndDateTime.ToDateTime() ? simulation.MaximumEndDateTime.ToDateTime() : endDateTime.Value)
                    : simulation.MaximumEndDateTime.ToDateTime(),
                Status = "Initializing",
                SimulationStarted = DateTime.UtcNow,
                EcologyHost = string.Empty,
                Order = _simulations.Count + 1
            };

            await Task.WhenAll(simulationInitialisationTasks);
            _logger.LogInformation("{NrOfTasks} initialisation calls completed for simulation {SimulationId}", simulationInitialisationTasks.Count, simulationId);

            string hostValue = string.Empty;
            if (ecopathInitialiseSimulationResponse != null)
            {
                // Await the response headers
                var headers = await ecopathInitialiseSimulationResponse.ResponseHeadersAsync;

                // Find the header by key (case-insensitive)
                hostValue = headers.GetValue("host") ?? string.Empty; // returns string.Empty if not found
            }

            _simulations[simulationId].EcologyHost = hostValue ?? string.Empty;
            _simulations[simulationId].Status = "Initialised";

            _logger.LogInformation("Simulation {SimulationId} is created and initialised on {EcologyHost}", simulationId, hostValue);

            // Return promptly, do not await the task to prevent the gRPC call from timing out. The simulation will continue to run in the background, and its progress can be tracked through the SimulationManager's state.
        }

        /// <summary>
        /// Run a simulation
        /// </summary>
        /// <param name="simulationId">The unique identifier for the simulation</param>
        /// <param name="scenarioName">The identifier for the scenario</param>
        /// <param name="endDateTime">An optional end date and time for the simulation. </param>
        /// <param name="simulation">The simulation details</param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public async Task RunSimulationAsync(string simulationId, string scenarioName, Grpc.Surimi.RegulationDefinitionsSummary regulationsSummary, CancellationToken cancellationToken)
        {
            if (!_simulations.ContainsKey(simulationId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Simulation with Id {simulationId} is not found"));
            }
            var simulation = _simulations[simulationId];

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            simulation.Cts = cts;

            await _fisheriesAuthorityServiceClient.CreateRegulationsAsync(new CreateRegulationsRequest()
            {
                SimulationId = simulationId,
                RegulationsSummary = regulationsSummary
            }, cts.Token);

            var current = simulation.StartDateTime;
            var end = simulation.EndDateTime;

            simulation.Status = "Running";
            simulation.SimulationDuration = TimeSpan.FromMilliseconds(10);    // so you immediately see a duration, instead of nothing

            try
            {
                while (current <= end)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    await ProcessSimulationStep(simulationId, current, AddStepSize(current, simulation.StepSize), cts.Token);
                    simulation.SimulationDuration = DateTime.UtcNow - simulation.SimulationStarted;

                    current = AddStepSize(current, simulation.StepSize);
                }

                // Finalise the simulation
                var finaliseSimulationRequest = CreateFinaliseSimulationRequest(simulationId);

                await Task.WhenAll(
                    _ecopathServiceClient.FinaliseSimulationAsync(finaliseSimulationRequest, cts.Token),
                    _poseidonServiceClient.FinaliseSimulationAsync(finaliseSimulationRequest, cts.Token),
                    _marketServiceClient.FinaliseSimulationAsync(finaliseSimulationRequest, cts.Token),
                    _fisheriesAuthorityServiceClient.FinaliseSimulationAsync(finaliseSimulationRequest, cts.Token));

                simulation.Status = "Finished";
                simulation.SimulationDuration = DateTime.UtcNow - simulation.SimulationStarted;
                _logger.LogInformation("Simulation {SimulationId} is finished", simulationId);
                SimulationFinalised?.Invoke(this, new ExperimentEventArgs(simulation.ExperimentId, simulationId, end, cts.Token));
            }
            catch (RpcException ex) when (ex.InnerException is OperationCanceledException)
            {
                // cancel the simulation — use CancellationToken.None because cts is already cancelled at this point
                var cancelRequest = CreateCancelRequest(simulationId);

                await Task.WhenAll(
                    _poseidonServiceClient.CancelSimulationAsync(cancelRequest, CancellationToken.None),
                    _ecopathServiceClient.CancelSimulationAsync(cancelRequest, CancellationToken.None),
                    _marketServiceClient.CancelSimulationAsync(cancelRequest, CancellationToken.None),
                    _fisheriesAuthorityServiceClient.CancelSimulationAsync(cancelRequest, CancellationToken.None));

                simulation.Status = "Canceled";
                _logger.LogInformation("Simulation {SimulationId} is canceled", simulationId);
                SimulationCancelled?.Invoke(this, new ExperimentEventArgs(simulation.ExperimentId, simulationId, DateTime.UtcNow, CancellationToken.None));
            }
            catch (RpcException ex)
            {
                simulation.Status = "Error";
                _logger.LogError(ex, "{SimulationId} encountered RpcException. StatusCode: {StatusCode}, Method: {Method}",
                        simulationId, ex.StatusCode, ex.Status.Detail);
                throw;
            }
            catch (Exception ex)
            {
                simulation.Status = "Error";
                _logger.LogError(ex, "{SimulationId} encountered an Exception", simulationId);
                throw;
            }
        }

        public Task CancelSimulationAsync(string simulationId)
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
            return Task.CompletedTask;
        }

        private async Task ProcessSimulationStep(string simulationId, DateTime current, DateTime endStepDateTime, CancellationToken token)
        {
            var experimentEventArgs = new ExperimentEventArgs(_simulations[simulationId].ExperimentId, simulationId, current, token);

            // check if it's the first step of a new year (assuming steps are in monthly intervals), by comparing the month of the current step with the month of the last processed step (SimulationCurrent).
            // If it's a new year, get and update regulations, as they can be different each year (or if it's the first step of the simulation, as indicated by SimulationCurrent being DateTime.MinValue, then also get and update regulations)
            if (current.Month < _simulations[simulationId].SimulationCurrent.Month || _simulations[simulationId].SimulationCurrent == DateTime.MinValue)
            {
                _logger.LogInformation("{SimulationId} It's a new year {Year}", simulationId, current.Year);
                var regulationsResponse = await _fisheriesAuthorityServiceClient.GetRegulationsAsync(new GetRegulationsRequest() 
                { 
                    SimulationId = simulationId, 
                    StartDateTime = current.ToTimestamp(), 
                    EndDateTime = endStepDateTime.ToTimestamp() 
                }, current, cancellationToken: token);

                var updateRegulationsRequest = CreateUpdateRegulationsRequest(regulationsResponse);
//                var xxx = GetProtoString<UpdateRegulationsRequest>(updateRegulationsRequest);

                await _ecopathServiceClient.UpdateRegulationsAsync(updateRegulationsRequest, current, cancellationToken: token);
                await _poseidonServiceClient.UpdateRegulationsAsync(updateRegulationsRequest, current, cancellationToken: token);
            }
            _simulations[simulationId].SimulationCurrent = current;

            var speciesPriceResponse = await _marketServiceClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest() { SimulationId = simulationId, DateTime = current.ToTimestamp() }, current, cancellationToken: token);
            SpeciesPriceUpdated?.Invoke(this, new SpeciesPriceEventArgs(speciesPriceResponse.SpeciesPriceSummary, experimentEventArgs));

            var updatePriceRequest = CreateUpdateSpeciesPricesRequest(speciesPriceResponse);
            var xxx = GetProtoString<UpdateSpeciesPricesRequest>(updatePriceRequest);

            await _poseidonServiceClient.UpdateSpeciesPricesAsync(updatePriceRequest, current, cancellationToken: token);
            await _ecopathServiceClient.UpdateSpeciesPricesAsync(updatePriceRequest, current, cancellationToken: token);

            var getEnvironmentVariablesResponse = await _environmentServiceClient.GetEnvironmentVariables(new GetEnvironmentVariablesRequest() { ExperimentId = _simulations[simulationId].ExperimentId, DateTime = current.ToTimestamp() }, current, cancellationToken: token);
            var updateEnvironmentVariablesRequest = CreateUpdateEnvironmentVariablesRequest(getEnvironmentVariablesResponse, simulationId);

            await _ecopathServiceClient.UpdateEnvironmentVariablesAsync(updateEnvironmentVariablesRequest, current, cancellationToken: token);

            var simulationStepRequest = CreateSimulateStepRequest(simulationId, current);

            await _ecopathServiceClient.SimulateStepAsync(simulationStepRequest, current, token);

            var getBiomassResponseIntermediate = await _ecopathServiceClient.GetBiomassAsync(new GetBiomassRequest() { SimulationId = simulationId, DateTime = current.ToTimestamp() }, current, cancellationToken: token);

            var updateBiomassIntermediateRequest = CreateUpdateBiomassRequest(getBiomassResponseIntermediate);

            //            xx = GetProtoString<UpdateBiomassRequest>(updateBiomassIntermediateRequest);

            var poseidonUpdateBiomassResponse = await _poseidonServiceClient.UpdateBiomassAsync(updateBiomassIntermediateRequest, current, cancellationToken: token);

            var poseidonSimulateStepResponse = await _poseidonServiceClient.SimulateStepAsync(simulationStepRequest, current, token);

            var getCatchDispositionRequest = CreateGetCatchDispositionRequest(simulationId, current, endStepDateTime);

            var poseidonCatchDisposition = await _poseidonServiceClient.GetCatchDispositionAsync(getCatchDispositionRequest, current, cancellationToken: token);
            // TODO: check start_date_time and end_date_time in the response, to make sure they are correct and consistent with the request and the current simulation step

            var updateCatchDispositionIntermediateRequest = CreateUpdateCatchDispositionRequest(poseidonCatchDisposition);

            var updateCatchDispositionResponse = await _ecopathServiceClient.UpdateCatchDispositionAsync(updateCatchDispositionIntermediateRequest, current, cancellationToken: token);
            // TODO: check start_date_time and end_date_time in the response, to make sure they are correct and consistent with the request and the current simulation step

            var getBiomassResponseTotal = await _ecopathServiceClient.GetBiomassAsync(new GetBiomassRequest() { SimulationId = simulationId, DateTime = current.ToTimestamp() }, current, cancellationToken: token);
            BiomassUpdated?.Invoke(this, new BiomassEventArgs(getBiomassResponseTotal.BiomassSummary, experimentEventArgs));

            var ecopathCatchDispositionSummary = await _ecopathServiceClient.GetCatchDispositionAsync(getCatchDispositionRequest, current, cancellationToken: token);

            var updateCatchDispositionTotalRequest = CreateUpdateCatchDispositionRequest(ecopathCatchDispositionSummary);
 //           xx = GetProtoString<UpdateCatchDispositionRequest>(updateCatchDispositionTotalRequest);

            await _fisheriesAuthorityServiceClient.UpdateCatchDispositionAsync(updateCatchDispositionTotalRequest, current, cancellationToken: token);
            CatchDispositionUpdated?.Invoke(this, new CatchDispositionEventArgs(ecopathCatchDispositionSummary.CatchDispositionSummary, experimentEventArgs));

            var getSalesRequest = CreateGetSalesRequest(simulationId, current, endStepDateTime);

            var ecopathGetSalesResponse = await _ecopathServiceClient.GetSalesAsync(getSalesRequest, current, cancellationToken: token);
            SalesUpdated?.Invoke(this, new SalesEventArgs(ecopathGetSalesResponse.SalesSummary, experimentEventArgs));

            // TODO: check start_date_time and end_date_time in the response, to make sure they are correct and consistent with the request and the current simulation step

            var ecopathUpdateSalesRequest = CreateUpdateSalesRequest(ecopathGetSalesResponse, current, endStepDateTime);
 //           xx = GetProtoString<UpdateSalesRequest>(ecopathUpdateSalesRequest);

            var marketUpdateSalesResponse = await _marketServiceClient.UpdateSalesAsync(ecopathUpdateSalesRequest, current, token);

            var poseidonGetSalesResponse = await _poseidonServiceClient.GetSalesAsync(getSalesRequest, current, cancellationToken: token);
            SalesUpdated?.Invoke(this, new SalesEventArgs(poseidonGetSalesResponse.SalesSummary, experimentEventArgs));

            var poseidonUpdateSalesRequest = CreateUpdateSalesRequest(poseidonGetSalesResponse, current, endStepDateTime);
 //           xx = GetProtoString<UpdateSalesRequest>(poseidonUpdateSalesRequest);

            marketUpdateSalesResponse = await _marketServiceClient.UpdateSalesAsync(poseidonUpdateSalesRequest, current, token);

            var fishingActivityRequest = CreateGetFishingActivityRequest(simulationId, current, endStepDateTime);
            var fishingActivityEcopathResponse = await _ecopathServiceClient.GetFishingActivityAsync(fishingActivityRequest, current, cancellationToken: token);
            FishingActivityUpdated?.Invoke(this, new FishingActivityEventArgs(fishingActivityEcopathResponse.FishingActivitySummary, experimentEventArgs));

            var updateFishingActivityRequest = CreateUpdateFishingActivityRequest(fishingActivityEcopathResponse);
            var updateFishingActivityResponse = await _fisheriesAuthorityServiceClient.UpdateFishingActivityAsync(updateFishingActivityRequest, current, cancellationToken: token);

            var fishingActivityPoseidonResponse = await _poseidonServiceClient.GetFishingActivityAsync(fishingActivityRequest, current, cancellationToken: token);
            FishingActivityUpdated?.Invoke(this, new FishingActivityEventArgs(fishingActivityPoseidonResponse.FishingActivitySummary, experimentEventArgs));

            updateFishingActivityRequest = CreateUpdateFishingActivityRequest(fishingActivityPoseidonResponse);
            await _fisheriesAuthorityServiceClient.UpdateFishingActivityAsync(updateFishingActivityRequest, current, cancellationToken: token);

            await _marketServiceClient.SimulateStepAsync(simulationStepRequest, current, token);
            SimulateStep?.Invoke(this, experimentEventArgs);
        }

        private UpdateFishingActivityRequest CreateUpdateFishingActivityRequest(GetFishingActivityResponse fishingActivityEcopathResponse)
        {
            return new UpdateFishingActivityRequest()
            {
                SimulationId = fishingActivityEcopathResponse.SimulationId,
                FishingActivitySummary = fishingActivityEcopathResponse.FishingActivitySummary,
                StartDateTime = fishingActivityEcopathResponse.StartDateTime,
                EndDateTime = fishingActivityEcopathResponse.EndDateTime
            };
        }

        private GetFishingActivityRequest CreateGetFishingActivityRequest(string simulationId, DateTime current, DateTime endStepDateTime)
        {
            return new GetFishingActivityRequest()
            {
                SimulationId = simulationId,
                StartDateTime = current.ToTimestamp(),
                EndDateTime = endStepDateTime.ToTimestamp()
            };
        }

        private UpdateRegulationsRequest CreateUpdateRegulationsRequest(GetRegulationsResponse regulationsResponse)
        {
            return new UpdateRegulationsRequest()
            {
                SimulationId = regulationsResponse.SimulationId,
                StartDateTime = regulationsResponse.StartDateTime,
                EndDateTime = regulationsResponse.EndDateTime,
                RegulationsSummary = regulationsResponse.RegulationsSummary
            };
        }

        private UpdateEnvironmentVariablesRequest CreateUpdateEnvironmentVariablesRequest(GetEnvironmentVariablesResponse getEnvironmentVariablesResponse, string simulationId)
        {
            return new UpdateEnvironmentVariablesRequest()
            {
                SimulationId = simulationId,
                EnvironmentVariablesSummary = getEnvironmentVariablesResponse.EnvironmentVariablesSummary,
                DateTime = getEnvironmentVariablesResponse.DateTime
            };
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

            resultaat = resultaat.Replace("unit_", "unit");
            return resultaat;
        }

        public static DateTime AddStepSize(DateTime current, string input)
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
                // To guarantee sequences like 01-01 → 01-02 → 01-03 → 01-04  and 10-01 → 10-02 → 10-03 → 10-04 etc,
                // calculate the resulting month and year, but use the rest.
                int year = current.Year;
                int month = current.Month + months;
                if (month > 12)
                {
                    year += 1;
                    month = month % 12;
                }
                var next = current.AddMonths(months);

                return new DateTime(
                    year, month, next.Day,
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
            var result = current + ts;
            return TimeZoneInfo.ConvertTimeToUtc(result);
        }

        public static UpdateSalesRequest CreateUpdateSalesRequest(GetSalesResponse response, DateTime startDateTime, DateTime endDateTime)
        {
            return new UpdateSalesRequest
            {
                SimulationId = response.SimulationId,
                StartDateTime = Timestamp.FromDateTime(startDateTime),
                EndDateTime = Timestamp.FromDateTime(endDateTime),
                SalesSummary = response.SalesSummary
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
            // TODO!!!!! REMOVE THIS FILTERING WHEN CMSY CAN HANDLE DISPOSITION GRIDS WITH NULL FLEETSEGMENT OR GEARCODE. This is just a temporary workaround to avoid errors in Aggregator when it encounters such disposition grids, which can be present in EwE responses.
            var filteredCatchDispositionSummary = new CatchDispositionSummary()
            {
                DispositionGrids =
                {
                    catchDisposition.CatchDispositionSummary.DispositionGrids
                        .Where(dg => dg.FleetSegment != null && !string.IsNullOrWhiteSpace(dg.FleetSegment.GearCode))
                        .ToList()
                }
            };

            var result = new UpdateCatchDispositionRequest()
            {
                SimulationId = catchDisposition.SimulationId,
                CatchDispositionSummary = filteredCatchDispositionSummary,
                StartDateTime = catchDisposition.StartDateTime,
                EndDateTime = catchDisposition.EndDateTime
            };

            return result;
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
                BiomassSummary = getBiomassResponse.BiomassSummary,
                DateTime = getBiomassResponse.DateTime
            };

            return updateRequest;
        }

        private static SimulateStepRequest CreateSimulateStepRequest(string simulationId, DateTime currentDateTime)
        {
            return new SimulateStepRequest
            {
                SimulationId = simulationId,
                CurrentDateTime = Timestamp.FromDateTime(currentDateTime)
            };
        }

        private static CancelSimulationRequest CreateCancelRequest(string simulationId)
        {
            return new CancelSimulationRequest
            {
                SimulationId = simulationId
            };
        }

        private static FinaliseSimulationRequest CreateFinaliseSimulationRequest(string simulationId)
        {
            return new FinaliseSimulationRequest
            {
                SimulationId = simulationId
            };
        }

        private static UpdateSpeciesPricesRequest CreateUpdateSpeciesPricesRequest(GetSpeciesPricesResponse speciesPriceResponse)
        {
            return new UpdateSpeciesPricesRequest()
            {
                SimulationId = speciesPriceResponse.SimulationId,
                SpeciesPriceSummary = speciesPriceResponse.SpeciesPriceSummary, 
                DateTime = speciesPriceResponse.DateTime
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
                        ExperimentId = sim.Value.ExperimentId,
                        ScenarioName = sim.Value.ScenarioName,
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
