using Grpc.Surimi;
using SURIMI_controller.Services;
using FluentAssertions;

namespace SURIMI.Controller.Tests
{
    public class AggregatorServiceCatchDispositionTests
    {
        private readonly AggregatorService _service;

        public AggregatorServiceCatchDispositionTests()
        {
            _service = new AggregatorService();
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithNullList_ShouldReturnEmptyStatistics()
        {
            // Arrange
            List<CatchDispositionSummary?> summaries = null;

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            result.Should().NotBeNull();
            result.DispositionGridsStatistics.Should().BeEmpty();
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithEmptyList_ShouldReturnEmptyStatistics()
        {
            // Arrange
            var summaries = new List<CatchDispositionSummary?>();

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            result.Should().NotBeNull();
            result.DispositionGridsStatistics.Should().BeEmpty();
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithAllNullSummaries_ShouldReturnEmptyStatistics()
        {
            // Arrange
            var summaries = new List<CatchDispositionSummary?> { null, null };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            result.Should().NotBeNull();
            result.DispositionGridsStatistics.Should().BeEmpty();
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithSingleSummary_ShouldCopySpeciesAndFleetSegment()
        {
            // Arrange
            var fs = MakeFleetSegment("OTB");
            var summaries = new List<CatchDispositionSummary?>
            {
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 10.0, DeadDiscards = 5.0 }
                    }
                })
            };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            result.DispositionGridsStatistics.Should().ContainSingle();
            var gridStats = result.DispositionGridsStatistics[0];
            gridStats.Species.SpeciesCode.Should().Be("COD");
            gridStats.FleetSegment.GearCode.Should().Be("OTB");
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithSingleSummary_ShouldCalculateAllThreeCellValues()
        {
            // Arrange
            var fs = MakeFleetSegment("OTB");
            var summaries = new List<CatchDispositionSummary?>
            {
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 20.0, DeadDiscards = 8.0 }
                    }
                })
            };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            var cell = result.DispositionGridsStatistics[0].DispositionCellsStatistics[0];
            cell.Latitude.Should().Be(3.4);
            cell.Longitude.Should().Be(6.7);
            cell.GrossCatch.Mean.Should().Be(100.0);
            cell.LiveDiscards.Mean.Should().Be(20.0);
            cell.DeadDiscards.Mean.Should().Be(8.0);
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithMultipleSummaries_ShouldCalculateMeanForAllThreeValues()
        {
            // Arrange
            var fs = MakeFleetSegment("OTB");
            var summaries = new List<CatchDispositionSummary?>
            {
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 10.0, DeadDiscards = 4.0 }
                    }
                }),
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 200.0, LiveDiscards = 20.0, DeadDiscards = 8.0 }
                    }
                }),
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 300.0, LiveDiscards = 30.0, DeadDiscards = 12.0 }
                    }
                })
            };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            var cell = result.DispositionGridsStatistics[0].DispositionCellsStatistics[0];
            cell.GrossCatch.Mean.Should().Be(200.0);   // (100 + 200 + 300) / 3
            cell.LiveDiscards.Mean.Should().Be(20.0);  // (10 + 20 + 30) / 3
            cell.DeadDiscards.Mean.Should().Be(8.0);   // (4 + 8 + 12) / 3
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithDifferentFleetSegments_ShouldProduceSeparateGridEntries()
        {
            // Arrange
            var fs1 = MakeFleetSegment("OTB");
            var fs2 = MakeFleetSegment("GNS");
            var summaries = new List<CatchDispositionSummary?>
            {
                MakeSummary(
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "COD" },
                        FleetSegment = fs1,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 5.0, DeadDiscards = 2.0 }
                        }
                    },
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "COD" },
                        FleetSegment = fs2,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 50.0, LiveDiscards = 3.0, DeadDiscards = 1.0 }
                        }
                    })
            };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            result.DispositionGridsStatistics.Should().HaveCount(2);
            result.DispositionGridsStatistics.Should().Contain(g => g.FleetSegment.GearCode == "OTB");
            result.DispositionGridsStatistics.Should().Contain(g => g.FleetSegment.GearCode == "GNS");
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithDifferentFleetSegments_ShouldProduceSeparateGridEntries2()
        {
            // Arrange
            var fs1 = MakeFleetSegment("OTB");
            var fs2 = MakeFleetSegment("GNS");
            var summaries = new List<CatchDispositionSummary?>
            {
                // make 2 summaries 
                MakeSummary(
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "HADDOCK" },
                        FleetSegment = fs1,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 5.0, DeadDiscards = 2.0 },
                            new DispositionCell { Latitude = 4.4, Longitude = 7.7, GrossCatch = 75.0, LiveDiscards = 15.0, DeadDiscards = 20.0 }
                        }
                    },
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "COD" },
                        FleetSegment = fs1,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 4.4, Longitude = 7.7, GrossCatch = 75.0, LiveDiscards = 15.0, DeadDiscards = 20.0 }
                        }
                    },
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "COD" },
                        FleetSegment = fs2,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 50.0, LiveDiscards = 3.0, DeadDiscards = 1.0 }
                        }
                    }),

                MakeSummary(
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "HADDOCK" },
                        FleetSegment = fs1,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 200.0, LiveDiscards = 10.0, DeadDiscards = 4.0 }
                        }
                    },
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "COD" },
                        FleetSegment = fs1,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 200.0, LiveDiscards = 10.0, DeadDiscards = 4.0 },
                            new DispositionCell { Latitude = 4.4, Longitude = 7.7, GrossCatch = 150.0, LiveDiscards = 30.0, DeadDiscards = 40.0 }
                        }
                    },
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "COD" },
                        FleetSegment = fs2,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 6.0, DeadDiscards = 2.0 }
                        }
                    })
            };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            result.DispositionGridsStatistics.Should().HaveCount(3);   // 0:HADDOCK-fs1(OTB), 1:COD-fs1(OTB), 2:COD-fs2(GNS)
            result.DispositionGridsStatistics.Should().Contain(g => g.FleetSegment.GearCode == "OTB");
            result.DispositionGridsStatistics.Should().Contain(g => g.FleetSegment.GearCode == "GNS");

            var haddockCells = result.DispositionGridsStatistics[0].DispositionCellsStatistics;
            var cell1 = haddockCells.First(c => c.Latitude == 3.4 && c.Longitude == 6.7);
            cell1.GrossCatch.Mean.Should().Be(150.0);    // (100 + 200) / 2
            cell1.LiveDiscards.Mean.Should().Be(7.5);    // (5 + 10) / 2
            cell1.DeadDiscards.Mean.Should().Be(3.0);    // (2 + 4) / 2

            var CodCells = result.DispositionGridsStatistics[1].DispositionCellsStatistics;
            var firstCodcell = CodCells.First(c => c.Latitude == 4.4 && c.Longitude == 7.7);
            firstCodcell.GrossCatch.Mean.Should().Be(112.5);   // (75 + 150) / 2
            firstCodcell.LiveDiscards.Mean.Should().Be(22.5);  // (15 + 30) / 2
            firstCodcell.DeadDiscards.Mean.Should().Be(30.0);  // (20 + 40) / 2

            var secondCodCells = result.DispositionGridsStatistics[2].DispositionCellsStatistics;
            var cell3 = secondCodCells.First(c => c.Latitude == 3.4 && c.Longitude == 6.7);
            cell3.GrossCatch.Mean.Should().Be(75.0);    // (50 + 100) / 2
            cell3.LiveDiscards.Mean.Should().Be(4.5);   // (3 + 6) / 2
            cell3.DeadDiscards.Mean.Should().Be(1.5);   // (1 + 2) / 2
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithDifferentSpeciesSameFleetSegment_ShouldProduceSeparateGridEntries()
        {
            // Arrange
            var fs = MakeFleetSegment("OTB");
            var summaries = new List<CatchDispositionSummary?>
            {
                MakeSummary(
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "COD" },
                        FleetSegment = fs,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 5.0, DeadDiscards = 2.0 }
                        }
                    },
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "HADDOCK" },
                        FleetSegment = fs,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 80.0, LiveDiscards = 4.0, DeadDiscards = 1.0 }
                        }
                    })
            };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            result.DispositionGridsStatistics.Should().HaveCount(2);
            result.DispositionGridsStatistics.Should().Contain(g => g.Species.SpeciesCode == "COD");
            result.DispositionGridsStatistics.Should().Contain(g => g.Species.SpeciesCode == "HADDOCK");
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithMissingCellInOneSummary_ShouldTreatMissingAsZero()
        {
            // Arrange
            var fs = MakeFleetSegment("OTB");
            var summaries = new List<CatchDispositionSummary?>
            {
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 10.0, DeadDiscards = 4.0 },
                        new DispositionCell { Latitude = 3.5, Longitude = 6.8, GrossCatch = 200.0, LiveDiscards = 20.0, DeadDiscards = 8.0 }
                    }
                }),
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.5, Longitude = 6.8, GrossCatch = 100.0, LiveDiscards = 10.0, DeadDiscards = 4.0 }
                    }
                })
                    // cell (3.4, 6.7) missing → treated as 0
            };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            var cells = result.DispositionGridsStatistics[0].DispositionCellsStatistics;
            cells.Should().HaveCount(2);

            var cell1 = cells.First(c => c.Latitude == 3.4 && c.Longitude == 6.7);
            cell1.GrossCatch.Mean.Should().Be(50.0);    // (100 + 0) / 2
            cell1.LiveDiscards.Mean.Should().Be(5.0);   // (10 + 0) / 2
            cell1.DeadDiscards.Mean.Should().Be(2.0);   // (4 + 0) / 2

            var cell2 = cells.First(c => c.Latitude == 3.5 && c.Longitude == 6.8);
            cell2.GrossCatch.Mean.Should().Be(150.0);   // (200 + 100) / 2
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithMixedNullSummaries_ShouldIgnoreNulls()
        {
            // Arrange
            var fs = MakeFleetSegment("OTB");
            var summaries = new List<CatchDispositionSummary?>
            {
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 100.0, LiveDiscards = 10.0, DeadDiscards = 4.0 }
                    }
                }),
                null,
                MakeSummary(new DispositionGrid
                {
                    Species = new Species { SpeciesCode = "COD" },
                    FleetSegment = fs,
                    DispositionCells =
                    {
                        new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = 200.0, LiveDiscards = 20.0, DeadDiscards = 8.0 }
                    }
                })
            };

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            var cell = result.DispositionGridsStatistics[0].DispositionCellsStatistics[0];
            cell.GrossCatch.Mean.Should().Be(150.0);   // (100 + 200) / 2, null ignored
            cell.LiveDiscards.Mean.Should().Be(15.0);  // (10 + 20) / 2
            cell.DeadDiscards.Mean.Should().Be(6.0);   // (4 + 8) / 2
        }

        [Fact]
        public void AddAggregateCatchDisposition_WithLargeDataset_ShouldCalculatePercentilesForAllThreeValues()
        {
            // Arrange — 100 summaries with values 1..100
            var fs = MakeFleetSegment("OTB");
            var summaries = Enumerable.Range(1, 100)
                .Select(i => (CatchDispositionSummary?)MakeSummary(
                    new DispositionGrid
                    {
                        Species = new Species { SpeciesCode = "COD" },
                        FleetSegment = fs,
                        DispositionCells =
                        {
                            new DispositionCell { Latitude = 3.4, Longitude = 6.7, GrossCatch = i, LiveDiscards = i * 0.1, DeadDiscards = i * 0.05 }
                        }
                    }))
                .ToList();

            // Act
            var result = _service.AddAggregateCatchDisposition(summaries);

            // Assert
            var cell = result.DispositionGridsStatistics[0].DispositionCellsStatistics[0];

            cell.GrossCatch.Mean.Should().Be(50.5);
            cell.GrossCatch.P05.Should().BeInRange(4.0, 6.0);
            cell.GrossCatch.P95.Should().BeInRange(94.0, 96.0);

            cell.LiveDiscards.Mean.Should().BeApproximately(5.05, 0.01);
            cell.LiveDiscards.P05.Should().BeInRange(0.4, 0.6);
            cell.LiveDiscards.P95.Should().BeInRange(9.4, 9.6);

            cell.DeadDiscards.Mean.Should().BeApproximately(2.525, 0.001);
            cell.DeadDiscards.P05.Should().BeInRange(0.2, 0.3);
            cell.DeadDiscards.P95.Should().BeInRange(4.7, 4.8);
        }

        private static CatchDispositionSummary MakeSummary(params DispositionGrid[] grids)
        {
            var summary = new CatchDispositionSummary();
            foreach (var grid in grids) summary.DispositionGrids.Add(grid);
            return summary;
        }

        private static FleetSegment MakeFleetSegment(string gearCode, string model = "POSEIDON") =>
            new FleetSegment { GearCode = gearCode, VesselLengthClass = "L1", Scale = "S1", CountryCode = "NL", Model = model };

        }
}
