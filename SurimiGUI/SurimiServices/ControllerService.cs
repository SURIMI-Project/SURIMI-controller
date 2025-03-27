using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using SurimiController;
using SurimiGUI.Models;
using SurimiInitRequest;
using SurimiWorkflow;
using System.Text;

namespace SurimiGUI.Services
{
    public class ControllerService
    {
        private readonly Workflow.WorkflowClient _workflowClient;
        private readonly Controller.ControllerClient _controllerClient;

        public ControllerService(Workflow.WorkflowClient workflowClient, Controller.ControllerClient controllerClient)
        {
            _workflowClient = workflowClient;
            _controllerClient = controllerClient;
        }

        public async Task<string> Init(SimulationConfig config)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(1000000);

            try
            {
                var reply = await _workflowClient.InitAsync(new InitRequest { ExperimentId = config.ExperimentId }, cancellationToken: cts.Token);
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

        public async Task<string> RunSimulation()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(1000000);

            try
            {
                var reply = await _controllerClient.RunSimulationAsync(new Empty(), cancellationToken: cts.Token);
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
