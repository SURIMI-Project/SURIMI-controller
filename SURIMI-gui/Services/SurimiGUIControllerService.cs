using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Grpc.Surimi;
using System.Diagnostics;
using System.Text;

namespace SURIMI_gui.Services
{
    public class SurimiGUIControllerService
    {
        private readonly ControllerService.ControllerServiceClient _controllerClient;
        private readonly ILogger<SurimiGUIControllerService> _logger;
        private static readonly ActivitySource _activitySource = new("SurimiGUI");

        public SurimiGUIControllerService(ControllerService.ControllerServiceClient controllerClient, ILogger<SurimiGUIControllerService> logger)
        {
            _controllerClient = controllerClient;
            _logger = logger;
        }

        public async Task<string> Init(Models.ExperimentConfig config, CancellationToken token)
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
                                    SpeciesCode = "HKE",
                                    LifeStage = "juvenile"
                                },
                                BiomassLimit = 0.0110,
                                BiomassBase = 0.0440,
                                FMax = 1.081
                            },
                            new TargetFishingMortality() {
                                Species = new Species() {
                                    SpeciesCode = "PIL",
                                    LifeStage = "adult"
                                },
                                BiomassLimit = 1.103,
                                BiomassBase = 2.207,
                                FMax = 0.076
                            }
                        }
                    },
                    ClimateScenario = "RCP_4.5"

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