using Grpc.Core;

namespace SURIMI_controller.Services
{
    public interface ISimulationDispatcher
    {
        AsyncUnaryCall<TResponse> DispatchAsync<TClient, TRequest, TResponse>(
            TRequest request,
            string simulationId,
            Func<TClient, TRequest, AsyncUnaryCall<TResponse>> grpcMethod)
            where TClient : ClientBase<TClient>;

        Task<TResponse> DispatchWithRetryAsync<TClient, TRequest, TResponse>(
            TRequest request,
            string simulationId,
            Func<TClient, TRequest, AsyncUnaryCall<TResponse>> grpcMethod,
            int maxRetries = 3,
            int retryDelayMs = 500)
            where TClient : ClientBase<TClient>;

        void ReleasePodFromSimulation(string simulationId);
    }
}
