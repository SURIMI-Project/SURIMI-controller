using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class ValueChainServiceClient : IValueChainServiceClient
    {
        private readonly ILogger<ValueChainServiceClient> _logger;
        private readonly ValueChainService.ValueChainServiceClient _valueChainClient;
        private readonly bool _includeValueChain = Environment.GetEnvironmentVariable("EXCLUDE_VALUECHAIN")?.ToLower() != "true";

        public ValueChainServiceClient(GrpcClientFactory clientFactory, ILogger<ValueChainServiceClient> logger)
        {
            _valueChainClient = clientFactory.CreateClient<ValueChainService.ValueChainServiceClient>("ValueChain");

            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseExperimentResponse>? AddInitialise(List<Task<InitialiseExperimentResponse>> initializationTasks, InitialiseExperimentRequest initialiseExperimentRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_includeValueChain)
            {
                var InitialiseExperimentResponse = _valueChainClient.InitialiseExperimentAsync(initialiseExperimentRequest, cancellationToken: cancellationToken);
                initializationTasks.Add(InitialiseExperimentResponse.ResponseAsync);
                return InitialiseExperimentResponse;
            }
            return null;
        }

        public async Task<CancelExperimentResponse> CancelExperimentAsync(CancelExperimentRequest cancelRequest, CancellationToken token)
        {
            if (_includeValueChain)
            {
                return await _valueChainClient.CancelExperimentAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelExperimentResponse() { ExperimentId = cancelRequest.ExperimentId };
        }

        public async Task<ExperimentStepResponse> ExperimentStepAsync(ExperimentStepRequest experimentStepRequest, DateTime current, CancellationToken token)
        {
            if (_includeValueChain)
            {
                LogStep(experimentStepRequest.ExperimentId, current, "ExperimentStep");
                return await _valueChainClient.ExperimentStepAsync(experimentStepRequest, cancellationToken: token);
            }
            return new ExperimentStepResponse() { ExperimentId = experimentStepRequest.ExperimentId };
        }

        public async Task<FinaliseExperimentResponse> FinaliseExperimentAsync(FinaliseExperimentRequest finaliseExperimentRequest, CancellationToken cancellationToken = default)
        {
            if (_includeValueChain)
            {
                return await _valueChainClient.FinaliseExperimentAsync(finaliseExperimentRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseExperimentResponse() { ExperimentId = finaliseExperimentRequest.ExperimentId };
        }

        public async Task<UpdateSalesStatisticsResponse> UpdateSalesStatisticsAsync(UpdateSalesStatisticsRequest updateSalesStatisticsRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeValueChain)
            {
                LogStep(updateSalesStatisticsRequest.ExperimentId, current, "UpdateSalesStatistics");
                return await _valueChainClient.UpdateSalesStatisticsAsync(updateSalesStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSalesStatisticsResponse() { ExperimentId = updateSalesStatisticsRequest.ExperimentId };
        }

        private void LogStep(string ExperimentId, DateTime current, string step)
        {
            _logger.LogInformation("{ExperimentId} Processing step ValueChain.{Step}. {DateTime}", ExperimentId, step, current);
        }
    }
}
