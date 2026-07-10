using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SURIMI_controller.Services
{
    public class CmsyServiceClient(GrpcClientFactory clientFactory, ILogger<CmsyServiceClient> logger)
        : OptionalGrpcServiceClientBase<CmsyServiceClient>(logger, "EXCLUDE_CMSY"), ICmsyServiceClient
    {
        private readonly StockAssessmentService.StockAssessmentServiceClient _cmsyClient =
            clientFactory.CreateClient<StockAssessmentService.StockAssessmentServiceClient>("Cmsy");

        public AsyncUnaryCall<InitialiseExperimentResponse>? AddInitialise(List<Task<InitialiseExperimentResponse>> initialisationTasks, InitialiseExperimentRequest initialiseExperimentRequest, CancellationToken cancellationToken = default)
        {
            if (IsEnabled)
            {
                var callOptions = new CallOptions(
                    deadline: DateTime.UtcNow.AddMinutes(10),   // adjust to expected CMSY init time
                    cancellationToken: cancellationToken);
                var response = _cmsyClient.InitialiseExperimentAsync(initialiseExperimentRequest, callOptions);
                initialisationTasks.Add(response.ResponseAsync);
                return response;
            }
            return null;
        }

        public async Task<CancelExperimentResponse> CancelExperimentAsync(CancelExperimentRequest cancelRequest, CancellationToken token)
        {
            if (IsEnabled)
            {
                return await _cmsyClient.CancelExperimentAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelExperimentResponse() { ExperimentId = cancelRequest.ExperimentId };
        }

        public async Task<ExperimentStepResponse> ExperimentStepAsync(ExperimentStepRequest experimentStepRequest, DateTime current, CancellationToken token)
        {
            if (IsEnabled)
            {
                return await _cmsyClient.ExperimentStepAsync(experimentStepRequest, cancellationToken: token);
            }
            return new ExperimentStepResponse() { ExperimentId = experimentStepRequest.ExperimentId };
        }

        public async Task<FinaliseExperimentResponse> FinaliseExperimentAsync(FinaliseExperimentRequest finaliseExperimentRequest, CancellationToken cancellationToken = default)
        {
            if (IsEnabled)
            {
                return await _cmsyClient.FinaliseExperimentAsync(finaliseExperimentRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseExperimentResponse() { ExperimentId = finaliseExperimentRequest.ExperimentId };
        }

        public async Task<GetStockAssessmentResponse> GetStockAssessmentAsync(GetStockAssessmentRequest getStockAssessmentRequest, CancellationToken cancellationToken = default)
        {
            if (IsEnabled)
            {
                return await _cmsyClient.GetStockAssessmentAsync(getStockAssessmentRequest, cancellationToken: cancellationToken);
            }
            return new GetStockAssessmentResponse() { ExperimentId = getStockAssessmentRequest.ExperimentId };
        }

        public async Task<UpdateBiomassStatisticsResponse> UpdateBiomassStatisticsAsync(UpdateBiomassStatisticsRequest updateBiomassStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(experimentId, current);
                return await _cmsyClient.UpdateBiomassStatisticsAsync(updateBiomassStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateBiomassStatisticsResponse() { ExperimentId = experimentId };
        }

        public async Task<UpdateCatchDispositionStatisticsResponse> UpdateCatchDispositionStatisticsAsync(UpdateCatchDispositionStatisticsRequest updateCatchDispositionStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if (IsEnabled)
            {
                LogStep(experimentId, current);
                return await _cmsyClient.UpdateCatchDispositionStatisticsAsync(updateCatchDispositionStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateCatchDispositionStatisticsResponse() { ExperimentId = experimentId };
        }

    }
}
