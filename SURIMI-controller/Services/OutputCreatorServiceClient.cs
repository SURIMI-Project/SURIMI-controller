using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class OutputCreatorServiceClient : IOutputCreatorServiceClient
    {
        private readonly ILogger<OutputCreatorServiceClient> _logger;
        private readonly OutputCreatorService.OutputCreatorServiceClient _outputCreatorServiceClient;
        private readonly bool _includeOutputCreator = Environment.GetEnvironmentVariable("EXCLUDE_OUTPUTCREATOR")?.ToLower() != "true";

        public OutputCreatorServiceClient(GrpcClientFactory clientFactory, ILogger<OutputCreatorServiceClient> logger)
        {
            _outputCreatorServiceClient = clientFactory.CreateClient<OutputCreatorService.OutputCreatorServiceClient>("OutputCreator");
            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseExperimentResponse>? AddInitialise(List<Task<InitialiseExperimentResponse>> initializationTasks, InitialiseExperimentRequest initialiseExperimentRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_includeOutputCreator)
            {
                var InitialiseExperimentResponse = _outputCreatorServiceClient.InitialiseExperimentAsync(initialiseExperimentRequest, cancellationToken: cancellationToken);
                initializationTasks.Add(InitialiseExperimentResponse.ResponseAsync);
                return InitialiseExperimentResponse;
            }
            return null;
        }

        public async Task<CancelExperimentResponse> CancelExperimentAsync(CancelExperimentRequest cancelRequest, CancellationToken token)
        {
            if (_includeOutputCreator)
            {
                return await _outputCreatorServiceClient.CancelExperimentAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelExperimentResponse() { ExperimentId = cancelRequest.ExperimentId };
        }

        public async Task<ExperimentStepResponse> ExperimentStepAsync(ExperimentStepRequest experimentStepRequest, DateTime current, CancellationToken token)
        {
            if (_includeOutputCreator)
            {
                return await _outputCreatorServiceClient.ExperimentStepAsync(experimentStepRequest, cancellationToken: token);
            }
            return new ExperimentStepResponse() { ExperimentId = experimentStepRequest.ExperimentId };
        }

        public async Task<FinaliseExperimentResponse> FinaliseExperimentAsync(FinaliseExperimentRequest finaliseExperimentRequest, CancellationToken cancellationToken = default)
        {
            if (_includeOutputCreator)
            {
                return await _outputCreatorServiceClient.FinaliseExperimentAsync(finaliseExperimentRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseExperimentResponse() { ExperimentId = finaliseExperimentRequest.ExperimentId };
        }

        public async Task<UpdateBiomassStatisticsResponse> UpdateBiomassStatisticsAsync(UpdateBiomassStatisticsRequest updateBiomassStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeOutputCreator)
            {
                LogStep(updateBiomassStatisticsRequest.ExperimentId, current, "UpdateBiomassStatistics");
                return await _outputCreatorServiceClient.UpdateBiomassStatisticsAsync(updateBiomassStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateBiomassStatisticsResponse() { ExperimentId = updateBiomassStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateCatchDispositionStatisticsResponse> UpdateCatchDispositionStatisticsAsync(UpdateCatchDispositionStatisticsRequest updateCatchDispositionStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeOutputCreator)
            {
                LogStep(updateCatchDispositionStatisticsRequest.ExperimentId, current, "UpdateCatchDispositionStatistics");
                return await _outputCreatorServiceClient.UpdateCatchDispositionStatisticsAsync(updateCatchDispositionStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateCatchDispositionStatisticsResponse() { ExperimentId = updateCatchDispositionStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateFishingActivityStatisticsResponse> UpdateFishingActivityStatisticsAsync(UpdateFishingActivityStatisticsRequest updateFishingActivityStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeOutputCreator)
            {
                LogStep(updateFishingActivityStatisticsRequest.ExperimentId, current, "UpdateFishingActivityStatistics");
                return await _outputCreatorServiceClient.UpdateFishingActivityStatisticsAsync(updateFishingActivityStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateFishingActivityStatisticsResponse() { ExperimentId = updateFishingActivityStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateSalesStatisticsResponse> UpdateSalesStatisticsAsync(UpdateSalesStatisticsRequest updateSalesStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeOutputCreator)
            {
                LogStep(updateSalesStatisticsRequest.ExperimentId, current, "UpdateSalesStatistics");
                return await _outputCreatorServiceClient.UpdateSalesStatisticsAsync(updateSalesStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSalesStatisticsResponse() { ExperimentId = updateSalesStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateSpeciesPriceStatisticsResponse> UpdateSpeciesPriceStatisticsAsync(UpdateSpeciesPriceStatisticsRequest updateSpeciesPriceStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (_includeOutputCreator)
            {
                LogStep(updateSpeciesPriceStatisticsRequest.ExperimentId, current, "UpdateSpeciesPriceStatistics");
                return await _outputCreatorServiceClient.UpdateSpeciesPriceStatisticsAsync(updateSpeciesPriceStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSpeciesPriceStatisticsResponse() { ExperimentId = updateSpeciesPriceStatisticsRequest.ExperimentId };
        }

        private void LogStep(string experimentId, DateTime current, string step)
        {
            _logger.LogInformation("{ExperimentId} Processing step OutputCreator.{Step}. {DateTime}", experimentId, step, current);
        }
    }
}
