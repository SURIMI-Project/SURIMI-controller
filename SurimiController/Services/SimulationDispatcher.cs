using Grpc.Core;
using Grpc.Surimi;
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
        string pod;

        if (!simulationToPodMap.TryGetValue(simulationId, out pod))
        {
            pod = podAvailability.FirstOrDefault(p => p.Value).Key;
            if (pod == null)
                throw new Exception("No available pods");

            simulationToPodMap[simulationId] = pod;
            podAvailability[pod] = false;
        }

        var address = Environment.GetEnvironmentVariable("ECOPATH_URL");    // for example: http://surimi-ecopath-0.surimi-ecopath.user-rikkert.svc.cluster.local:8080
        if (address!.Contains("surimi-ecopath-0"))
        {
            address = address!.Replace("surimi-ecopath-0", pod);
        }

        _logger.LogInformation("Using address {Address} for pod {Pod} and simulationId {SimulationId}", address, pod, simulationId);
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
            _logger.LogInformation("Released pod {Pod} from simulationId {SimulationId}", pod, simulationId);
        }
        else
        {
            _logger.LogWarning("No pod found for simulationId {SimulationId} to release", simulationId);
        }
    }
}
