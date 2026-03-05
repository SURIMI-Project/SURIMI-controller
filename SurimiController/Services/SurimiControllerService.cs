using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI.ConfigurationService;
using SURIMI.Datamodel;

namespace SurimiController.Services
{
    public class SurimiControllerService : ControllerService.ControllerServiceBase
    {
        private readonly ILogger<SurimiControllerService> _logger;

        private readonly IExperimentManager _experimentManager;
        private readonly ISurimiConfigurationService _surimiConfigurationService;

        public SurimiControllerService(GrpcClientFactory clientFactory, IExperimentManager experimentManager, ILogger<SurimiControllerService> logger, ISurimiConfigurationService surimiConfigurationService)
        {
            _logger = logger;
            _experimentManager = experimentManager;
            this._surimiConfigurationService = surimiConfigurationService;
        }

        public override async Task<InitialiseExperimentResponse> InitialiseExperiment(InitialiseExperimentRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Simulation {ExperimentId} is initializing", request.ExperimentId);
            System.Diagnostics.Activity.Current?.SetTag("experiment_id", request.ExperimentId);

            // TODO: the name of the contract should come from the request
            var surimiConfiguration = await _surimiConfigurationService.ReadConfigurationAsync("western_med_contract");

            var simulation = GetSimulation(surimiConfiguration);

            await _experimentManager.InitialiseExperiment(request, simulation);

            return new InitialiseExperimentResponse() { ExperimentId = request.ExperimentId };
        }

        public override Task<RunExperimentResponse> RunExperiment(RunExperimentRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Running experiment {ExperimentId} ...", request.ExperimentId);
            System.Diagnostics.Activity.Current?.SetTag("Running experiment {ExperimentId} ...", request.ExperimentId);
            _experimentManager.RunExperimentAsync(request.ExperimentId, context.CancellationToken);

            return Task.FromResult(new RunExperimentResponse() { ExperimentId = request.ExperimentId });
        }

        public override async Task<CancelExperimentResponse> CancelExperiment(CancelExperimentRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Cancel Experiment {ExperimentId}", request.ExperimentId);
            System.Diagnostics.Activity.Current?.SetTag("Experiment_id", request.ExperimentId);
            await _experimentManager.CancelExperimentAsync(request.ExperimentId, context.CancellationToken);

            return new CancelExperimentResponse() { ExperimentId = request.ExperimentId };
        }

        public override async Task<GetAllSimulationStatusesResponse> GetAllSimulationStatuses(GetAllSimulationStatusesRequest get, ServerCallContext context)
        {
            return await _experimentManager.GetAllSimulationStatussesAsync(context.CancellationToken);
        }

        /// <summary>
        /// Mapping method from SurimiConfiguration to Grpc.Surimi.Simulation
        /// </summary>
        /// <param name="surimiConfiguration"></param>
        /// <returns></returns>
        private Grpc.Surimi.Simulation GetSimulation(SurimiConfiguration surimiConfiguration)
        {
            if (surimiConfiguration.Simulation is null)
            {
                throw new ArgumentNullException(nameof(surimiConfiguration.Simulation), "Simulation configuration cannot be null.");
            }
            surimiConfiguration.Simulation.StartDateTime = DateTime.SpecifyKind(surimiConfiguration.Simulation.StartDateTime, DateTimeKind.Utc);
            surimiConfiguration.Simulation.MaximumEndDateTime = DateTime.SpecifyKind(surimiConfiguration.Simulation.MaximumEndDateTime, DateTimeKind.Utc);
            return new Grpc.Surimi.Simulation()
            {
                CaseStudyName = surimiConfiguration.Simulation.CaseStudyName ?? string.Empty,
                StartDateTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(surimiConfiguration.Simulation.StartDateTime),
                MaximumEndDateTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(surimiConfiguration.Simulation.MaximumEndDateTime),
                TimeStep = surimiConfiguration.Simulation.TimeStep ?? string.Empty,
                Geography = surimiConfiguration.Simulation.Geography != null ? new Grpc.Surimi.Geography()
                {
                    RasterCellOrigin = System.Enum.Parse<Grpc.Surimi.RasterCellOrigin>(surimiConfiguration.Simulation.Geography.RasterCellOrigin.ToString()),
                    Crs = new Grpc.Surimi.CoordinateReferenceSystem()
                    {
                        Name = surimiConfiguration.Simulation.Geography.Crs?.Name ?? string.Empty,
                        Authority = surimiConfiguration.Simulation.Geography.Crs?.Authority ?? string.Empty,
                        Code = surimiConfiguration.Simulation.Geography.Crs?.Code ?? string.Empty,
                    },
                    Xres = surimiConfiguration.Simulation.Geography.Xres,
                    Yres = surimiConfiguration.Simulation.Geography.Yres,
                    Ncol = surimiConfiguration.Simulation.Geography.Ncol,
                    Nrow = surimiConfiguration.Simulation.Geography.Nrow,
                    Xmin = surimiConfiguration.Simulation.Geography.Xmin,
                    Xmax = surimiConfiguration.Simulation.Geography.Xmax,
                    Ymin = surimiConfiguration.Simulation.Geography.Ymin,
                    Ymax = surimiConfiguration.Simulation.Geography.Ymax
                } : null,
                Standards = new Grpc.Surimi.Standards()
                {
                    DateAndTime = surimiConfiguration.Standards?.DateAndTime ?? string.Empty,
                    SpeciesCode = surimiConfiguration.Standards?.SpeciesCode ?? string.Empty,
                    GearCode = surimiConfiguration.Standards?.GearCode ?? string.Empty,
                    LifeStage = surimiConfiguration.Standards?.LifeStage ?? string.Empty,
                    MarketCode = surimiConfiguration.Standards?.MarketCode ?? string.Empty,
                    Currency = surimiConfiguration.Standards?.Currency ?? string.Empty,
                    CountryCode = surimiConfiguration.Standards?.CountryCode ?? string.Empty,
                    Measurements = new Grpc.Surimi.Measurement()
                    {
                        System = surimiConfiguration.Standards?.Measurements?.System ?? string.Empty,

                        Units =
                        {
                            (surimiConfiguration.Standards ?.Measurements ?.Units ?? Enumerable.Empty<UnitType>())
                                .Select(u => new Grpc.Surimi.Unit
                                {
                                    Quantity = u.Quantity ?? string.Empty,
                                    Unit_ = u.Unit ?? string.Empty, // Unit_ because 'unit' may be reserved in proto
                                })
                        }

                    },
                },
                Items = new Grpc.Surimi.Items()
                {
                    Species =
                    {
                        (surimiConfiguration.Items?.Species ?? Enumerable.Empty<SURIMI.Datamodel.Species>())
                            .Select(s => new Grpc.Surimi.Species()
                            {
                                SpeciesCode = s.SpeciesCode ?? string.Empty,
                                LengthClass = s.LengthClass ?? string.Empty,
                                Age = s.Age ?? string.Empty,
                                LifeStage = s.LifeStage ?? string.Empty,
                            })
                    },
                    FleetSegments =
                    {
                        (surimiConfiguration.Items?.FleetSegments ?? Enumerable.Empty<SURIMI.Datamodel.FleetSegment>())
                            .Select(g => new Grpc.Surimi.FleetSegment()
                            {
                                GearCode = g.GearCode ?? string.Empty,
                                VesselLengthClass = g.VesselLengthClass ?? string.Empty,
                                Scale = g.Scale ?? string.Empty,
                                CountryCode = g.CountryCode ?? string.Empty,
                            })
                    },
                    Markets =
                    {
                        (surimiConfiguration.Items?.Markets ?? Enumerable.Empty<SURIMI.Datamodel.Market>())
                            .Select(m => new Grpc.Surimi.Market()
                            {
                                MarketCode = m.MarketCode ?? string.Empty
                            })
                    },
                    Currencies =
                    {
                        (surimiConfiguration.Items?.Currencies ?? Enumerable.Empty<SURIMI.Datamodel.Currency>())
                            .Select(m => new Grpc.Surimi.Currency()
                            {
                                Code = m.CurrencyCode ?? string.Empty
                            })
                    }
                }
            };
        }
    }
}
