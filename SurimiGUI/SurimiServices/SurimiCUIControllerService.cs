using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using Grpc.Surimi;
using SurimiGUI.Models;
using System.Text;

namespace SurimiGUI.Services
{
    public class SurimiCUIControllerService
    {
        private readonly WorkflowService.WorkflowServiceClient _workflowClient;
        private readonly ControllerService.ControllerServiceClient _controllerClient;

        public SurimiCUIControllerService(WorkflowService.WorkflowServiceClient workflowClient, ControllerService.ControllerServiceClient controllerClient)
        {
            _workflowClient = workflowClient;
            _controllerClient = controllerClient;
        }

        public async Task<string> Init(SimulationConfig config)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(1000000);

            InitSimulationResponse reply;
            try
            {
                reply = await _controllerClient.InitSimulationAsync(new InitSimulationRequest { ScenarioId = config.ScenarioId, StartDateTime = Timestamp.FromDateTime(config.StartDateTime), StepSize = config.StepSize }, cancellationToken: cts.Token);
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
                return error.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return reply.SimulationId;
        }

        public async Task<string> RunSimulation(SimulationConfig config)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(1000000);

            try
            {
                var reply = await _controllerClient.RunSimulationAsync(new RunSimulationRequest() 
                { 
                    StartDateTime = Timestamp.FromDateTime(config.StartDateTime.ToUniversalTime()),
                    StepSize = config.StepSize,
                    SimulationDuration = config.SimulationDuration,
                    SimulationId = config.SimulationId
                }, cancellationToken: cts.Token);
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
                return error.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "OK";
        }
    }
}
