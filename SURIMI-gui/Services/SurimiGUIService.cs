using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Grpc.Surimi;
using System.Diagnostics;
using System.Text;

namespace SURIMI_gui.Services
{
    public class SurimiGUIService
    {
        private readonly ControllerService.ControllerServiceClient _controllerClient;
        private readonly ILogger<SurimiGUIService> _logger;
        private static readonly ActivitySource _activitySource = new("SurimiGUI");

        public SurimiGUIService(ControllerService.ControllerServiceClient controllerClient, ILogger<SurimiGUIService> logger)
        {
            _controllerClient = controllerClient;
            _logger = logger;
        }

        public async Task<string> Submit(Models.ExperimentConfig config, CancellationToken token)
        {
            Activity.Current = null; // Ensure no previous activity is set. In a Blazor application, the Activity.Current might be unaltered which causes telemetry to use the same TraceId for all requests, leading to confusion in telemetry data.
            SubmitExperimentResponse reply;
            try
            {
                reply = await _controllerClient.SubmitExperimentAsync(new SubmitExperimentRequest
                {
                    ScenarioName = config.ScenarioName ?? "",
                    EndDateTime = config.EndDateTime.HasValue ? Timestamp.FromDateTime(DateTime.SpecifyKind(config.EndDateTime.Value, DateTimeKind.Utc)) : null,
                    ExperimentId = config.ExperimentId,
                    NumberOfRuns = config.NumberOfRuns,
                    RegulationsDefinitionsSummary = new RegulationDefinitionsSummary()
                    {
                        TargetFishingMortalities = {
                            new TargetFishingMortality() {
                                Species = new Species() {
                                    SpeciesCode = "MUT",    // Red mullet
                                    LifeStage = "adult"
                                },
                                BiomassLimit = 0.018,
                                BiomassBase = 0.072,
                                FMax = 0.317561
                            },
                            new TargetFishingMortality() {
                                Species = new Species() {
                                    SpeciesCode = "BFT"    // Blue fin tuna
                                },
                                BiomassLimit = 0.008,
                                BiomassBase = 0.032,
                                FMax = 0.6216745
                            }
                        }
                    },
                    ClimateScenario = config.ClimateScenario ?? ""

                },
                cancellationToken: token);
            }
            catch (RpcException ex)
            {
                return CreateErrorStringFromGrpcException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initialising simulation");
                return ex.Message;
            }

            return reply.ExperimentId;
        }

        public async Task<string> RemoveExperimentAsync(string experimentId, CancellationToken token)
        {
            Activity.Current = null; // Ensure no previous activity is set
            try
            {
                var reply = await _controllerClient.RemoveExperimentAsync(new RemoveExperimentRequest() { ExperimentId = experimentId }, cancellationToken: token);
            }
            catch (RpcException ex)
            {
                return CreateErrorStringFromGrpcException(ex);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "OK";
        }

        public async Task<List<Models.SimulationStatus>> GetAllSimulationsAsync(CancellationToken token)
        {
            // for this method we do not set the Activity.Current to null because we want to group all calls to this method under the same Activity in telemetry. So it shows as one line in the Aspire Dashboard
            using var activity = _activitySource.StartActivity("GetAllSimulations", ActivityKind.Server, parentContext: default);

            GetAllSimulationStatusesResponse reply;
            try
            {
                reply = await _controllerClient.GetAllSimulationStatusesAsync(new GetAllSimulationStatusesRequest(), cancellationToken: token);
            }
            catch (RpcException ex)
            {
                throw new Exception(CreateErrorStringFromGrpcException(ex));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return reply.SimulationStatuses.Select(sim => new Models.SimulationStatus
            {
                SimulationId = sim.SimulationId,
                ExperimentId = sim.ExperimentId,
                ScenarioName = sim.ScenarioName,
                StartDateTime = sim.StartDateTime.ToDateTime(),
                EndDateTime = sim.EndDateTime.ToDateTime(),
                SimulationStarted = sim.SimulationStarted?.ToDateTime(),
                SimulationDuration = sim.SimulationDuration?.ToTimeSpan(),
                SimulationCurrent = sim.SimulationCurrent?.ToDateTime(),
                Status = sim.Status,
                IP = string.IsNullOrEmpty(sim.EcologyHost) ? string.Empty : sim.EcologyHost.Replace("http://", "").Split(':')[3]
            }).ToList();
        }

        public async Task<List<string>> GetScenarioNamesAsync(CancellationToken token)
        {
            Activity.Current = null; // Ensure no previous activity is set
            GetScenarioNamesResponse reply;
            try
            {
                reply = await _controllerClient.GetScenarioNamesAsync(new GetScenarioNamesRequest(), cancellationToken: token);
            }
            catch (RpcException ex)
            {
                throw new Exception(CreateErrorStringFromGrpcException(ex));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return reply.ScenarioNames.ToList();
        }

        public async Task<List<string>> GetClimateScenariosAsync(string scenarioName, CancellationToken token)
        {
            Activity.Current = null; // Ensure no previous activity is set
            GetSimulationContractResponse reply;
            try
            {
                reply = await _controllerClient.GetSimulationContractAsync(new GetSimulationContractRequest { ScenarioName = scenarioName ?? "" }, cancellationToken: token);
            }
            catch (RpcException ex)
            {
                throw new Exception(CreateErrorStringFromGrpcException(ex));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return reply.Simulation.Items.ClimateScenarios.Select(c => c.ClimateScenarioCode).ToList();
        }

        private string CreateErrorStringFromGrpcException(RpcException ex)
        {
            var error = new StringBuilder();
            error.AppendLine($"Server error: {ex.Status.Detail}");
            var badRequest = ex.GetRpcStatus()?.GetDetail<BadRequest>();
            if (badRequest != null)
            {
                foreach (var fieldViolation in badRequest.FieldViolations)
                {
                    error.AppendLine($"Field: {fieldViolation.Field}");
                    error.AppendLine($"Description: {fieldViolation.Description}");
                }
            }
            error.Append($" Method: {ex.Trailers.GetValue("method")} Application: {ex.Trailers.GetValue("application")}");
            return error.ToString();
        }
    }
}