using Grpc.Core;
using Grpc.Surimi;

namespace Ecopath.Services
{
    public class EcopathFisheryService : FisheryService.FisheryServiceBase
    {
        private readonly ILogger<EcopathEcologyService> _logger;

        public EcopathFisheryService(ILogger<EcopathEcologyService> logger)
        {
            _logger = logger;
        }

        public override Task<GetSalesSummaryResponse> GetSalesSummary(GetSalesSummaryRequest request, ServerCallContext context)
        {
            GrpcValidation.ArgumentNotNullOrEmpty(request.SimulationId);
            _logger.LogInformation($"Ecopath GetSalesSummary for {request.SimulationId}...");

            // Mock response
            var response = new GetSalesSummaryResponse();
            var salesSum = new SalesSummary()
            {
                MeasurementUnit = "tons",
                Currency = "EUR",
                MarketCode = "Barcelona"
            };
            salesSum.Sales.Add(new Sale
            {
                SpeciesCode = "PIL",
                Quantity = 500.0f,
                Value = 15000.0f
            });
            salesSum.Sales.Add(new Sale
            {
                SpeciesCode = "BOG",
                Quantity = 500.0f,
                Value = 100.0f
            });
            response.SalesSummaries.Add(salesSum);

            return Task.FromResult(response);
        }

        public override Task<GetCatchDispositionSummaryResponse> GetCatchDispositionSummary(GetCatchDispositionSummaryRequest request, ServerCallContext context)
        {
            GrpcValidation.ArgumentNotNullOrEmpty(request.SimulationId);
            _logger.LogInformation($"Ecopath GetCatchDispositionSummary for {request.SimulationId}...");

            var response = new GetCatchDispositionSummaryResponse
            {
                MeasurementUnit = "kg"
            };
            var grid = new DispositionGrid
            {
                SpeciesCode = "PIL",
                GearCode = "Trawl"
            };
            grid.DispositionCells.Add(new DispositionCell
            {
                Longitude = 1.6877561f,
                Latitude = 40.901618f,
                GrossCatch = 1000.0f,
                LiveDiscards = 200.0f,
                DeadDiscards = 100.0f,
            });
            response.DispositionGrids.Add(grid);

            return Task.FromResult(response);
        }
    }
}
