using Grpc.Surimi;
using SURIMI_controller.Services;

namespace SURIMI.Controller.Tests
{
    public class AggregatorServiceTests
    {
        private readonly AggregatorService _service;

        public AggregatorServiceTests()
        {
            _service = new AggregatorService();
        }

        [Fact]
        public void AddAggregateBiomass_WithNullList_ShouldReturnEmptyStatistics()
        {
            // Arrange
            List<BiomassSummary?> biomassSummaries = null;

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.BiomassGridsStatistics);
        }

        [Fact]
        public void AddAggregateBiomass_WithEmptyList_ShouldReturnEmptyStatistics()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>();

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.BiomassGridsStatistics);
        }

        [Fact]
        public void AddAggregateBiomass_WithAllNullSummaries_ShouldReturnEmptyStatistics()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?> { null, null, null };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.BiomassGridsStatistics);
        }

        [Fact]
        public void AddAggregateBiomass_WithSingleSummary_ShouldCalculateCorrectStatistics()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells =
                            {
                                new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 },
                                new BiomassCell { Biomass = 200.0, Latitude = 3.5, Longitude = 6.8 }
                            }
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.BiomassGridsStatistics);
            Assert.Equal("COD", result.BiomassGridsStatistics[0].Species.SpeciesCode);
            Assert.Equal(2, result.BiomassGridsStatistics[0].BiomassCellsStatistics.Count);
            
            // With only one value, mean, p05, and p95 should all be the same value
            var firstCell = result.BiomassGridsStatistics[0].BiomassCellsStatistics[0];
            Assert.Equal(3.4, firstCell.Latitude);
            Assert.Equal(6.7, firstCell.Longitude);
            Assert.Equal(100.0, firstCell.Biomass.Mean);
            Assert.Equal(100.0, firstCell.Biomass.P05);
            Assert.Equal(100.0, firstCell.Biomass.P95);
            
            var secondCell = result.BiomassGridsStatistics[0].BiomassCellsStatistics[1];
            Assert.Equal(3.5, secondCell.Latitude);
            Assert.Equal(6.8, secondCell.Longitude);
            Assert.Equal(200.0, secondCell.Biomass.Mean);
            Assert.Equal(200.0, secondCell.Biomass.P05);
            Assert.Equal(200.0, secondCell.Biomass.P95);
        }

        [Fact]
        public void AddAggregateBiomass_WithMultipleSummaries_ShouldCalculateCorrectMean()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 } }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = 200.0, Latitude = 3.4, Longitude = 6.7 } }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = 300.0, Latitude = 3.4, Longitude = 6.7 } }
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.BiomassGridsStatistics);
            Assert.Equal("COD", result.BiomassGridsStatistics[0].Species.SpeciesCode);
            Assert.Single(result.BiomassGridsStatistics[0].BiomassCellsStatistics);
            
            // Mean of 100, 200, 300 should be 200
            Assert.Equal(200.0, result.BiomassGridsStatistics[0].BiomassCellsStatistics[0].Biomass.Mean);
            Assert.Equal(3.4, result.BiomassGridsStatistics[0].BiomassCellsStatistics[0].Latitude);
            Assert.Equal(6.7, result.BiomassGridsStatistics[0].BiomassCellsStatistics[0].Longitude);
        }

        [Fact]
        public void AddAggregateBiomass_WithMultipleSpecies_ShouldProcessAllSpecies()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 } }
                        },
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "HADDOCK" },
                            BiomassCells = { new BiomassCell { Biomass = 50.0, Latitude = 3.5, Longitude = 6.8 } }
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.BiomassGridsStatistics.Count);
            Assert.Contains(result.BiomassGridsStatistics, grid => grid.Species.SpeciesCode == "COD");
            Assert.Contains(result.BiomassGridsStatistics, grid => grid.Species.SpeciesCode == "HADDOCK");
        }

        [Fact]
        public void AddAggregateBiomass_WithMixedNullSummaries_ShouldIgnoreNulls()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 } }
                        }
                    }
                },
                null,
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = 200.0, Latitude = 3.4, Longitude = 6.7 } }
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.BiomassGridsStatistics);
            // Mean of 100 and 200 should be 150 (null is ignored)
            Assert.Equal(150.0, result.BiomassGridsStatistics[0].BiomassCellsStatistics[0].Biomass.Mean);
        }

        [Fact]
        public void AddAggregateBiomass_WithMissingSpeciesInSomeSummaries_ShouldUseZeroForMissing()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 } }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "HADDOCK" },
                            BiomassCells = { new BiomassCell { Biomass = 200.0, Latitude = 3.5, Longitude = 6.8 } }
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.BiomassGridsStatistics.Count);
            
            // COD appears in first summary only, so mean = (100 + 0) / 2 = 50
            var codStats = result.BiomassGridsStatistics.First(g => g.Species.SpeciesCode == "COD");
            Assert.Single(codStats.BiomassCellsStatistics);
            Assert.Equal(50.0, codStats.BiomassCellsStatistics[0].Biomass.Mean);
            Assert.Equal(3.4, codStats.BiomassCellsStatistics[0].Latitude);
            Assert.Equal(6.7, codStats.BiomassCellsStatistics[0].Longitude);
            
            // HADDOCK appears in second summary only, so mean = (0 + 200) / 2 = 100
            var haddockStats = result.BiomassGridsStatistics.First(g => g.Species.SpeciesCode == "HADDOCK");
            Assert.Single(haddockStats.BiomassCellsStatistics);
            Assert.Equal(100.0, haddockStats.BiomassCellsStatistics[0].Biomass.Mean);
            Assert.Equal(3.5, haddockStats.BiomassCellsStatistics[0].Latitude);
            Assert.Equal(6.8, haddockStats.BiomassCellsStatistics[0].Longitude);
        }

        [Fact]
        public void AddAggregateBiomass_WithMissingCellsInSomeGrids_ShouldUseZeroForMissing()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 },
                                new BiomassCell { Biomass = 200.0, Latitude = 3.5, Longitude = 6.8 }
                            }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = 150.0, Latitude = 3.5, Longitude = 6.8 } }
                            // Cell at (3.4, 6.7) is missing because biomass is 0
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.BiomassGridsStatistics);
            Assert.Equal(2, result.BiomassGridsStatistics[0].BiomassCellsStatistics.Count);
            
            // First cell at (3.4, 6.7): (100 + 0) / 2 = 50
            var firstCell = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.4 && c.Longitude == 6.7);
            Assert.Equal(50.0, firstCell.Biomass.Mean);
            
            // Second cell at (3.5, 6.8): (200 + 150) / 2 = 175
            var secondCell = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.5 && c.Longitude == 6.8);
            Assert.Equal(175.0, secondCell.Biomass.Mean);
        }

        [Fact]
        public void AddAggregateBiomass_WithNullBiomassCells_ShouldSkipGrid()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.BiomassGridsStatistics);
        }

        [Fact]
        public void AddAggregateBiomass_WithLargeDataset_ShouldCalculatePercentiles()
        {
            // Arrange - Create 100 summaries with biomass values from 1 to 100
            var biomassSummaries = new List<BiomassSummary?>();
            for (int i = 1; i <= 100; i++)
            {
                biomassSummaries.Add(new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { new BiomassCell { Biomass = i, Latitude = 3.4, Longitude = 6.7 } }
                        }
                    }
                });
            }

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.BiomassGridsStatistics);
            var stats = result.BiomassGridsStatistics[0].BiomassCellsStatistics[0].Biomass;
            
            // Mean of 1 to 100 should be 50.5
            Assert.Equal(50.5, stats.Mean);
            
            // P05 should be around 5
            Assert.InRange(stats.P05, 4.0, 6.0);
            
            // P95 should be around 95
            Assert.InRange(stats.P95, 94.0, 96.0);
        }

        [Fact]
        public void AddAggregateBiomass_WithZeroBiomassValues_ShouldHandleCorrectly()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                // Cell at (3.4, 6.7) not included (biomass = 0)
                                new BiomassCell { Biomass = 100.0, Latitude = 3.5, Longitude = 6.8 }
                            }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 50.0, Latitude = 3.4, Longitude = 6.7 },
                                // Cell at (3.5, 6.8) not included (biomass = 0)
                            }
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.BiomassGridsStatistics);
            Assert.Equal(2, result.BiomassGridsStatistics[0].BiomassCellsStatistics.Count);
            
            // Cell at (3.4, 6.7): (0 + 50) / 2 = 25
            var firstCell = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.4 && c.Longitude == 6.7);
            Assert.Equal(25.0, firstCell.Biomass.Mean);
            
            // Cell at (3.5, 6.8): (100 + 0) / 2 = 50
            var secondCell = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.5 && c.Longitude == 6.8);
            Assert.Equal(50.0, secondCell.Biomass.Mean);
        }

        [Fact]
        public void AddAggregateBiomass_WithComplexMissingCellsScenario_ShouldCalculateCorrectly()
        {
            // Arrange - 3 summaries with different cell patterns
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 },
                                new BiomassCell { Biomass = 200.0, Latitude = 3.5, Longitude = 6.8 },
                                new BiomassCell { Biomass = 300.0, Latitude = 3.6, Longitude = 6.9 }
                            }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                // (3.4, 6.7) missing = 0
                                new BiomassCell { Biomass = 150.0, Latitude = 3.5, Longitude = 6.8 },
                                new BiomassCell { Biomass = 250.0, Latitude = 3.6, Longitude = 6.9 }
                            }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 120.0, Latitude = 3.4, Longitude = 6.7 },
                                // (3.5, 6.8) missing = 0
                                // (3.6, 6.9) missing = 0
                            }
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.BiomassGridsStatistics);
            Assert.Equal(3, result.BiomassGridsStatistics[0].BiomassCellsStatistics.Count);
            
            // Cell at (3.4, 6.7): (100 + 0 + 120) / 3 = 73.333...
            var cell1 = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.4 && c.Longitude == 6.7);
            Assert.Equal(73.333, cell1.Biomass.Mean, 3);
            
            // Cell at (3.5, 6.8): (200 + 150 + 0) / 3 = 116.666...
            var cell2 = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.5 && c.Longitude == 6.8);
            Assert.Equal(116.667, cell2.Biomass.Mean, 3);
            
            // Cell at (3.6, 6.9): (300 + 250 + 0) / 3 = 183.333...
            var cell3 = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.6 && c.Longitude == 6.9);
            Assert.Equal(183.333, cell3.Biomass.Mean, 3);
        }

        [Fact]
        public void AddAggregateBiomass_WithMultipleSpeciesAndMixedCells_ShouldProcessIndependently()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 },
                                new BiomassCell { Biomass = 200.0, Latitude = 3.5, Longitude = 6.8 }
                            }
                        },
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "HADDOCK" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 50.0, Latitude = 3.4, Longitude = 6.7 }
                            }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 150.0, Latitude = 3.5, Longitude = 6.8 }
                                // (3.4, 6.7) missing for COD
                            }
                        },
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "HADDOCK" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 75.0, Latitude = 3.4, Longitude = 6.7 },
                                new BiomassCell { Biomass = 100.0, Latitude = 3.5, Longitude = 6.8 }
                            }
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.BiomassGridsStatistics.Count);
            
            // COD statistics
            var codStats = result.BiomassGridsStatistics.First(g => g.Species.SpeciesCode == "COD");
            Assert.Equal(2, codStats.BiomassCellsStatistics.Count);
            
            var codCell1 = codStats.BiomassCellsStatistics.First(c => c.Latitude == 3.4);
            Assert.Equal(50.0, codCell1.Biomass.Mean); // (100 + 0) / 2
            
            var codCell2 = codStats.BiomassCellsStatistics.First(c => c.Latitude == 3.5);
            Assert.Equal(175.0, codCell2.Biomass.Mean); // (200 + 150) / 2
            
            // HADDOCK statistics
            var haddockStats = result.BiomassGridsStatistics.First(g => g.Species.SpeciesCode == "HADDOCK");
            Assert.Equal(2, haddockStats.BiomassCellsStatistics.Count);
            
            var haddockCell1 = haddockStats.BiomassCellsStatistics.First(c => c.Latitude == 3.4);
            Assert.Equal(62.5, haddockCell1.Biomass.Mean); // (50 + 75) / 2
            
            var haddockCell2 = haddockStats.BiomassCellsStatistics.First(c => c.Latitude == 3.5);
            Assert.Equal(50.0, haddockCell2.Biomass.Mean); // (0 + 100) / 2
        }

        [Fact]
        public void AddAggregateBiomass_WithAllCellsMissingInOneSummary_ShouldUseZeroForAll()
        {
            // Arrange
            var biomassSummaries = new List<BiomassSummary?>
            {
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = 
                            { 
                                new BiomassCell { Biomass = 100.0, Latitude = 3.4, Longitude = 6.7 },
                                new BiomassCell { Biomass = 200.0, Latitude = 3.5, Longitude = 6.8 }
                            }
                        }
                    }
                },
                new BiomassSummary
                {
                    BiomassGrids =
                    {
                        new BiomassGrid
                        {
                            Species = new Species { SpeciesCode = "COD" },
                            BiomassCells = { } // Empty - all cells have biomass 0
                        }
                    }
                }
            };

            // Act
            var result = _service.AddAggregateBiomass(biomassSummaries);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.BiomassGridsStatistics);
            Assert.Equal(2, result.BiomassGridsStatistics[0].BiomassCellsStatistics.Count);
            
            // Cell at (3.4, 6.7): (100 + 0) / 2 = 50
            var cell1 = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.4);
            Assert.Equal(50.0, cell1.Biomass.Mean);
            
            // Cell at (3.5, 6.8): (200 + 0) / 2 = 100
            var cell2 = result.BiomassGridsStatistics[0].BiomassCellsStatistics
                .First(c => c.Latitude == 3.5);
            Assert.Equal(100.0, cell2.Biomass.Mean);
        }
    }
}
