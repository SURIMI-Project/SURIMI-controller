using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class EnvironmentServiceClient(GrpcClientFactory clientFactory, ILogger<EnvironmentServiceClient> logger)
        : GrpcServiceClientBase<EnvironmentServiceClient>(logger), IEnvironmentServiceClient
    {
        private readonly EnvironmentService.EnvironmentServiceClient _environmentClient =
            clientFactory.CreateClient<EnvironmentService.EnvironmentServiceClient>("Environment");
        private readonly Dictionary<(string ExperimentId, DateTime Date), GetEnvironmentVariablesResponse> _cache = [];

        public AsyncUnaryCall<InitialiseExperimentResponse>? AddInitialise(List<Task<InitialiseExperimentResponse>> initialisationTasks, InitialiseExperimentRequest initialiseExperimentRequest, CancellationToken cancellationToken = default)
        {
            var initialiseExperimentResponse = _environmentClient.InitialiseExperimentAsync(initialiseExperimentRequest, cancellationToken: cancellationToken);
            initialisationTasks.Add(initialiseExperimentResponse.ResponseAsync);
            return initialiseExperimentResponse;
        }

        public async Task<CancelExperimentResponse> CancelExperimentAsync(CancelExperimentRequest cancelRequest, CancellationToken token)
        {
            return await _environmentClient.CancelExperimentAsync(cancelRequest, cancellationToken: token);
        }

        public async Task<FinaliseExperimentResponse> FinaliseExperimentAsync(FinaliseExperimentRequest finaliseExperimentRequest, CancellationToken cancellationToken = default)
        {
            return await _environmentClient.FinaliseExperimentAsync(finaliseExperimentRequest, cancellationToken: cancellationToken);
        }

        public async Task<ExperimentStepResponse> ExperimentStepAsync(ExperimentStepRequest experimentStepRequest, DateTime current, CancellationToken token)
        {
            return await _environmentClient.ExperimentStepAsync(experimentStepRequest, cancellationToken: token);
        }

        public async Task<GetEnvironmentVariablesResponse> GetEnvironmentVariables(GetEnvironmentVariablesRequest getEnvironmentVariablesRequest, DateTime current, CancellationToken token)
        {
            var cacheKey = (getEnvironmentVariablesRequest.ExperimentId, current.Date);

            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                Logger.LogInformation("{ExperimentId} Cache hit for Environment.GetEnvironmentVariables. {DateTime}", getEnvironmentVariablesRequest.ExperimentId, current);
                return cached;
            }

            LogStep(getEnvironmentVariablesRequest.ExperimentId, current);
            var response = await _environmentClient.GetEnvironmentVariablesAsync(getEnvironmentVariablesRequest, cancellationToken: token);
            _cache[cacheKey] = response;
            return response;
        }

    }
}
