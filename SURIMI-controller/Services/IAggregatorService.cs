using Grpc.Surimi;
using SURIMI_controller.Models;

namespace SURIMI_controller.Services
{
    public interface IAggregatorService
    {
        BiomassStatisticsSummary AddAggregateBiomass(List<BiomassSummary?> list);
        CatchDispositionStatisticsSummary AddAggregateCatchDisposition(List<CatchDispositionSummary?> list);
        FishingActivityStatisticsSummary AddAggregateFishingActivity(List<FishingActivitySummary?> list);
        SalesStatisticsSummary AddAggregateSales(List<SalesSummary?> list);
        SpeciesPriceStatisticsSummary AddAggregateSpeciesPrice(List<SpeciesPriceSummary?> list);
    }
}