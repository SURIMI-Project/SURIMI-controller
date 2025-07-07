using Grpc.Core;
using SurimiController.Services;
using System.Collections.Concurrent;

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

    public SimulationDispatcher(ILogger<SimulationDispatcher> logger)
    {
        podAvailability = new ConcurrentDictionary<string, bool>(
            podNames.Select(p => new KeyValuePair<string, bool>(p, true))
        );
        _logger = logger;
    }

    public AsyncUnaryCall<TResponse> DispatchAsync<TClient, TRequest, TResponse>(
        TRequest request,
        string simulationId,
        Func<TClient, TRequest, AsyncUnaryCall<TResponse>> grpcMethod)
        where TClient : ClientBase<TClient>
    {
        var address = Environment.GetEnvironmentVariable("ECOPATH_URL");    // for example: http://surimi-ecopath-0.surimi-ecopath.user-rikkert.svc.cluster.local:8080
        string pod;

        if (!simulationToPodMap.TryGetValue(simulationId, out pod))
        {
            // so this is a new simulation
            pod = podAvailability.FirstOrDefault(p => p.Value).Key; // Find the first available pod
            if (pod == null)
                throw new Exception("No available pods");

            if (address!.Contains("surimi-ecopath-0"))      // so only when not running on a Dev machine. Because then address = http://localhost:7890
            {
                address = address!.Replace("surimi-ecopath-0", pod);
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

            simulationToPodMap[simulationId] = pod;
            podAvailability[pod] = false;
        }

        if (address!.Contains("surimi-ecopath-0"))      // so only when not running on a Dev machine. Because then address = http://localhost:7890
        {
            address = address!.Replace("surimi-ecopath-0", pod);
        }

        _logger.LogInformation("{SimulationId} Using address {Address} for pod {Pod} and simulationId", simulationId, address, pod);
        try
        {
            var client = DynamicGrpcClientFactory.CreateClient<TClient>(address);
            var response = grpcMethod(client, request);

            //simulationToPodMap.TryRemove(simulationId, out _);
            //podAvailability[pod] = true;

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while dispatching request to pod {Pod} for simulationId {SimulationId}", pod, simulationId);
            throw;
        }
    }

    public void ReleasePodFromSimulation(string simulationId)
    {
        if (simulationToPodMap.TryRemove(simulationId, out var pod))
        {
            podAvailability[pod] = true;
            _logger.LogInformation("{SimulationId} Released pod {Pod} from simulationId", simulationId, pod);
        }
        else
        {
            _logger.LogWarning("{SimulationId} No pod found for simulationId to release", simulationId);
        }
    }
}
