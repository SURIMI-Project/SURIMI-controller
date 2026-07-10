using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class ValueChainServiceClient(GrpcClientFactory clientFactory, ILogger<ValueChainServiceClient> logger)
        : OptionalGrpcServiceClientBase<ValueChainServiceClient>(logger, "EXCLUDE_VALUECHAIN"), IValueChainServiceClient
    {
        private readonly ValueChainService.ValueChainServiceClient _valueChainClient =
            clientFactory.CreateClient<ValueChainService.ValueChainServiceClient>("ValueChain");

        public AsyncUnaryCall<InitialiseExperimentResponse>? AddInitialise(List<Task<InitialiseExperimentResponse>> initialisationTasks, InitialiseExperimentRequest initialiseExperimentRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (IsEnabled)
            {
                var InitialiseExperimentResponse = _valueChainClient.InitialiseExperimentAsync(initialiseExperimentRequest, cancellationToken: cancellationToken);
                initialisationTasks.Add(InitialiseExperimentResponse.ResponseAsync);
                return InitialiseExperimentResponse;
            }
            return null;
        }

        public async Task<CancelExperimentResponse> CancelExperimentAsync(CancelExperimentRequest cancelRequest, CancellationToken token)
        {
            if (IsEnabled)
            {
                return await _valueChainClient.CancelExperimentAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelExperimentResponse() { ExperimentId = cancelRequest.ExperimentId };
        }

        public async Task<ExperimentStepResponse> ExperimentStepAsync(ExperimentStepRequest experimentStepRequest, DateTime current, CancellationToken token)
        {
            if (IsEnabled)
            {
                LogStep(experimentStepRequest.ExperimentId, current);
                return await _valueChainClient.ExperimentStepAsync(experimentStepRequest, cancellationToken: token);
            }
            return new ExperimentStepResponse() { ExperimentId = experimentStepRequest.ExperimentId };
        }

        public async Task<FinaliseExperimentResponse> FinaliseExperimentAsync(FinaliseExperimentRequest finaliseExperimentRequest, CancellationToken cancellationToken = default)
        {
            if (IsEnabled)
            {
                return await _valueChainClient.FinaliseExperimentAsync(finaliseExperimentRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseExperimentResponse() { ExperimentId = finaliseExperimentRequest.ExperimentId };
        }

        public async Task<UpdateSalesStatisticsResponse> UpdateSalesStatisticsAsync(UpdateSalesStatisticsRequest updateSalesStatisticsRequest, DateTime current, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(updateSalesStatisticsRequest.ExperimentId, current);
                return await _valueChainClient.UpdateSalesStatisticsAsync(updateSalesStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSalesStatisticsResponse() { ExperimentId = updateSalesStatisticsRequest.ExperimentId };
        }
    }
}
