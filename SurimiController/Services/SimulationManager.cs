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
        private readonly ILogger<SimulationManager> _logger;

        private readonly IEcopathServiceClient _ecopathServiceClient;
        private readonly IPoseidonServiceClient _poseidonServiceClient;
        private readonly ICmsyServiceClient _cmsyServiceClient;
        private readonly IAggregatorServiceClient _aggregatorServiceClient;
        private readonly IValueChainServiceClient _valueChainServiceClient;
        private readonly IMarketServiceClient _marketServiceClient;

        public SimulationManager(GrpcClientFactory clientFactory, ILogger<SimulationManager> logger, ICmsyServiceClient cmsyServiceClient, IAggregatorServiceClient aggregatorServiceClient, IValueChainServiceClient valueChainServiceClient, IMarketServiceClient marketServiceClient, IPoseidonServiceClient poseidonServiceClient, IEcopathServiceClient ecopathServiceClient)
        {
            _logger = logger;
            _cmsyServiceClient = cmsyServiceClient;
            _aggregatorServiceClient = aggregatorServiceClient;
            _valueChainServiceClient = valueChainServiceClient;
            _marketServiceClient = marketServiceClient;
            _poseidonServiceClient = poseidonServiceClient;
            _ecopathServiceClient = ecopathServiceClient;
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
        public Task InitSimulationAsync(string simulationId, string experimentId, string scenarioId, DateTime? endDateTime, Grpc.Surimi.Simulation simulation)
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

            //var xx = GetProtoString<InitialiseRequest>(initRequest);

            var initializationTasks = new List<Task<InitialiseResponse>>();

            var ecopathInitialiseResponse = _ecopathServiceClient.AddInitialise(initializationTasks, initRequest);
            _poseidonServiceClient.AddInitialise(initializationTasks, initRequest);
            _marketServiceClient.AddInitialise(initializationTasks, initRequest);
            _cmsyServiceClient.AddInitialise(initializationTasks, initRequest);
            _valueChainServiceClient.AddInitialise(initializationTasks, initRequest);
            _aggregatorServiceClient.AddInitialise(initializationTasks, initRequest);

            _simulations[simulationId] = new Models.Simulation
            {
                ScenarioId = scenarioId,
                ExperimentId = experimentId,
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
                await Task.WhenAll(initializationTasks);

                string hostValue = string.Empty;
                if (ecopathInitialiseResponse != null)
                {
                    // Await the response headers
                    var headers = await ecopathInitialiseResponse.ResponseHeadersAsync;

                    // Find the header by key (case-insensitive)
                    hostValue = headers.GetValue("host") ?? string.Empty; // returns string.Empty if not found
                }

                _simulations[simulationId].EcologyHost = hostValue ?? string.Empty;
                _simulations[simulationId].Status = "Initialised";

                _logger.LogInformation("Simulation {SimulationId} is created and initialised on {EcologyHost}", simulationId, hostValue);
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
                    }

                    // Finalise the simulation
                    var finaliseRequest = CreateFinaliseRequest(simulationId);

                    await _ecopathServiceClient.FinaliseAsync(finaliseRequest, cts.Token);
                    await _poseidonServiceClient.FinaliseAsync(finaliseRequest, cts.Token);
                    await _marketServiceClient.FinaliseAsync(finaliseRequest, cts.Token);
                    await _cmsyServiceClient.FinaliseAsync(finaliseRequest, cts.Token);
                    await _valueChainServiceClient.FinaliseAsync(finaliseRequest, cts.Token);
                    await _aggregatorServiceClient.FinaliseAsync(finaliseRequest, cts.Token);

                    _simulations[simulationId].Status = "Finished";
                    _simulations[simulationId].SimulationDuration = DateTime.UtcNow - _simulations[simulationId].SimulationStarted;
                    _logger.LogInformation("{SimulationId} is finished", simulationId);
                }
                catch (RpcException ex) when (ex.InnerException is OperationCanceledException)
                {
                    // cancel the simulation
                    var cancelRequest = CreateCancelRequest(simulationId);

                    await _poseidonServiceClient.CancelAsync(cancelRequest, cts.Token);
                    await _ecopathServiceClient.CancelAsync(cancelRequest, cts.Token);
                    await _marketServiceClient.CancelAsync(cancelRequest, cts.Token); 
                    await _cmsyServiceClient.CancelAsync(cancelRequest, cts.Token); 
                    await _valueChainServiceClient.CancelAsync(cancelRequest, cts.Token);
                    await _aggregatorServiceClient.CancelAsync(cancelRequest, cts.Token);

                    _simulations[simulationId].Status = "Canceled";
                    _logger.LogInformation("{SimulationId} is canceled", simulationId);
                }
                catch (RpcException ex)
                {
                    _simulations[simulationId].Status = "Error";
                    _logger.LogError(ex, "{SimulationId} encountered a RpcException", simulationId);
                    throw;
                }
                catch (Exception ex)
                {
                    _simulations[simulationId].Status = "Error";
                    _logger.LogError(ex, "{SimulationId} encountered an Exception", simulationId);
                    throw;
                }
            }, cts.Token);

            _simulations[simulationId].Task = task;
            _simulations[simulationId].Cts = cts;
            return task;
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
            _simulations[simulationId].SimulationCurrent = current;

            var speciesPriceResponse = await _marketServiceClient.GetSpeciesPricesAsync(new GetSpeciesPricesRequest() { SimulationId = simulationId }, current, cancellationToken: token);

            var updatePriceRequest = CreateUpdateSpeciesPricesRequest(speciesPriceResponse);
//            var xx = GetProtoString<UpdateSpeciesPricesRequest>(updatePriceRequest);

            await _poseidonServiceClient.UpdateSpeciesPricesAsync(updatePriceRequest, current, cancellationToken: token);
            await _ecopathServiceClient.UpdateSpeciesPricesAsync(updatePriceRequest, current, cancellationToken: token);

            var simulationStepRequest = CreateSimulateStepRequest(simulationId, current);

            await _ecopathServiceClient.SimulateStepAsync(simulationStepRequest, current, cancellationToken: token);

            var getBiomassResponseIntermediate = await _ecopathServiceClient.GetBiomassAsync(new GetBiomassRequest() { SimulationId = simulationId, DateTime = current.ToTimestamp() }, current, cancellationToken: token);

            var updateBiomassIntermediateRequest = CreateUpdateBiomassRequest(getBiomassResponseIntermediate);

//            xx = GetProtoString<UpdateBiomassRequest>(updateBiomassIntermediateRequest);

            var poseidonUpdateBiomassResponse = await _poseidonServiceClient.UpdateBiomassAsync(updateBiomassIntermediateRequest, current, cancellationToken: token);

            var poseidonSimulateStepResponse = await _poseidonServiceClient.SimulateStepAsync(simulationStepRequest, current, cancellationToken: token);

            var getCatchDispositionRequest = CreateGetCatchDispositionRequest(simulationId, current, endStepDateTime);

            var poseidonCatchDisposition = await _poseidonServiceClient.GetCatchDispositionAsync(getCatchDispositionRequest, current, cancellationToken: token);
            // TODO: check start_date_time and end_date_time in the response, to make sure they are correct and consistent with the request and the current simulation step

            var updateCatchDispositionIntermediateRequest = CreateUpdateCatchDispositionRequest(poseidonCatchDisposition);

            var updateCatchDispositionResponse = await _ecopathServiceClient.UpdateCatchDispositionAsync(updateCatchDispositionIntermediateRequest, current, cancellationToken: token);
            // TODO: check start_date_time and end_date_time in the response, to make sure they are correct and consistent with the request and the current simulation step

            var getBiomassResponseTotal = await _ecopathServiceClient.GetBiomassAsync(new GetBiomassRequest() { SimulationId = simulationId }, current, cancellationToken: token);

            var updateBiomassRequest = CreateUpdateBiomassRequest(getBiomassResponseTotal);
            await _cmsyServiceClient.UpdateBiomassAsync(updateBiomassRequest, current, cancellationToken: token);
            await _aggregatorServiceClient.UpdateBiomassAsync(updateBiomassRequest, current, cancellationToken: token);

            var ecopathCatchDispositionSummary = await _ecopathServiceClient.GetCatchDispositionAsync(getCatchDispositionRequest, current, cancellationToken: token);

            var updateCatchDispositionRequest = CreateUpdateCatchDispositionRequest(ecopathCatchDispositionSummary);
            var xx = GetProtoString<UpdateCatchDispositionRequest>(updateCatchDispositionRequest);

            await _cmsyServiceClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: token);
            await _aggregatorServiceClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: token);

            var getSalesRequest = CreateGetSalesRequest(simulationId, current, endStepDateTime);

            var ecopathGetSalesResponse = await _ecopathServiceClient.GetSalesAsync(getSalesRequest, current, cancellationToken: token);

            // TODO: check start_date_time and end_date_time in the response, to make sure they are correct and consistent with the request and the current simulation step
            // Update Sales to Market

            var ecopathUpdateSalesRequest = CreateUpdateSalesRequest(ecopathGetSalesResponse, current, endStepDateTime);
            var marketUpdateSalesResponse = await _marketServiceClient.UpdateSalesAsync(ecopathUpdateSalesRequest, current, token);
            await _aggregatorServiceClient.UpdateSalesAsync(ecopathUpdateSalesRequest, current, cancellationToken: token);
            await _valueChainServiceClient.UpdateSalesAsync(ecopathUpdateSalesRequest, current, cancellationToken: token);

            var poseidonGetSalesResponse = await _poseidonServiceClient.GetSalesAsync(getSalesRequest, current, cancellationToken: token);

            var poseidonUpdateSalesRequest = CreateUpdateSalesRequest(poseidonGetSalesResponse, current, endStepDateTime);
//            string xx = GetProtoString<UpdateSalesRequest>(poseidonUpdateSalesRequest);
            marketUpdateSalesResponse = await _marketServiceClient.UpdateSalesAsync(poseidonUpdateSalesRequest, current, token);
            await _aggregatorServiceClient.UpdateSalesAsync(poseidonUpdateSalesRequest, current, cancellationToken: token);
            await _valueChainServiceClient.UpdateSalesAsync(poseidonUpdateSalesRequest, current, cancellationToken: token);

            await _cmsyServiceClient.SimulateStepAsync(simulationStepRequest, current, cancellationToken: token);

            await _marketServiceClient.SimulateStepAsync(simulationStepRequest, current, cancellationToken: token);

            await _valueChainServiceClient.SimulateStepAsync(simulationStepRequest, current, cancellationToken: token);
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

            // TODO replace "unit_" with "unit"
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
                CatchDispositionSummary = catchDisposition.CatchDispositionSummary,
                StartDateTime = catchDisposition.StartDateTime,
                EndDateTime = catchDisposition.EndDateTime
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
                        ExperimentId = sim.Value.ExperimentId,
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
