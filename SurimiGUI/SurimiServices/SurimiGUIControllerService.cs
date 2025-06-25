using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Grpc.Surimi;
using SurimiGUI.Models;
using System.Text;

namespace SurimiGUI.Services
{
    public class SurimiGUIControllerService
    {
        private readonly ControllerService.ControllerServiceClient _controllerClient;
        private readonly ILogger<SurimiGUIControllerService> _logger;

        public SurimiGUIControllerService(ControllerService.ControllerServiceClient controllerClient, ILogger<SurimiGUIControllerService> logger)
        {
            _controllerClient = controllerClient;
            _logger = logger;
        }

        public async Task<string> Init(SimulationConfig config, CancellationToken token)
        {
            InitSimulationResponse reply;
            try
            {
                reply = await _controllerClient.InitSimulationAsync(new InitSimulationRequest
                {
                    Simulation = new Grpc.Surimi.Simulation()
                    {
                        ScenarioId = config.ScenarioId,
                        StartDateTime = Timestamp.FromDateTime(config.StartDateTime),
                        StepSize = config.StepSize,
                        SimulationId = config.SimulationId
                    }
                },
                cancellationToken: token);
            }
            catch (RpcException ex)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing simulation");
                return ex.Message;
            }

            return reply.SimulationId;
        }

        public async Task<string> RunSimulation(SimulationConfig config, CancellationToken token)
        {
            try
            {
                var reply = await _controllerClient.RunSimulationAsync(new RunSimulationRequest()
                {
                    StartDateTime = Timestamp.FromDateTime(config.StartDateTime.ToUniversalTime()),
                    StepSize = config.StepSize,
                    SimulationDuration = config.SimulationDuration,
                    SimulationId = config.SimulationId
                }, cancellationToken: token);
            }
            catch (RpcException ex)
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
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "OK";
        }

        public async Task<List<Models.Simulation>> GetAllSimulations(CancellationToken token)
        {
            GetAllSimulationsResponse reply;
            try
            {
                reply = await _controllerClient.GetAllSimulationsAsync(new GetAllSimulationsRequest(), cancellationToken: token);
            }
            catch (RpcException ex)
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
                throw new Exception(error.ToString());
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
                SimulationCreated = sim.SimulationCreated?.ToDateTime(),
                SimulationCurrent = sim.SimulationCurrent?.ToDateTime(),
                Status = sim.Status,
                IP = string.IsNullOrEmpty(sim.EcologyHost) ? string.Empty : sim.EcologyHost.Replace("http://", "").Split(':')[3]
            }).ToList();
        }
    }
}