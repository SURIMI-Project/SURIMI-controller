using Grpc.Core;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace SURIMI_controller.Services
{
    public class SimulationDispatcher : ISimulationDispatcher
    {
        // Key = podName, Value = simulationId currently occupying it (null = available)
        private readonly ConcurrentDictionary<string, string?> podOccupancy;
        private readonly SimulationDispatcherOptions _options;
        private readonly ILogger<SimulationDispatcher> _logger;
        private readonly ILogger<GrpcErrorDetailLoggingInterceptor> _interceptorLogger;

        public SimulationDispatcher(IOptions<SimulationDispatcherOptions> options, ILogger<SimulationDispatcher> logger, ILogger<GrpcErrorDetailLoggingInterceptor> interceptorLogger)
        {
            _options = options.Value;
            podOccupancy = new ConcurrentDictionary<string, string?>(
                Enumerable.Range(0, _options.NrOfPods).Select(i => new KeyValuePair<string, string?>($"surimi-ecopath-{i}", null))
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
            var address = _options.EcopathUrl;    // for example: http://pod.surimi-ecopath.namespace.svc.cluster.local:8080  Or http://localhost:7890 when running on a dev machine
            address = address.Replace("namespace", _options.PodNamespace);  // this environment variable is set in the Deployment yaml to "user-rikkert", "project-surimi" etc
            string? pod;

            if (Guid.TryParse(simulationId, out var guid))
            {
                var existingEntry = podOccupancy.FirstOrDefault(kvp => kvp.Value == simulationId);
                if (existingEntry.Key != null)
                {
                    pod = existingEntry.Key;
                }
                else
                {
                    // so this is a new simulation
                    // Atomically claim the first available pod to avoid race conditions when multiple
                    // simulations start concurrently (TryUpdate only succeeds if value is still null).
                    pod = null;
                    foreach (var kvp in podOccupancy)
                    {
                        if (kvp.Value == null && podOccupancy.TryUpdate(kvp.Key, simulationId, null))
                        {
                            pod = kvp.Key;
                            break;
                        }
                    }
                    if (pod == null)
                        throw new Exception("No available pods");

                    // Check if the dns address can be resolved. If not, don't use this pod yet
                    var resolveAddress = address.Contains("pod")
                        ? address.Replace("pod", pod)
                        : address;
                    try
                    {
                        var uri = new Uri(resolveAddress);
                        var host = uri.Host;
                        var addresses = System.Net.Dns.GetHostAddresses(host);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        podOccupancy.TryUpdate(pod, null, simulationId);
                        throw new RpcException(
                            new Status(StatusCode.Unavailable, $"Could not resolve DNS for pod {pod} with address {resolveAddress}"),
                            new Metadata
                            {
                                { "pod", pod },
                                { "simulationId", simulationId }
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        podOccupancy.TryUpdate(pod, null, simulationId);
                        throw new RpcException(
                            new Status(StatusCode.Internal, $"Error resolving DNS for pod {pod}: {ex.Message}"),
                            new Metadata
                            {
                                { "pod", pod },
                                { "simulationId", simulationId }
                            }
                        );
                    }

                    _logger.LogInformation("Add Simulation:{SimulationId} with pod:{Pod} to podOccupancy", simulationId, pod);
                }
            }
            else
            {
                // the simulationId is not a valid Guid, so we don't reserve a pod for it. This allows us to use the dispatcher for non-simulation related calls without consuming pod resources.
                pod = podOccupancy.Keys.First();  // Just take the first pod
            }

            if (address.Contains("pod"))      // so only when not running on a Dev machine. Because then address = http://localhost:7890
            {
                address = address.Replace("pod", pod);                
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
            var entry = podOccupancy.FirstOrDefault(kvp => kvp.Value == simulationId);
            if (entry.Key != null)
            {
                podOccupancy.TryUpdate(entry.Key, null, simulationId);
                _logger.LogInformation("Simulation {SimulationId} released pod {Pod}", simulationId, entry.Key);
            }
            else
            {
                _logger.LogWarning("Simulation {SimulationId} No pod found for simulationId to release", simulationId);
            }
        }
    }
}