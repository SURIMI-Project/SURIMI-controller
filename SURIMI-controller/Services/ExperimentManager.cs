using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI_controller.Models;
using System.Collections.Concurrent;

namespace SURIMI_controller.Services
{
    /// <summary>
    /// Central orchestrator for SURIMI experiments.
    /// Wires <see cref="ISimulationManager"/> events, fans out downstream gRPC calls,
    /// and aggregates per-simulation summaries once all runs have reported in for a given date.
    /// </summary>
    public class ExperimentManager : IExperimentManager
    {
        /// <summary>Active experiments keyed by <c>ExperimentId</c>.</summary>
        private readonly ConcurrentDictionary<string, Experiment> _experiments = new();

        private readonly ISimulationManager _simulationManager;
        private readonly ILogger<ExperimentManager> _logger;
        private readonly IAggregatorService _aggregatorService;
        private readonly ICmsyServiceClient _cmsyServiceClient;
        private readonly IOutputCreatorServiceClient _outputCreatorClient;
        private readonly IEnvironmentServiceClient _environmentServiceClient;
        private readonly IValueChainServiceClient _valueChainServiceClient;
        private readonly VersionCheckerService _versionCheckerService;

        /// <summary>
        /// Initializes a new <see cref="ExperimentManager"/> and subscribes to all
        /// <see cref="ISimulationManager"/> events.
        /// </summary>
        public ExperimentManager(GrpcClientFactory clientFactory, ISimulationManager simulationManager, ICmsyServiceClient cmsyServiceClient, ILogger<ExperimentManager> logger, IAggregatorService aggregatorService, IOutputCreatorServiceClient outputCreatorClient, IEnvironmentServiceClient environmentServiceClient, IValueChainServiceClient valueChainServiceClient, VersionCheckerService versionCheckerService)
        {
            _simulationManager = simulationManager;
            _cmsyServiceClient = cmsyServiceClient;
            _aggregatorService = aggregatorService;
            _logger = logger;
            _outputCreatorClient = outputCreatorClient;
            _environmentServiceClient = environmentServiceClient;
            _valueChainServiceClient = valueChainServiceClient;
            _versionCheckerService = versionCheckerService;

            // Subscribe to all simulation lifecycle and data events
            _simulationManager.SimulateStep += OnSimulateStep;
            _simulationManager.SimulationFinalised += OnSimulationFinalised;
            _simulationManager.SimulationCancelled += OnSimulationCancelled;
            _simulationManager.BiomassUpdated += OnBiomassUpdated;
            _simulationManager.CatchDispositionUpdated += OnCatchDispositionUpdated;
            _simulationManager.SalesUpdated += OnSalesUpdated;
            _simulationManager.FishingActivityUpdated += OnFishingActivityUpdated;
            _simulationManager.SpeciesPriceUpdated += OnSpeciesPriceUpdated;
        }

        /// <summary>
        /// Registers a new experiment, initialises downstream services, and launches
        /// all simulation runs in a background task.
        /// Returns immediately — the experiment continues running in the background.
        /// </summary>
        /// <param name="request">Experiment configuration, including number of runs and scenario.</param>
        /// <param name="simulation">Shared simulation parameters forwarded to every run.</param>
        /// <param name="cancellationToken">Token used to cancel the background work.</param>
        public Task SubmitExperiment(SubmitExperimentRequest request, Grpc.Surimi.Simulation simulation, CancellationToken cancellationToken)
        {
            if (_experiments.ContainsKey(request.ExperimentId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Experiment with Id {request.ExperimentId} is already submitteded"));
            }

            // Generate a unique simulation ID for each requested run
            var simulationIds = Enumerable.Range(0, request.NumberOfRuns)
                .Select(_ => Guid.NewGuid().ToString())
                .ToList();

            _experiments[request.ExperimentId] = new Experiment { SimulationIds = simulationIds };

            // All summary dictionaries (BiomassSummary, CatchDispositionSummary, FishingActivitySummary, SalesSummary, SpeciesPriceSummary) are lazily initialized per date in OnSummaryUpdated

            // Run the rest of the logic in a background task after all initialisation calls complete
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var task = Task.Run(async () =>
            {
                _logger.LogInformation("Checking versions of connected services (retrying until all are available)...");
                await _versionCheckerService.WriteVersionsAsync(cts.Token);

                var initRequest = new InitialiseExperimentRequest
                {
                    ExperimentId = request.ExperimentId,
                    ScenarioName = request.ScenarioName,
                    EndDateTime = request.EndDateTime,
                    Simulation = simulation,
                    SimulationIds = { _experiments[request.ExperimentId].SimulationIds },
                };

                // Kick off initialisation on all downstream services in parallel
                var initializationTasks = new List<Task<InitialiseExperimentResponse>>();
                var cmsyTask = _cmsyServiceClient.AddInitialise(initializationTasks, initRequest, cancellationToken);
                var outputCreatorTask = _outputCreatorClient.AddInitialise(initializationTasks, initRequest, cancellationToken);
                var environmentTask = _environmentServiceClient.AddInitialise(initializationTasks, initRequest, cancellationToken);
                var valueChainTask = _valueChainServiceClient.AddInitialise(initializationTasks, initRequest, cancellationToken);

                // Start all simulation runs concurrently
                var initTasks = _experiments[request.ExperimentId].SimulationIds.Select(async simulationId =>
                {
                    try
                    {
                        await _simulationManager.RunSimulationAsync(
                        simulationId,
                        request.ExperimentId,
                        request.ScenarioName,
                        request.EndDateTime?.ToDateTime(),
                        simulation,
                        request.RegulationsDefinitionsSummary,
                        cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception in Running Simulation. ID={SimulationId}", simulationId);
                    }
                }).ToList();

                // Combine both task collections and await all together
                var allTasks = initTasks.Concat<Task>(initializationTasks);
                await Task.WhenAll(allTasks);
                _logger.LogInformation("All simulations and initialisation calls completed for ExperimentId={ExperimentId}", request.ExperimentId);
            }, cts.Token);

            _experiments[request.ExperimentId].Task = task;
            _experiments[request.ExperimentId].Cts = cts;

            // Return promptly, do not await the task to prevent the gRPC call from timing out. The simulation will continue to run in the background, and its progress can be tracked through the SimulationManager's state.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cancels all simulation runs belonging to the specified experiment in parallel.
        /// </summary>
        public async Task CancelExperimentAsync(string experimentId, CancellationToken cancellationToken)
        {
            if (!_experiments.ContainsKey(experimentId))
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Experiment with Id {experimentId} cannot be found and cannot cancel"));
            }

            var cancelTasks = _experiments[experimentId].SimulationIds.Select(async simulationId =>
            {
                try
                {
                    await _simulationManager.CancelSimulationAsync(simulationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in canceling Simulation. ID={SimulationId}", simulationId);
                }
            }).ToList();

            // This will cause multiple simulations to cancel in parallel
            await Task.WhenAll(cancelTasks);
        }

        /// <inheritdoc/>
        public async Task<GetAllSimulationStatusesResponse> GetAllSimulationStatussesAsync(CancellationToken cancellationToken)
        {
            return await _simulationManager.GetAllSimulationStatussesAsync(cancellationToken);
        }

        // -------------------------------------------------------------------------
        // Flag event handlers — each delegates to OnFlagUpdated; fire-and-forget
        // downstream calls are intentionally not awaited.
        // -------------------------------------------------------------------------

        /// <summary>
        /// Raised when a simulation reports that the current time step is ready to advance.
        /// Fans out <see cref="IExperimentStepRequest"/> to CMSY, Environment, and OutputCreator
        /// once all runs have checked in for the date.
        /// </summary>
        private void OnSimulateStep(object? sender, ExperimentEventArgs e) =>
            OnFlagUpdated(
                e,
                eventName: nameof(OnSimulateStep),
                getDictionary: experiment => experiment.SimulateStepCalled,
                onAllReceived: async (experimentId, current, token) =>
                {
                    var request = new ExperimentStepRequest() { ExperimentId = experimentId, CurrentDateTime = current.ToTimestamp() };

                    await Task.WhenAll(
                        _cmsyServiceClient.ExperimentStepAsync(request, current: current, token: token),
                        _environmentServiceClient.ExperimentStepAsync(request, current: current, token: token),
                        _outputCreatorClient.ExperimentStepAsync(request, current: current, token: token));
                });

        /// <summary>
        /// Raised when all simulations have completed successfully.
        /// Fans out <see cref="FinaliseExperimentRequest"/> to CMSY, Environment, and OutputCreator.
        /// </summary>
        private void OnSimulationFinalised(object? sender, ExperimentEventArgs e) =>
            OnFlagUpdated(
                e,
                eventName: nameof(OnSimulationFinalised),
                getDictionary: experiment => experiment.SimulationFinalisedCalled,
                onAllReceived: async (experimentId, current, token) =>
                {
                    // Get stock assessment from CMSY
                    var stockAssessmentResponse = await _cmsyServiceClient.GetStockAssessmentAsync(new GetStockAssessmentRequest() { ExperimentId = experimentId }, token);

                    // send it to the output-creator
//                    _outputCreatorClient.UpdateStockAssessmentAsync(new UpdateStockAssessmentRequest() { StockAssessmentSummary = stockAssessmentResponse.StockAssessmentSummary, ExperimentId = experimentId }, experimentId, token);
                    var request = new FinaliseExperimentRequest() { ExperimentId = experimentId };
                    await Task.WhenAll(
                        _cmsyServiceClient.FinaliseExperimentAsync(request, token),
                        _environmentServiceClient.FinaliseExperimentAsync(request, token),
                        _outputCreatorClient.FinaliseExperimentAsync(request, token),
                        _valueChainServiceClient.FinaliseExperimentAsync(request, token));
                });

        /// <summary>
        /// Raised when all simulations have been cancelled.
        /// Fans out <see cref="CancelExperimentRequest"/> to CMSY, Environment, and OutputCreator.
        /// </summary>
        private void OnSimulationCancelled(object? sender, ExperimentEventArgs e) =>
            OnFlagUpdated(
                e,
                eventName: nameof(OnSimulationCancelled),
                getDictionary: experiment => experiment.SimulationCancelledCalled,
                onAllReceived: async (experimentId, current, token) =>
                {
                    var request = new CancelExperimentRequest() { ExperimentId = experimentId };
                    await Task.WhenAll(
                        _cmsyServiceClient.CancelExperimentAsync(request, token),
                        _environmentServiceClient.CancelExperimentAsync(request, token),
                        _outputCreatorClient.CancelExperimentAsync(request, token),
                        _valueChainServiceClient.CancelExperimentAsync(request, token));
                });

        // -------------------------------------------------------------------------
        // Summary event handlers — each delegates to OnSummaryUpdated; downstream
        // calls are fire-and-forget and intentionally not awaited.
        // -------------------------------------------------------------------------

        /// <summary>
        /// Aggregates <see cref="BiomassSummary"/> data across all runs for a date and
        /// forwards the resulting statistics to CMSY and OutputCreator.
        /// </summary>
        private void OnBiomassUpdated(object? sender, BiomassEventArgs e) =>
            OnSummaryUpdated(
                e,
                getSummary: e => e.BiomassSummary,
                getDictionary: experiment => experiment.BiomassSummary,
                onAllReceived: async (dateSummaries, experimentId, current, token) =>
                {
                    var statisticsSummary = _aggregatorService.AddAggregateBiomass(dateSummaries.Values.ToList());
                    var updateBomassStatisticsRequest = new UpdateBiomassStatisticsRequest()
                    {
                        ExperimentId = experimentId,
                        BiomassStatisticsSummary = statisticsSummary,
                        DateTime = current.ToTimestamp()
                    };
                    await Task.WhenAll(
                    _cmsyServiceClient.UpdateBiomassStatisticsAsync(updateBomassStatisticsRequest, experimentId, current, token),
                    _outputCreatorClient.UpdateBiomassStatisticsAsync(updateBomassStatisticsRequest, experimentId, current, token));
                });

        /// <summary>
        /// Aggregates <see cref="CatchDispositionSummary"/> data across all runs for a date and
        /// forwards the resulting statistics to CMSY and OutputCreator.
        /// </summary>
        private void OnCatchDispositionUpdated(object? sender, CatchDispositionEventArgs e) =>
            OnSummaryUpdated(
                e,
                getSummary: e => e.CatchDispositionSummary,
                getDictionary: experiment => experiment.CatchDispositionSummary,
                onAllReceived: async (dateSummaries, experimentId, current, token) =>
                {
                    var statisticsSummary = _aggregatorService.AddAggregateCatchDisposition(dateSummaries.Values.ToList());
                    var updateCatchDispositionStatisticsRequest = new UpdateCatchDispositionStatisticsRequest()
                    {
                        ExperimentId = experimentId,
                        CatchDispositionStatisticsSummary = statisticsSummary,
                        StartDateTime = current.ToTimestamp()
                    };
                    await Task.WhenAll(
                        _cmsyServiceClient.UpdateCatchDispositionStatisticsAsync(updateCatchDispositionStatisticsRequest, experimentId, current, token),
                        _outputCreatorClient.UpdateCatchDispositionStatisticsAsync(updateCatchDispositionStatisticsRequest, experimentId, current, token));
                });

        /// <summary>
        /// Aggregates <see cref="SalesSummary"/> data across all runs for a date and
        /// forwards the resulting statistics to OutputCreator.
        /// </summary>
        private void OnSalesUpdated(object? sender, SalesEventArgs e) =>
            OnSummaryUpdated(
                e,
                getSummary: e => e.SalesSummary,
                getDictionary: experiment => experiment.SalesSummary,
                onAllReceived: async (dateSummaries, experimentId, current, token) =>
                {
                    var statisticsSummary = _aggregatorService.AddAggregateSales(dateSummaries.Values.ToList());
                    await Task.WhenAll(
                        _outputCreatorClient.UpdateSalesStatisticsAsync(new UpdateSalesStatisticsRequest() { ExperimentId = experimentId, SalesStatisticsSummary = statisticsSummary, StartDateTime = current.ToTimestamp() }, experimentId, current, token),
                        _valueChainServiceClient.UpdateSalesStatisticsAsync(new UpdateSalesStatisticsRequest() { ExperimentId = experimentId, SalesStatisticsSummary = statisticsSummary, StartDateTime = current.ToTimestamp() }, current, token));
                });

        /// <summary>
        /// Aggregates <see cref="FishingActivitySummary"/> data across all runs for a date and
        /// forwards the resulting statistics to OutputCreator.
        /// </summary>
        private void OnFishingActivityUpdated(object? sender, FishingActivityEventArgs e) =>
            OnSummaryUpdated(
                e,
                getSummary: e => e.FishingActivitySummary,
                getDictionary: experiment => experiment.FishingActivitySummary,
                onAllReceived: async (dateSummaries, experimentId, current, token) =>
                {
                    var statisticsSummary = _aggregatorService.AddAggregateFishingActivity(dateSummaries.Values.ToList());
                    await _outputCreatorClient.UpdateFishingActivityStatisticsAsync(new UpdateFishingActivityStatisticsRequest() { ExperimentId = experimentId, FishingActivityStatisticsSummary = statisticsSummary, StartDateTime = current.ToTimestamp() }, experimentId, current, token);
                });

        /// <summary>
        /// Aggregates <see cref="SpeciesPriceSummary"/> data across all runs for a date and
        /// forwards the resulting statistics to OutputCreator.
        /// </summary>
        private void OnSpeciesPriceUpdated(object? sender, SpeciesPriceEventArgs e) =>
            OnSummaryUpdated(
                e,
                getSummary: e => e.SpeciesPriceSummary,
                getDictionary: experiment => experiment.SpeciesPriceSummary,
                onAllReceived: async (dateSummaries, experimentId, current, token) =>
                {
                    var statisticsSummary = _aggregatorService.AddAggregateSpeciesPrice(dateSummaries.Values.ToList());
                    await _outputCreatorClient.UpdateSpeciesPriceStatisticsAsync(new UpdateSpeciesPriceStatisticsRequest() { ExperimentId = experimentId, SpeciesPriceStatisticsSummary = statisticsSummary, DateTime = current.ToTimestamp() }, experimentId, current, token);
                });

        // -------------------------------------------------------------------------
        // Aggregation helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Generic handler for Summary simulation events.
        /// <para>
        /// Lazily initialises a <c>(date → simulationId → TSummary?)</c> bucket, records the
        /// incoming summary, and calls <paramref name="onAllReceived"/> once every simulation
        /// in the experiment has reported in for the current date. The date bucket is then
        /// removed to free memory.
        /// </para>
        /// </summary>
        /// <typeparam name="TEventArgs">Event args type; must derive from <see cref="ExperimentEventArgs"/>.</typeparam>
        /// <typeparam name="TSummary">Summary type.</typeparam>
        /// <param name="e">The incoming event args.</param>
        /// <param name="getSummary">Extracts the summary from the event args.</param>
        /// <param name="getDictionary">Returns the per-date tracking dictionary from an <see cref="Experiment"/>.</param>
        /// <param name="onAllReceived">
        /// Callback invoked when all simulations have reported; receives the fully-populated
        /// date bucket, the experiment ID, the current date, and a cancellation token.
        /// </param>
        private void OnSummaryUpdated<TEventArgs, TSummary>(
            TEventArgs e,
            Func<TEventArgs, TSummary> getSummary,
            Func<Experiment, Dictionary<DateTime, Dictionary<string, TSummary?>>> getDictionary,
            Action<Dictionary<string, TSummary?>, string, DateTime, CancellationToken> onAllReceived)
            where TEventArgs : ExperimentEventArgs
            where TSummary : class
        {
            if (!_experiments.TryGetValue(e.ExperimentId, out Experiment? experiment))
            {
                _logger.LogWarning("Received {SummaryName} update for unknown experiment. ExperimentId={ExperimentId}, SimulationId={SimulationId}", typeof(TSummary).Name, e.ExperimentId, e.SimulationId);
                return;
            }

            if (!experiment.SimulationIds.Contains(e.SimulationId))
            {
                _logger.LogWarning("Received {SummaryName} update for unknown simulation. ExperimentId={ExperimentId}, SimulationId={SimulationId}", typeof(TSummary).Name, e.ExperimentId, e.SimulationId);
                return;
            }

            var dateKey = e.Current.Date;
            var dictionary = getDictionary(experiment);

            // Lazily initialize the date entry with null slots for all simulations
            if (!dictionary.TryGetValue(dateKey, out var summaryForSpecificDate))
            {
                summaryForSpecificDate = experiment.SimulationIds.ToDictionary(id => id, _ => (TSummary?)null);
                dictionary[dateKey] = summaryForSpecificDate;
            }

            // Guard against duplicate events from the same simulation on the same date
            if (summaryForSpecificDate[e.SimulationId] != null)
            {
                _logger.LogWarning("Received duplicate {SummaryName} update. ExperimentId={ExperimentId}, SimulationId={SimulationId}, Date={Date}", typeof(TSummary).Name, e.ExperimentId, e.SimulationId, dateKey);
                return;
            }

            summaryForSpecificDate[e.SimulationId] = getSummary(e);

            // Check if all simulations have reported in for this date; if so, aggregate, store, and free memory
            if (summaryForSpecificDate.All(kv => kv.Value != null))
            {
                _logger.LogInformation("All simulations received {SummaryName} for date {Date}. ExperimentId={ExperimentId}", typeof(TSummary).Name, dateKey.ToString("yyyy-MM-dd"), e.ExperimentId);
                onAllReceived(summaryForSpecificDate, e.ExperimentId, e.Current, e.Token);
                dictionary.Remove(dateKey);
            }
        }

        /// <summary>
        /// Generic handler for flag-only simulation events. (cancel, finalise, simulate step)
        /// <para>
        /// Lazily initialises a <c>(date → simulationId → bool?)</c> bucket, marks the
        /// simulation as having fired the event, and calls <paramref name="onAllReceived"/>
        /// once every simulation has checked in for the current date. The date bucket is then
        /// removed to free memory.
        /// </para>
        /// </summary>
        /// <param name="e">The incoming event args.</param>
        /// <param name="eventName">Name of the event, used for structured logging.</param>
        /// <param name="getDictionary">Returns the per-date flag dictionary from an <see cref="Experiment"/>.</param>
        /// <param name="onAllReceived">
        /// Callback invoked when all simulations have reported; receives the experiment ID,
        /// the current date, and a cancellation token.
        /// </param>
        private void OnFlagUpdated(
            ExperimentEventArgs e,
            string eventName,
            Func<Experiment, Dictionary<DateTime, Dictionary<string, bool?>>> getDictionary,
            Func<string, DateTime, CancellationToken, Task> onAllReceived)
        {
            if (!_experiments.TryGetValue(e.ExperimentId, out Experiment? experiment))
            {
                _logger.LogWarning("Received {EventName} for unknown experiment. ExperimentId={ExperimentId}, SimulationId={SimulationId}", eventName, e.ExperimentId, e.SimulationId);
                return;
            }

            if (!experiment.SimulationIds.Contains(e.SimulationId))
            {
                _logger.LogWarning("Received {EventName} for unknown simulation. ExperimentId={ExperimentId}, SimulationId={SimulationId}", eventName, e.ExperimentId, e.SimulationId);
                return;
            }

            var dateKey = e.Current.Date;
            var dictionary = getDictionary(experiment);

            // Lazily initialize the date entry with null slots for all simulations
            if (!dictionary.TryGetValue(dateKey, out var flagsForDate))
            {
                flagsForDate = experiment.SimulationIds.ToDictionary(id => id, _ => (bool?)null);
                dictionary[dateKey] = flagsForDate;
            }

            // Guard against duplicate events from the same simulation on the same date
            if (flagsForDate[e.SimulationId] != null)
            {
                _logger.LogWarning("Received duplicate {EventName}. ExperimentId={ExperimentId}, SimulationId={SimulationId}, Date={Date}", eventName, e.ExperimentId, e.SimulationId, dateKey.ToString("yyyy-MM-dd"));
                return;
            }

            flagsForDate[e.SimulationId] = true;

            // Check if all simulations have reported in for this date; if so, notify downstream and free memory
            if (flagsForDate.All(kv => kv.Value != null))
            {
                _logger.LogInformation("All simulations received {EventName} for date {Date}. ExperimentId={ExperimentId}", eventName, dateKey.ToString("yyyy-MM-dd"), e.ExperimentId);
                _ = onAllReceived(e.ExperimentId, e.Current, e.Token);
                dictionary.Remove(dateKey);
            }
        }
    }
}