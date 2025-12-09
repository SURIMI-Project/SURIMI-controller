using Grpc.Core;
using Grpc.Surimi;
using SURIMI.ConfigurationService;
using SURIMI.ConfigurationService.Models;

namespace SurimiController.Services
{
    public class SurimiControllerService : ControllerService.ControllerServiceBase
    {
        private readonly ILogger<SurimiControllerService> _logger;

        private readonly ISimulationManager _simulationManager;
        private readonly ISurimiConfigurationService _surimiConfigurationService;

        public SurimiControllerService(ILogger<SurimiControllerService> logger, ISimulationManager simulationManager, ISurimiConfigurationService surimiConfigurationService)
        {
            _logger = logger;
            _simulationManager = simulationManager;
            this._surimiConfigurationService = surimiConfigurationService;
        }

        public override async Task<InitialiseSimulationResponse> InitialiseSimulation(InitialiseSimulationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Simulation {SimulationId} is initializing scenario {ScenarioId}", request.SimulationId, request.ScenarioId);
            System.Diagnostics.Activity.Current?.SetTag("simulation_id", request.SimulationId);

            string configuration = File.ReadAllText(Directory.GetCurrentDirectory() + "/western_med_contract.yaml");
            var surimiConfiguration = _surimiConfigurationService.ReadYaml(configuration);

            var simulation = GetSimulation(surimiConfiguration);
            await _simulationManager.InitSimulationAsync(
                request.SimulationId,
                request.ScenarioId,
                request.EndDateTime.ToDateTime(),
                simulation
                );

            //activity?.AddEvent(new ActivityEvent("Finished ecopoath and poseidon"));
            return new InitialiseSimulationResponse() { SimulationId = request.SimulationId };
        }

        public override Task<RunSimulationResponse> RunSimulation(RunSimulationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Running simulation {SimulationId} ...", request.SimulationId);
            System.Diagnostics.Activity.Current?.SetTag("simulation_id", request.SimulationId);

            try
            {
                _simulationManager.RunSimulationAsync(request.SimulationId, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in RunSimulation. ID={SimulationId}", request.SimulationId);
            }

            return Task.FromResult(new RunSimulationResponse() { SimulationId = request.SimulationId });
        }

        public override Task<CancelSimulationResponse> CancelSimulation(CancelSimulationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Cancel Simulation {SimulationId}", request.SimulationId);
            System.Diagnostics.Activity.Current?.SetTag("simulation_id", request.SimulationId);
            _simulationManager.CancelSimulation(request.SimulationId);

            return Task.FromResult(new CancelSimulationResponse() { SimulationId = request.SimulationId });
        }

        public override async Task<GetAllSimulationStatusesResponse> GetAllSimulationStatuses(GetAllSimulationStatusesRequest get, ServerCallContext context)
        {
            return await _simulationManager.GetAllSimulationStatussesAsync(context.CancellationToken);
        }

        private Grpc.Surimi.Simulation GetSimulation(SurimiConfiguration surimiConfiguration)
        {
            return new Grpc.Surimi.Simulation()
            {
                CaseStudyName = surimiConfiguration.Simulation?.CaseStudyName ?? string.Empty,
                StartDateTime = surimiConfiguration.Simulation != null ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(surimiConfiguration.Simulation.StartDateTime.ToUniversalTime()) : null,
                MaximumEndDateTime = surimiConfiguration.Simulation != null ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(surimiConfiguration.Simulation.MaximumEndDateTime.ToUniversalTime()) : null,
                TimeStep = surimiConfiguration.Simulation?.TimeStep ?? string.Empty,
                Geography = surimiConfiguration.Simulation?.Geography != null ? new Grpc.Surimi.Geography()
                {
                    RasterCellOrigin = Enum.Parse<Grpc.Surimi.RasterCellOrigin>(surimiConfiguration.Simulation.Geography.RasterCellOrigin.ToString()),
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
                    DateAndTime = surimiConfiguration.Simulation?.Standards?.DateAndTime ?? string.Empty,
                    SpeciesCode = surimiConfiguration.Simulation?.Standards?.SpeciesCode ?? string.Empty,
                    GearCode = surimiConfiguration.Simulation?.Standards?.GearCode ?? string.Empty,
                    LifeStage = surimiConfiguration.Simulation?.Standards?.LifeStage ?? string.Empty,
                    MarketCode = surimiConfiguration.Simulation?.Standards?.MarketCode ?? string.Empty,
                    Currency = surimiConfiguration.Simulation?.Standards?.Currency ?? string.Empty,
                    CountryCode = surimiConfiguration.Simulation?.Standards?.CountryCode ?? string.Empty,
                    Measurements = new Grpc.Surimi.Measurement()
                    {
                        System = surimiConfiguration.Simulation?.Standards?.Measurements?.System ?? string.Empty,

                        Units =
                        {
                            (surimiConfiguration.Simulation?.Standards?.Measurements?.Units ?? Enumerable.Empty<UnitType>())
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
                        (surimiConfiguration.Items?.Species ?? Enumerable.Empty<SURIMI.ConfigurationService.Models.Species>())
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
                        (surimiConfiguration.Items?.FleetSegments ?? Enumerable.Empty<SURIMI.ConfigurationService.Models.FleetSegment>())
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
                        (surimiConfiguration.Items?.Markets ?? Enumerable.Empty<SURIMI.ConfigurationService.Models.Market>())
                            .Select(m => new Grpc.Surimi.Market()
                            {
                                MarketCode = m.MarketCode ?? string.Empty
                            })
                    },
                }
            };
        }
    }
}
