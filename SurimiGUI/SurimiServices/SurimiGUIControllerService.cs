using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Grpc.Surimi;
using SurimiGUI.Models;
using System.Diagnostics;
using System.Text;

namespace SurimiGUI.Services
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

        public async Task<string> Init(SimulationConfig config, CancellationToken token)
        {
            Activity.Current = null; // Ensure no previous activity is set. In a Blazor application, the Activity.Current might be unaltered which causes telemetry to use the same TraceId for all requests, leading to confusion in telemetry data.
            InitialiseSimulationResponse reply;
            try
            {
                reply = await _controllerClient.InitialiseSimulationAsync(new InitialiseSimulationRequest
                {
                    Simulation = new Grpc.Surimi.Simulation()
                    {
                        ScenarioId = config.ScenarioId,
                        StartDateTime = Timestamp.FromDateTime(config.StartDateTime),
                        StepSize = config.StepSize,
                        SimulationId = config.SimulationId,
                        SimulationDuration = config.SimulationDuration
                    }
                },
                cancellationToken: token);
            }
            catch (RpcException ex)
            {
                return CreateErrorStringFromGrpcException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing simulation");
                return ex.Message;
            }

            return reply.SimulationId;
        }

        public Task<string> RunSimulationAsync(string simulationId, CancellationToken token)
        {
            Activity.Current = null; // Ensure no previous activity is set. In a Blazor application, the Activity.Current might be unaltered which causes telemetry to use the same TraceId for all requests, leading to confusion in telemetry data.
            try
            {
                var reply = _controllerClient.RunSimulationAsync(new RunSimulationRequest() { SimulationId = simulationId }, cancellationToken: token);
                return Task.FromResult("OK");
            }
            catch (RpcException ex)
            {
                return Task.FromResult(CreateErrorStringFromGrpcException(ex));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ex.Message);
            }
        }

        public async Task<string> CancelSimulationAsync(string simulationId, CancellationToken token)
        {
            Activity.Current = null; // Ensure no previous activity is set
            try
            {
                var reply = await _controllerClient.CancelSimulationAsync(new CancelSimulationRequest() { SimulationId = simulationId }, cancellationToken: token);
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

        public async Task<List<Models.Simulation>> GetAllSimulationsAsync(CancellationToken token)
        {
            // for this method we do not set the Activity.Current to null because we want to group all calls to this method under the same Activity in telemetry. So it shows as one line in the Aspire Dashboard
            using var activity = _activitySource.StartActivity("GetAllSimulations", ActivityKind.Server, parentContext: default);

            GetAllSimulationsResponse reply;
            try
            {
                reply = await _controllerClient.GetAllSimulationsAsync(new GetAllSimulationsRequest(), cancellationToken: token);
            }
            catch (RpcException ex)
            {
                throw new Exception(CreateErrorStringFromGrpcException(ex));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return reply.Simulations.Select(sim => new Models.Simulation
            {
                SimulationId = sim.SimulationId,
                ScenarioId = sim.ScenarioId,
                StartDateTime = sim.StartDateTime.ToDateTime(),
                StepSize = sim.StepSize,
                Duration = sim.SimulationDuration,
                SimulationCreated = sim.SimulationCreated?.ToDateTime(),
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