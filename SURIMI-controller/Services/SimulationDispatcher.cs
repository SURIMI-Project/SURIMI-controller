using Grpc.Core;
using System.Collections.Concurrent;

namespace SURIMI_controller.Services
{
    public class SimulationDispatcher
    {
        private readonly List<string> podNames = new()
        {
            "surimi-ecopath-0",
            "surimi-ecopath-1",
            "surimi-ecopath-2",
            "surimi-ecopath-3",
            "surimi-ecopath-4"
        };

        private readonly ConcurrentDictionary<string, string> simulationToPodMap = new();   // SimulationId is key
        private readonly ConcurrentDictionary<string, bool> podAvailability;    // Pod is key
        private readonly ILogger<SimulationDispatcher> _logger;
        private readonly ILogger<GrpcErrorDetailLoggingInterceptor> _interceptorLogger;

        public SimulationDispatcher(ILogger<SimulationDispatcher> logger, ILogger<GrpcErrorDetailLoggingInterceptor> interceptorLogger)
        {
            podAvailability = new ConcurrentDictionary<string, bool>(
                podNames.Select(p => new KeyValuePair<string, bool>(p, true))
            );
            _logger = logger;
            _interceptorLogger = interceptorLogger;
        }

        public AsyncUnaryCall<TResponse> DispatchAsync<TClient, TRequest, TResponse>(
            TRequest request,
            string simulationId,
            Func<TClient, TRequest, AsyncUnaryCall<TResponse>> grpcMethod)
            where TClient : ClientBase<TClient>
        {
            var address = Environment.GetEnvironmentVariable("ECOPATH_URL");    // for example: http://pod.surimi-ecopath.namespace.svc.cluster.local:8080
            var ns = Environment.GetEnvironmentVariable("POD_NAMESPACE");       // this environment variable is set in the Deployment yaml to "user-rikkert", "project-surimi" etc
            string? pod;

            if (!simulationToPodMap.TryGetValue(simulationId, out pod))
            {
                // so this is a new simulation
                // Atomically claim the first available pod to avoid race conditions when multiple
                // simulations start concurrently (TryUpdate only succeeds if value is still true).
                pod = null;
                foreach (var kvp in podAvailability)
                {
                    if (kvp.Value && podAvailability.TryUpdate(kvp.Key, false, true))
                    {
                        pod = kvp.Key;
                        break;
                    }
                }
                if (pod == null)
                    throw new Exception("No available pods");

                if (address!.Contains("pod"))      // so only when not running on a Dev machine. Because then address = http://localhost:7890
                {
                    address = address!.Replace("pod", pod);
                    address = address!.Replace("namespace", ns);
                }

                // Check if the dns address can be resolved. If not, don't use this pod yet
                try
                {
                    var uri = new Uri(address);
                    var host = uri.Host;
                    var addresses = System.Net.Dns.GetHostAddresses(host);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    throw new RpcException(
                        new Status(StatusCode.Unavailable, $"Could not resolve DNS for pod {pod} with address {address}"),
                        new Metadata
                        {
                            { "pod", pod },
                            { "simulationId", simulationId }
                        }
                    );
                }
                catch (Exception ex)
                {
                    throw new RpcException(
                        new Status(StatusCode.Internal, $"Error resolving DNS for pod {pod}: {ex.Message}"),
                        new Metadata
                        {
                            { "pod", pod },
                            { "simulationId", simulationId }
                        }
                    );
                }

                // Only reserve the pod if a simulationId is provided and is a valid Guid. This allows us to use the dispatcher for non-simulation related calls without consuming pod resources.
                // for example the GetProtocolVersion call from the client, doesn't need to be dispatched to a specific pod. The versions are all the same.
                if (Guid.TryParse(simulationId, out var guid))
                {
                    _logger.LogInformation("Add Simulation:{SimulationId} with pod:{pod} to simulationToPodMap", simulationId, pod);
                    simulationToPodMap[simulationId] = pod;
                    podAvailability[pod] = false;
                }
            }

            if (address!.Contains("pod"))      // so only when not running on a Dev machine. Because then address = http://localhost:7890
            {
                address = address!.Replace("pod", pod);
                address = address!.Replace("namespace", ns);
            }

            _logger.LogInformation("Using address {Address} for pod {Pod} and simulationId {SimulationId}", address, pod, simulationId);
            try
            {
                var client = DynamicGrpcClientFactory.CreateClient<TClient>(address, _interceptorLogger);
                var response = grpcMethod(client, request);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while dispatching request to pod {Pod} for simulationId {SimulationId}", pod, simulationId);
                throw;
            }
        }

        public async Task<TResponse> DispatchWithRetryAsync<TClient, TRequest, TResponse>(
            TRequest request,
            string simulationId,
            Func<TClient, TRequest, AsyncUnaryCall<TResponse>> grpcMethod,
            int maxRetries = 3,
            int retryDelayMs = 500)
            where TClient : ClientBase<TClient>
        {
            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await DispatchAsync(request, simulationId, grpcMethod);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable && attempt < maxRetries)
                {
                    _logger.LogWarning("Transient gRPC Unavailable for simulationId {SimulationId} (attempt {Attempt}/{MaxRetries}), retrying in {DelayMs}ms…",
                        simulationId, attempt + 1, maxRetries, retryDelayMs);
                    await Task.Delay(retryDelayMs);
                }
            }

            // Final attempt – let any exception propagate
            return await DispatchAsync(request, simulationId, grpcMethod);
        }

        public void ReleasePodFromSimulation(string simulationId)
        {
            if (simulationToPodMap.TryRemove(simulationId, out var pod))
            {
                podAvailability[pod] = true;
                _logger.LogInformation("Simulation {SimulationId} Released pod {Pod} from simulationId", simulationId, pod);
            }
            else
            {
                _logger.LogWarning("Simulation {SimulationId} No pod found for simulationId to release", simulationId);
            }
        }
    }
}