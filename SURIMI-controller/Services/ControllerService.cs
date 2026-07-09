using Grpc.Core;
using Grpc.Surimi;
using SURIMI.Common.gRPC.Services;
using SURIMI.Datamodel;
using SURIMI_controller.ConfigurationService;

namespace SURIMI_controller.Services
{
    public class ControllerService : Grpc.Surimi.ControllerService.ControllerServiceBase
    {
        private readonly ILogger<ControllerService> _logger;
        private readonly IExperimentManager _experimentManager;
        private readonly IConfigurationService _surimiConfigurationService;
        private readonly string _version;

        public ControllerService(IExperimentManager experimentManager, ILogger<ControllerService> logger, IConfigurationService surimiConfigurationService, ProtocolVersionService protocolVersionService)
        {
            _logger = logger;
            _experimentManager = experimentManager;
            _surimiConfigurationService = surimiConfigurationService;
            _version = protocolVersionService.LoadVersion();
        }

        public override async Task<SubmitExperimentResponse> SubmitExperiment(SubmitExperimentRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Simulation {ExperimentId} is initializing", request.ExperimentId);
            System.Diagnostics.Activity.Current?.SetTag("experiment_id", request.ExperimentId);

            var surimiConfiguration = await _surimiConfigurationService.ReadConfigurationAsync(request.ScenarioName);

            var simulation = GetSimulation(surimiConfiguration);

            await _experimentManager.SubmitExperiment(request, simulation, context.CancellationToken);

            return new SubmitExperimentResponse() { ExperimentId = request.ExperimentId };
        }

        public override async Task<RemoveExperimentResponse> RemoveExperiment(RemoveExperimentRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Cancel Experiment {ExperimentId}", request.ExperimentId);
            System.Diagnostics.Activity.Current?.SetTag("Experiment_id", request.ExperimentId);
            await _experimentManager.CancelExperimentAsync(request.ExperimentId, context.CancellationToken);

            return new RemoveExperimentResponse() { ExperimentId = request.ExperimentId };
        }

        public override async Task<GetAllSimulationStatusesResponse> GetAllSimulationStatuses(GetAllSimulationStatusesRequest get, ServerCallContext context)
        {
            return await _experimentManager.GetAllSimulationStatussesAsync(context.CancellationToken);
        }

        public override async Task<GetSimulationContractResponse> GetSimulationContract(GetSimulationContractRequest request, ServerCallContext context)
        {
            var surimiConfiguration = await _surimiConfigurationService.ReadConfigurationAsync(@"northwestern_med");
            var simulation = GetSimulation(surimiConfiguration);
            return new GetSimulationContractResponse() { Simulation = simulation };
        }
        public override Task<GetProtocolVersionResponse> GetProtocolVersion(GetProtocolVersionRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetProtocolVersionResponse() { ProtocolVersion = _version });
        }

        /// <summary>
        /// Mapping method from SurimiContract to Grpc.Surimi.Simulation
        /// </summary>
        /// <param name="surimiContract"></param>
        /// <returns></returns>
        private Grpc.Surimi.Simulation GetSimulation(SurimiContract surimiContract)
        {
            if (surimiContract.Simulation is null)
            {
                throw new ArgumentNullException(nameof(surimiContract.Simulation), "Simulation contract cannot be null.");
            }
            surimiContract.Simulation.StartDateTime = DateTime.SpecifyKind(surimiContract.Simulation.StartDateTime, DateTimeKind.Utc);
            surimiContract.Simulation.MaximumEndDateTime = DateTime.SpecifyKind(surimiContract.Simulation.MaximumEndDateTime, DateTimeKind.Utc);
            return new Grpc.Surimi.Simulation()
            {
                CaseStudyName = surimiContract.Simulation.CaseStudyName ?? string.Empty,
                StartDateTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(surimiContract.Simulation.StartDateTime),
                MaximumEndDateTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(surimiContract.Simulation.MaximumEndDateTime),
                TimeStep = surimiContract.Simulation.TimeStep ?? string.Empty,
                Geography = surimiContract.Simulation.Geography != null ? new Grpc.Surimi.Geography()
                {
                    RasterCellOrigin = System.Enum.Parse<Grpc.Surimi.RasterCellOrigin>(surimiContract.Simulation.Geography.RasterCellOrigin.ToString()),
                    Crs = new Grpc.Surimi.CoordinateReferenceSystem()
                    {
                        Name = surimiContract.Simulation.Geography.Crs?.Name ?? string.Empty,
                        Authority = surimiContract.Simulation.Geography.Crs?.Authority ?? string.Empty,
                        Code = surimiContract.Simulation.Geography.Crs?.Code ?? string.Empty,
                    },
                    Xres = surimiContract.Simulation.Geography.Xres,
                    Yres = surimiContract.Simulation.Geography.Yres,
                    Ncol = surimiContract.Simulation.Geography.Ncol,
                    Nrow = surimiContract.Simulation.Geography.Nrow,
                    Xmin = surimiContract.Simulation.Geography.Xmin,
                    Xmax = surimiContract.Simulation.Geography.Xmax,
                    Ymin = surimiContract.Simulation.Geography.Ymin,
                    Ymax = surimiContract.Simulation.Geography.Ymax
                } : null,
                Standards = new Grpc.Surimi.Standards()
                {
                    DateAndTime = surimiContract.Standards?.DateAndTime ?? string.Empty,
                    SpeciesCode = surimiContract.Standards?.SpeciesCode ?? string.Empty,
                    GearCode = surimiContract.Standards?.GearCode ?? string.Empty,
                    LifeStage = surimiContract.Standards?.LifeStage ?? string.Empty,
                    MarketCode = surimiContract.Standards?.MarketCode ?? string.Empty,
                    Currency = surimiContract.Standards?.Currency ?? string.Empty,
                    CountryCode = surimiContract.Standards?.CountryCode ?? string.Empty,
                    //CategoryCode = surimiContract.Standards?.CategoryCode ?? string.Empty,        TODO
                    Measurements = new Grpc.Surimi.Measurement()
                    {
                        System = surimiContract.Standards?.Measurements?.System ?? string.Empty,

                        Units =
                        {
                            (surimiContract.Standards ?.Measurements ?.Units ?? Enumerable.Empty<UnitType>())
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
                        (surimiContract.Items?.Species ?? Enumerable.Empty<SURIMI.Datamodel.Species>())
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
                        (surimiContract.Items?.FleetSegments ?? Enumerable.Empty<SURIMI.Datamodel.FleetSegment>())
                            .Select(g => new Grpc.Surimi.FleetSegment()
                            {
                                GearCode = g.GearCode ?? string.Empty,
                                VesselLengthClass = g.VesselLengthClass ?? string.Empty,
                                Scale = g.Scale ?? string.Empty,
                                CountryCode = g.CountryCode ?? string.Empty,
                                Model = g.Model ?? string.Empty,
                            })
                    },
                    Markets =
                    {
                        (surimiContract.Items?.Markets ?? Enumerable.Empty<SURIMI.Datamodel.Market>())
                            .Select(m => new Grpc.Surimi.Market()
                            {
                                MarketCode = m.MarketCode ?? string.Empty
                            })
                    },
                    Currencies =
                    {
                        (surimiContract.Items?.Currencies ?? Enumerable.Empty<SURIMI.Datamodel.Currency>())
                            .Select(m => new Grpc.Surimi.Currency()
                            {
                                Code = m.CurrencyCode ?? string.Empty
                            })
                    },
                    PriceCategories =
                    {
                        (surimiContract.Items?.Price_Categories ?? Enumerable.Empty<SURIMI.Datamodel.PriceCategory>())
                            .Select(m => new Grpc.Surimi.PriceCategory()
                            {
                                CategoryCode = m.CategoryCode ?? string.Empty
                            })
                    }
                }
            };
        }
    }
}
