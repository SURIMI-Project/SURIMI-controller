using Grpc.Surimi;
using MathNet.Numerics.Statistics;

namespace SURIMI_controller.Services
{
    public class AggregatorService : IAggregatorService
    {
        /// <summary>
        /// This method takes a list of BiomassSummary objects (which may contain nulls) of the same time period, and aggregates them into a single BiomassStatisticsSummary.
        /// </summary>
        /// <param name="biomassSummaries">The list of BiomassSummary objects to aggregate.</param>
        /// <returns>A BiomassStatisticsSummary containing the aggregated statistics.</returns>
        public BiomassStatisticsSummary AddAggregateBiomass(List<BiomassSummary?> biomassSummaries)
        {
            var result = new BiomassStatisticsSummary();

            var validSummaries = biomassSummaries?.Where(s => s != null).Select(s => s!).ToList();
            if (validSummaries == null || validSummaries.Count == 0)
                return result;

            var allSpeciesCodes = validSummaries
                .SelectMany(s => s.BiomassGrids)
                .Where(g => g.Species?.SpeciesCode != null)
                .Select(g => g.Species!.SpeciesCode)
                .Distinct()
                .ToList();

            foreach (var speciesCode in allSpeciesCodes)
            {
                var allCellLocations = validSummaries
                    .SelectMany(s => s.BiomassGrids
                        .Where(g => g.Species?.SpeciesCode == speciesCode)
                        .SelectMany(g => g.BiomassCells))
                    .Select(c => (c.Latitude, c.Longitude))
                    .Distinct()
                    .ToList();

                if (allCellLocations.Count == 0)
                    continue;

                var speciesObj = validSummaries
                    .SelectMany(s => s.BiomassGrids)
                    .First(g => g.Species?.SpeciesCode == speciesCode)
                    .Species;

                var gridStats = new BiomassGridStatistics { Species = speciesObj };

                foreach (var (lat, lon) in allCellLocations)
                {
                    var values = validSummaries
                        .Select(s => s.BiomassGrids
                            .Where(g => g.Species?.SpeciesCode == speciesCode)
                            .SelectMany(g => g.BiomassCells)
                            .FirstOrDefault(c => c.Latitude == lat && c.Longitude == lon))
                        .Select(c => c?.Biomass ?? 0.0)
                        .OrderBy(v => v)
                        .ToArray();

                    gridStats.BiomassCellsStatistics.Add(new BiomassCellStatistics
                    {
                        Latitude = lat,
                        Longitude = lon,
                        Biomass = new DoubleStatistics
                        {
                            Mean = Statistics.Mean(values),
                            P05 = Statistics.Percentile(values, 5),
                            P95 = Statistics.Percentile(values, 95)
                        }
                    });
                }

                result.BiomassGridsStatistics.Add(gridStats);
            }

            return result;
        }

        public CatchDispositionStatisticsSummary AddAggregateCatchDisposition(List<CatchDispositionSummary?> catchDispositionSummaries)
        {
            var result = new CatchDispositionStatisticsSummary();

            var validSummaries = catchDispositionSummaries?.Where(s => s != null).Select(s => s!).ToList();
            if (validSummaries == null || validSummaries.Count == 0)
                return result;

            var allGridKeys = validSummaries
                .SelectMany(s => s.DispositionGrids)
                .Where(g => g.Species != null)
                .DistinctBy(g => (g.Species!.SpeciesCode, g.FleetSegment?.GearCode, g.FleetSegment?.VesselLengthClass, g.FleetSegment?.Scale, g.FleetSegment?.CountryCode, g.FleetSegment?.Model))
                .ToList();

            foreach (var gridKey in allGridKeys)
            {
                var matchingGrids = (DispositionGrid g) =>
                    g.Species?.SpeciesCode == gridKey.Species?.SpeciesCode
                    && g.FleetSegment?.GearCode == gridKey.FleetSegment?.GearCode
                    && g.FleetSegment?.VesselLengthClass == gridKey.FleetSegment?.VesselLengthClass
                    && g.FleetSegment?.Scale == gridKey.FleetSegment?.Scale
                    && g.FleetSegment?.CountryCode == gridKey.FleetSegment?.CountryCode
                    && g.FleetSegment?.Model == gridKey.FleetSegment?.Model;

                var allCellLocations = validSummaries
                    .SelectMany(s => s.DispositionGrids.Where(matchingGrids).SelectMany(g => g.DispositionCells))
                    .Select(c => (c.Latitude, c.Longitude))
                    .Distinct()
                    .ToList();

                var gridStats = new DispositionGridStatistics
                {
                    Species = gridKey.Species,
                    FleetSegment = gridKey.FleetSegment
                };

                foreach (var (lat, lon) in allCellLocations)
                {
                    var cells = validSummaries
                        .Select(s => s.DispositionGrids
                            .Where(matchingGrids)
                            .SelectMany(g => g.DispositionCells)
                            .FirstOrDefault(c => c.Latitude == lat && c.Longitude == lon))
                        .ToList();

                    var grossCatch = cells.Select(c => c?.GrossCatch ?? 0.0).OrderBy(v => v).ToArray();
                    var liveDiscards = cells.Select(c => c?.LiveDiscards ?? 0.0).OrderBy(v => v).ToArray();
                    var deadDiscards = cells.Select(c => c?.DeadDiscards ?? 0.0).OrderBy(v => v).ToArray();

                    gridStats.DispositionCellsStatistics.Add(new DispositionCellStatistics
                    {
                        Latitude = lat,
                        Longitude = lon,
                        GrossCatch = new DoubleStatistics
                        {
                            Mean = Statistics.Mean(grossCatch),
                            P05 = Statistics.Percentile(grossCatch, 5),
                            P95 = Statistics.Percentile(grossCatch, 95)
                        },
                        LiveDiscards = new DoubleStatistics
                        {
                            Mean = Statistics.Mean(liveDiscards),
                            P05 = Statistics.Percentile(liveDiscards, 5),
                            P95 = Statistics.Percentile(liveDiscards, 95)
                        },
                        DeadDiscards = new DoubleStatistics
                        {
                            Mean = Statistics.Mean(deadDiscards),
                            P05 = Statistics.Percentile(deadDiscards, 5),
                            P95 = Statistics.Percentile(deadDiscards, 95)
                        }
                    });
                }

                result.DispositionGridsStatistics.Add(gridStats);
            }

            return result;
        }

        public FishingActivityStatisticsSummary AddAggregateFishingActivity(List<FishingActivitySummary?> fishingActivitySummaries)
        {
            var result = new FishingActivityStatisticsSummary();

            var validSummaries = fishingActivitySummaries.Where(s => s != null).Select(s => s!).ToList();
            if (validSummaries.Count == 0)
                return result;

            var allFleetSegmentKeys = validSummaries
                .SelectMany(s => s.FishingActivities)
                .Select(a => a.FleetSegment)
                .Where(fs => fs != null)
                .DistinctBy(fs => (fs.GearCode, fs.VesselLengthClass, fs.Scale, fs.CountryCode, fs.Model))
                .ToList();

            foreach (var fleetSegment in allFleetSegmentKeys)
            {
                var values = validSummaries
                    .Select(s => s.FishingActivities
                        .FirstOrDefault(a => a.FleetSegment?.GearCode == fleetSegment.GearCode
                            && a.FleetSegment?.VesselLengthClass == fleetSegment.VesselLengthClass
                            && a.FleetSegment?.Scale == fleetSegment.Scale
                            && a.FleetSegment?.CountryCode == fleetSegment.CountryCode
                            && a.FleetSegment?.Model == fleetSegment.Model))
                    .Select(a => a?.FishingActivityRatio ?? 0.0)
                    .OrderBy(v => v)
                    .ToArray();

                result.FishingActivitiesStatistics.Add(new FishingActivityStatistics
                {
                    FleetSegment = fleetSegment,
                    FishingActivityRatio = new DoubleStatistics
                    {
                        Mean = Statistics.Mean(values),
                        P05 = Statistics.Percentile(values, 5),
                        P95 = Statistics.Percentile(values, 95)
                    }
                });
            }

            return result;
        }

        public SalesStatisticsSummary AddAggregateSales(List<SalesSummary?> salesSummaries)
        {
            var result = new SalesStatisticsSummary();

            var validSummaries = salesSummaries.Where(s => s != null).Select(s => s!).ToList();
            if (validSummaries.Count == 0)
                return result;

            var allMarketCodes = validSummaries
                .SelectMany(s => s.MarketSales)
                .Select(m => m.MarketCode)
                .Distinct()
                .ToList();

            foreach (var marketCode in allMarketCodes)
            {
                var currency = validSummaries
                    .SelectMany(s => s.MarketSales)
                    .First(m => m.MarketCode == marketCode).Currency;

                var marketStats = new MarketSalesStatistics { MarketCode = marketCode, Currency = currency };

                var allSaleKeys = validSummaries
                    .SelectMany(s => s.MarketSales.Where(m => m.MarketCode == marketCode).SelectMany(m => m.Sales))
                    .Where(s => s.Species != null && s.FleetSegment != null)
                    .DistinctBy(s => (s.Species!.SpeciesCode, s.FleetSegment!.GearCode, s.FleetSegment.VesselLengthClass, s.FleetSegment.Scale, s.FleetSegment.CountryCode, s.FleetSegment.Model))
                    .ToList();

                foreach (var saleKey in allSaleKeys)
                {
                    var quantities = validSummaries
                        .Select(s => s.MarketSales
                            .Where(m => m.MarketCode == marketCode)
                            .SelectMany(m => m.Sales)
                            .FirstOrDefault(sale => sale.Species?.SpeciesCode == saleKey.Species?.SpeciesCode
                                && sale.FleetSegment?.GearCode == saleKey.FleetSegment?.GearCode
                                && sale.FleetSegment?.VesselLengthClass == saleKey.FleetSegment?.VesselLengthClass
                                && sale.FleetSegment?.Scale == saleKey.FleetSegment?.Scale
                                && sale.FleetSegment?.CountryCode == saleKey.FleetSegment?.CountryCode
                                && sale.FleetSegment?.Model == saleKey.FleetSegment?.Model))
                        .Select(s => s?.Quantity ?? 0.0)
                        .OrderBy(v => v)
                        .ToArray();

                    var values = validSummaries
                        .Select(s => s.MarketSales
                            .Where(m => m.MarketCode == marketCode)
                            .SelectMany(m => m.Sales)
                            .FirstOrDefault(sale => sale.Species?.SpeciesCode == saleKey.Species?.SpeciesCode
                                && sale.FleetSegment?.GearCode == saleKey.FleetSegment?.GearCode
                                && sale.FleetSegment?.VesselLengthClass == saleKey.FleetSegment?.VesselLengthClass
                                && sale.FleetSegment?.Scale == saleKey.FleetSegment?.Scale
                                && sale.FleetSegment?.CountryCode == saleKey.FleetSegment?.CountryCode
                                && sale.FleetSegment?.Model == saleKey.FleetSegment?.Model))
                        .Select(s => s?.Value ?? 0.0)
                        .OrderBy(v => v)
                        .ToArray();

                    marketStats.SalesStatistics.Add(new SaleStatistics
                    {
                        Species = saleKey.Species,
                        FleetSegment = saleKey.FleetSegment,
                        Quantity = new DoubleStatistics
                        {
                            Mean = Statistics.Mean(quantities),
                            P05 = Statistics.Percentile(quantities, 5),
                            P95 = Statistics.Percentile(quantities, 95)
                        },
                        Value = new DoubleStatistics
                        {
                            Mean = Statistics.Mean(values),
                            P05 = Statistics.Percentile(values, 5),
                            P95 = Statistics.Percentile(values, 95)
                        }
                    });
                }

                result.MarketSalesStatistics.Add(marketStats);
            }

            return result;
        }

        public SpeciesPriceStatisticsSummary AddAggregateSpeciesPrice(List<SpeciesPriceSummary?> speciesPriceSummaries)
        {
            var result = new SpeciesPriceStatisticsSummary();

            var validSummaries = speciesPriceSummaries.Where(s => s != null).Select(s => s!).ToList();
            if (validSummaries.Count == 0)
                return result;

            var allPriceKeys = validSummaries
                .SelectMany(s => s.SpeciesPrices)
                .DistinctBy(p => (p.Species?.SpeciesCode, p.GearCode, p.MarketCode, p.Currency))
                .ToList();

            foreach (var priceKey in allPriceKeys)
            {
                var values = validSummaries
                    .Select(s => s.SpeciesPrices
                        .FirstOrDefault(p => p.Species?.SpeciesCode == priceKey.Species?.SpeciesCode
                            && p.GearCode == priceKey.GearCode
                            && p.MarketCode == priceKey.MarketCode
                            && p.Currency == priceKey.Currency))
                    .Select(p => p?.Price ?? 0.0)
                    .OrderBy(v => v)
                    .ToArray();

                result.SpeciesPricesStatistics.Add(new SpeciesPriceStatistics
                {
                    Species = priceKey.Species,
                    GearCode = priceKey.GearCode,
                    MarketCode = priceKey.MarketCode,
                    Currency = priceKey.Currency,
                    Price = new DoubleStatistics
                    {
                        Mean = Statistics.Mean(values),
                        P05 = Statistics.Percentile(values, 5),
                        P95 = Statistics.Percentile(values, 95)
                    }
                });
            }

            return result;
        }

            }
        }
