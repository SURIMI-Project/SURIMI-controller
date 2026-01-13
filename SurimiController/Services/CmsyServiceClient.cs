using Grpc.Net.ClientFactory;
using Grpc.Surimi;

namespace SurimiController.Services
{
    public interface ICmsyServiceClient : IWorkflowService, IEcologyService, IFisheryService
    {
    }

    public class CmsyServiceClient : ICmsyServiceClient
    {
        private readonly ILogger<CmsyServiceClient> _logger;
        private readonly WorkflowService.WorkflowServiceClient _cmsyWorkflowClient;
        private readonly EcologyService.EcologyServiceClient _cmsyEcologyClient;
        private readonly FisheryService.FisheryServiceClient _cmsyFisheryClient;
        private readonly bool _includeCmsy = Environment.GetEnvironmentVariable("EXCLUDE_CMSY")?.ToLower() != "true";

        public CmsyServiceClient(GrpcClientFactory clientFactory, ILogger<CmsyServiceClient> logger)
        {
            _cmsyWorkflowClient = clientFactory.CreateClient<WorkflowService.WorkflowServiceClient>("CmsyWorkflow");
            _cmsyEcologyClient = clientFactory.CreateClient<EcologyService.EcologyServiceClient>("CmsyEcology");
            _cmsyFisheryClient = clientFactory.CreateClient<FisheryService.FisheryServiceClient>("CmsyFishery");

            _logger = logger;
        }

        public void AddInitialise(List<Task<InitialiseResponse>> initializationTasks, InitialiseRequest initialiseRequest, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_includeCmsy)
            {
                initializationTasks.Add(_cmsyWorkflowClient.InitialiseAsync(initialiseRequest, cancellationToken: cancellationToken).ResponseAsync);
            }
        }

        public async Task<CancelResponse> CancelAsync(CancelRequest cancelRequest, CancellationToken token)
        {
            if (_includeCmsy)
            {
                return await _cmsyWorkflowClient.CancelAsync(cancelRequest, cancellationToken: token);
            }
            return new CancelResponse();
        }

        public async Task<FinaliseResponse> FinaliseAsync(FinaliseRequest finaliseRequest, CancellationToken cancellationToken = default)
        {
            if (_includeCmsy)
            {
                return await _cmsyWorkflowClient.FinaliseAsync(finaliseRequest, cancellationToken: cancellationToken);
            }
            return new FinaliseResponse();
        }

        public Task<GetCatchDispositionResponse> GetCatchDispositionAsync(GetCatchDispositionRequest getCatchDispositionRequest, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<SimulateStepResponse> SimulateStepAsync(SimulateStepRequest simulationStepRequest, CancellationToken cancellationToken)
        {
            if (_includeCmsy)
            {
                LogStep(simulationStepRequest.SimulationId, "CMSY.SimulateStep");
                return await _cmsyWorkflowClient.SimulateStepAsync(simulationStepRequest, cancellationToken: cancellationToken);
            }
            return new SimulateStepResponse();
        }

        public async Task<UpdateBiomassResponse> UpdateBiomassAsync(UpdateBiomassRequest updateBiomassRequest, CancellationToken cancellationToken)
        {
            if (_includeCmsy)
            {
                LogStep(updateBiomassRequest.SimulationId, "CMSY++.UpdateBiomass (total)");
                return await _cmsyEcologyClient.UpdateBiomassAsync(updateBiomassRequest, cancellationToken: cancellationToken);
            }
            return new UpdateBiomassResponse();
        }

        public async Task<UpdateCatchDispositionResponse> UpdateCatchDispositionAsync(UpdateCatchDispositionRequest updateCatchDispositionRequest, CancellationToken cancellationToken)
        {
            if (_includeCmsy)
            {
                LogStep(updateCatchDispositionRequest.SimulationId, "CMSY UpdateCatchDisposition (total)");
                return await _cmsyFisheryClient.UpdateCatchDispositionAsync(updateCatchDispositionRequest, cancellationToken: cancellationToken);
            }
            return new UpdateCatchDispositionResponse();
        }

        private void LogStep(string simulationId, string step)
        {
            _logger.LogInformation("{SimulationId} Processing step {Step}.", simulationId, step);
        }
    }
}
