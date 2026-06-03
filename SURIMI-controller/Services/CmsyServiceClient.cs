using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using SURIMI_controller.Models;

namespace SURIMI_controller.Services
{
    public class CmsyServiceClient : ICmsyServiceClient
    {
        private readonly ILogger<CmsyServiceClient> _logger;
        private readonly StockAssessmentService.StockAssessmentServiceClient _cmsyClient;
        private readonly bool _includeCmsy = Environment.GetEnvironmentVariable("EXCLUDE_CMSY")?.ToLower() != "true";

        public CmsyServiceClient(GrpcClientFactory clientFactory, ILogger<CmsyServiceClient> logger)
        {
            _cmsyClient = clientFactory.CreateClient<StockAssessmentService.StockAssessmentServiceClient>("Cmsy");

            _logger = logger;
        }

        public AsyncUnaryCall<InitialiseExperimentResponse>? AddInitialise(List<Task<InitialiseExperimentResponse>> initializationTasks, InitialiseExperimentRequest initialiseExperimentRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_includeCmsy)
            {
                var InitialiseExperimentResponse = _cmsyClient.InitialiseExperimentAsync(initialiseExperimentRequest, cancellationToken: cancellationToken);
                initializationTasks.Add(InitialiseExperimentResponse.ResponseAsync);
                return InitialiseExperimentResponse;
            }
            return null;
        }

        public async Task<CancelExperimentResponse> CancelExperimentAsync(CancelExperimentRequest cancelRequest, CancellationToken token)
        {
            if (_includeCmsy)
            {
                return await _cmsyClient.CancelExperimentAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelExperimentResponse() { ExperimentId = cancelRequest.ExperimentId };
        }

        public async Task<ExperimentStepResponse> ExperimentStepAsync(ExperimentStepRequest experimentStepRequest, DateTime current, CancellationToken token)
        {
            if (_includeCmsy)
            {
                return await _cmsyClient.ExperimentStepAsync(experimentStepRequest, cancellationToken: token);
            }
            return new ExperimentStepResponse() { ExperimentId = experimentStepRequest.ExperimentId };
        }

        public async Task<FinaliseExperimentResponse> FinaliseExperimentAsync(FinaliseExperimentRequest finaliseExperimentRequest, CancellationToken cancellationToken = default)
        {
            if (_includeCmsy)
            {
                return await _cmsyClient.FinaliseExperimentAsync(finaliseExperimentRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseExperimentResponse() { ExperimentId = finaliseExperimentRequest.ExperimentId };
        }

        public async Task<UpdateBiomassStatisticsResponse> UpdateBiomassStatistics(UpdateBiomassStatisticsRequest updateBiomassStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if(_includeCmsy)
            {
                LogStep(experimentId, current, "UpdateBiomassStatistics");
                return await _cmsyClient.UpdateBiomassStatisticsAsync(updateBiomassStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateBiomassStatisticsResponse() { ExperimentId = experimentId };
        }

        public async Task<UpdateCatchDispositionStatisticsResponse> UpdateCatchDispositionStatistics(UpdateCatchDispositionStatisticsRequest updateCatchDispositionStatisticsRequest, string experimentId, DateTime current, CancellationToken cancellationToken)
        {
            if(_includeCmsy)
            {
                LogStep(experimentId, current, "UpdateCatchDispositionStatistics");
                return await _cmsyClient.UpdateCatchDispositionStatisticsAsync(updateCatchDispositionStatisticsRequest, cancellationToken: cancellationToken);
            }
            return new UpdateCatchDispositionStatisticsResponse() { ExperimentId = experimentId };
        }

        private void LogStep(string experimentId, DateTime current, string step)
        {
            _logger.LogInformation("{ExperimentId} Processing step CMSY.{Step}. {DateTime}", experimentId, step, current);
        }
    }
}
