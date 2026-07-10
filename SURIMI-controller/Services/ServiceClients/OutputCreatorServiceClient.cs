using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class OutputCreatorServiceClient(GrpcClientFactory clientFactory, ILogger<OutputCreatorServiceClient> logger)
        : OptionalGrpcServiceClientBase<OutputCreatorServiceClient>(logger, "EXCLUDE_OUTPUTCREATOR"), IOutputCreatorServiceClient
    {
        private readonly OutputCreatorService.OutputCreatorServiceClient _outputCreatorServiceClient =
            clientFactory.CreateClient<OutputCreatorService.OutputCreatorServiceClient>("OutputCreator");

        public AsyncUnaryCall<InitialiseExperimentResponse>? AddInitialise(List<Task<InitialiseExperimentResponse>> initialisationTasks, InitialiseExperimentRequest initialiseExperimentRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (IsEnabled)
            {
                LogStep(initialiseExperimentRequest.ExperimentId, initialiseExperimentRequest.Simulation.StartDateTime.ToDateTime());
               var InitialiseExperimentResponse = _outputCreatorServiceClient.InitialiseExperimentAsync(initialiseExperimentRequest, cancellationToken: cancellationToken);
                initialisationTasks.Add(InitialiseExperimentResponse.ResponseAsync);
                return InitialiseExperimentResponse;
            }
            return null;
        }

        public async Task<CancelExperimentResponse> CancelExperimentAsync(CancelExperimentRequest cancelRequest, CancellationToken token)
        {
            if (IsEnabled)
            {
                LogStep(cancelRequest.ExperimentId, DateTime.UtcNow);
                return await _outputCreatorServiceClient.CancelExperimentAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelExperimentResponse() { ExperimentId = cancelRequest.ExperimentId };
        }

        public async Task<ExperimentStepResponse> ExperimentStepAsync(ExperimentStepRequest experimentStepRequest, DateTime current, CancellationToken token)
        {
            if (IsEnabled)
            {
                LogStep(experimentStepRequest.ExperimentId, current);
                return await _outputCreatorServiceClient.ExperimentStepAsync(experimentStepRequest, cancellationToken: token);
            }
            return new ExperimentStepResponse() { ExperimentId = experimentStepRequest.ExperimentId };
        }

        public async Task<FinaliseExperimentResponse> FinaliseExperimentAsync(FinaliseExperimentRequest finaliseExperimentRequest, CancellationToken cancellationToken = default)
        {
            if (IsEnabled)
            {
                LogStep(finaliseExperimentRequest.ExperimentId, DateTime.UtcNow);
                return await _outputCreatorServiceClient.FinaliseExperimentAsync(finaliseExperimentRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseExperimentResponse() { ExperimentId = finaliseExperimentRequest.ExperimentId };
        }

        public async Task<UpdateBiomassStatisticsResponse> UpdateBiomassStatisticsAsync(UpdateBiomassStatisticsRequest updateBiomassStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(updateBiomassStatisticsRequest.ExperimentId, current);
                return await _outputCreatorServiceClient.UpdateBiomassStatisticsAsync(updateBiomassStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateBiomassStatisticsResponse() { ExperimentId = updateBiomassStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateCatchDispositionStatisticsResponse> UpdateCatchDispositionStatisticsAsync(UpdateCatchDispositionStatisticsRequest updateCatchDispositionStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(updateCatchDispositionStatisticsRequest.ExperimentId, current);
                return await _outputCreatorServiceClient.UpdateCatchDispositionStatisticsAsync(updateCatchDispositionStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateCatchDispositionStatisticsResponse() { ExperimentId = updateCatchDispositionStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateFishingActivityStatisticsResponse> UpdateFishingActivityStatisticsAsync(UpdateFishingActivityStatisticsRequest updateFishingActivityStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(updateFishingActivityStatisticsRequest.ExperimentId, current);
                return await _outputCreatorServiceClient.UpdateFishingActivityStatisticsAsync(updateFishingActivityStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateFishingActivityStatisticsResponse() { ExperimentId = updateFishingActivityStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateSalesStatisticsResponse> UpdateSalesStatisticsAsync(UpdateSalesStatisticsRequest updateSalesStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(updateSalesStatisticsRequest.ExperimentId, current);
                return await _outputCreatorServiceClient.UpdateSalesStatisticsAsync(updateSalesStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSalesStatisticsResponse() { ExperimentId = updateSalesStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateSpeciesPriceStatisticsResponse> UpdateSpeciesPriceStatisticsAsync(UpdateSpeciesPriceStatisticsRequest updateSpeciesPriceStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(updateSpeciesPriceStatisticsRequest.ExperimentId, current);
                return await _outputCreatorServiceClient.UpdateSpeciesPriceStatisticsAsync(updateSpeciesPriceStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateSpeciesPriceStatisticsResponse() { ExperimentId = updateSpeciesPriceStatisticsRequest.ExperimentId };
        }

        public async Task<UpdateStockAssessmentResponse> UpdateStockAssessmentAsync(UpdateStockAssessmentRequest updateStockAssessmentRequest, string experimentId, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(updateStockAssessmentRequest.ExperimentId, DateTime.UtcNow);
                return await _outputCreatorServiceClient.UpdateStockAssessmentAsync(updateStockAssessmentRequest, cancellationToken: cancellationToken);
            }
            return new UpdateStockAssessmentResponse() { ExperimentId = updateStockAssessmentRequest.ExperimentId };
        }
    }
}
