using Google.Rpc;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace SURIMI_controller.Services
{
    /// <summary>
    /// gRPC client interceptor that logs the trailers returned with a failed call.
    /// Downstream services carry structured error details in the <c>grpc-status-details-bin</c>
    /// trailer and/or plain-text trailer entries; this interceptor surfaces them in the logs.
    /// </summary>
    public class GrpcErrorDetailLoggingInterceptor(ILogger<GrpcErrorDetailLoggingInterceptor> logger) : Interceptor
    {
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(request, context);

            return new AsyncUnaryCall<TResponse>(
                responseAsync: WrapAsync(call.ResponseAsync, context.Method.FullName),
                responseHeadersAsync: call.ResponseHeadersAsync,
                getStatusFunc: call.GetStatus,
                getTrailersFunc: call.GetTrailers,
                disposeAction: call.Dispose);
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            try
            {
                return continuation(request, context);
            }
            catch (RpcException ex)
            {
                LogTrailers(ex, context.Method.FullName);
                throw;
            }
        }

        private async Task<TResponse> WrapAsync<TResponse>(Task<TResponse> inner, string methodName)
        {
            try
            {
                return await inner;
            }
            catch (RpcException ex)
            {
                LogTrailers(ex, methodName);
                throw;
            }
        }

        private void LogTrailers(RpcException ex, string methodName)
        {
            // methodName is e.g. "/surimi.v1.EcologyService/InitialiseSimulation"
            var parts = methodName.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var clientName = parts.Length >= 1 ? parts[0] : methodName;

            // Cancelled is an intentional signal (e.g. experiment cancelled by the user) — not an error
            if (ex.StatusCode == StatusCode.Cancelled)
            {
                logger.LogInformation(
                    "gRPC call {Method} on {Client} was cancelled.",
                    methodName,
                    clientName);
                return;
            }

            var rpcStatus = ex.GetRpcStatus();
            if (rpcStatus != null)
            {
                var details = DecodeStatusDetails(rpcStatus);
                logger.LogError(
                    "gRPC call {Method} on {Client} failed with status {StatusCode}: {Message}.{Details}",
                    methodName,
                    clientName,
                    ex.StatusCode,
                    ex.Status.Detail,
                    details.Count > 0 ? $"{Environment.NewLine}{string.Join(Environment.NewLine, details)}" : string.Empty);
                return;
            }

            // No google.rpc.Status present (e.g. connection failure) — always log, append trailers if any
            var trailers = ex.Trailers;
            var trailerText = trailers != null && trailers.Count > 0
                ? $". Trailers:{Environment.NewLine}" + string.Join(Environment.NewLine, trailers
                    .Select(e => e.IsBinary
                        ? $"  {e.Key} = <{e.ValueBytes.Length} bytes>"
                        : $"  {e.Key} = {e.Value}"))
                : string.Empty;

            logger.LogError(
                "gRPC call {Method} on {Client} failed with status {StatusCode}: {Message}{Trailers}",
                methodName,
                clientName,
                ex.StatusCode,
                ex.Status.Detail,
                trailerText);
        }

        private static List<string> DecodeStatusDetails(Google.Rpc.Status rpcStatus)
        {
            var lines = new List<string>();
            foreach (var detail in rpcStatus.Details)
            {
                // 1. BadRequest
                if (detail.TryUnpack<BadRequest>(out var badRequest))
                {
                    foreach (var violation in badRequest.FieldViolations)
                        lines.Add($"  BadRequest.FieldViolation: {violation.Field} — {violation.Description}");
                    continue;
                }

                // 2. ErrorInfo
                if (detail.TryUnpack<ErrorInfo>(out var errorInfo))
                {
                    lines.Add($"  ErrorInfo: {errorInfo.Reason} ({errorInfo.Domain})");
                    foreach (var kv in errorInfo.Metadata)
                        lines.Add($"    {kv.Key}: {kv.Value}");
                    continue;
                }

                // 3. PreconditionFailure
                if (detail.TryUnpack<PreconditionFailure>(out var precondition))
                {
                    foreach (var violation in precondition.Violations)
                        lines.Add($"  PreconditionFailure: [{violation.Type}] {violation.Subject} — {violation.Description}");
                    continue;
                }

                // 4. Protovalidate violations
                if (detail.TryUnpack<Buf.Validate.Violations>(out var protoViolations))
                {
                    foreach (var v in protoViolations.Violations_)
                        lines.Add($"  Validation: {v.Field} — {v.Message}");
                    continue;
                }

                // 5. Unknown Any type
                lines.Add($"  Detail: {detail.TypeUrl}");
            }

            return lines;
        }
    }
}
