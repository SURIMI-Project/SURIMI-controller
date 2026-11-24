using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Surimi;
using Microsoft.Extensions.Logging;
using Moq;
using SURIMI_gui.Services;

namespace SURIMI.Controller.Tests
{
    public class SurimiGUIServiceTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetSimulationsAsync_UsesEndpointMatchingExperimentIds(bool hasExperimentIds)
        {
            // Arrange
            using var cancellation = new CancellationTokenSource();
            var client = new Mock<ControllerService.ControllerServiceClient>(MockBehavior.Strict);
            var status = new SimulationStatus
            {
                SimulationId = "simulation-1",
                ExperimentId = "experiment-1",
                ScenarioName = "scenario",
                StartDateTime = Timestamp.FromDateTime(DateTime.UnixEpoch),
                EndDateTime = Timestamp.FromDateTime(DateTime.UnixEpoch.AddDays(1))
            };
            if (hasExperimentIds)
            {
                var response = new GetSimulationStatusesResponse { SimulationStatuses = { status } };
                client.Setup(c => c.GetSimulationStatusesAsync(
                    It.Is<GetSimulationStatusesRequest>(r => r.ExperimentIds.SequenceEqual(new[] { "experiment-1", "experiment-2" })),
                    null, null, cancellation.Token)).Returns(CreateCall(response));
            }
            else
            {
                var response = new GetAllSimulationStatusesResponse { SimulationStatuses = { status } };
                client.Setup(c => c.GetAllSimulationStatusesAsync(
                    It.IsAny<GetAllSimulationStatusesRequest>(), null, null, cancellation.Token)).Returns(CreateCall(response));
            }

            var enumerations = 0;
            IEnumerable<string> Ids()
            {
                enumerations++;
                if (hasExperimentIds)
                {
                    yield return "experiment-1";
                    yield return "experiment-2";
                }
            }

            var service = new SurimiGUIService(client.Object, Mock.Of<ILogger<SurimiGUIService>>());

            // Act
            var result = await service.GetSimulationsAsync(Ids(), cancellation.Token);

            // Assert
            var actual = Assert.Single(result);
            Assert.Equal(status.SimulationId, actual.SimulationId);
            Assert.Equal(status.ExperimentId, actual.ExperimentId);
            Assert.Equal(status.ScenarioName, actual.ScenarioName);
            Assert.Equal(status.StartDateTime.ToDateTime(), actual.StartDateTime);
            Assert.Equal(status.EndDateTime.ToDateTime(), actual.EndDateTime);
            Assert.Equal(1, enumerations);
            client.VerifyAll();
        }

        [Fact]
        public async Task HcrDefaultsAndSubmission_PreserveSpeciesAndSendOnlySelectedEdits()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource();
            var species = new Species { SpeciesCode = "COD", LengthClass = "large", Age = "3", LifeStage = "adult" };
            var response = new GetInitialHCRDataResponse
            {
                InitialHcrDataSummary = new InitialHCRDataSummary
                {
                    InitialHcrData =
                    {
                        new InitialHCRData { Species = species, InitialBiomass = 100, InitialFishingMortality = 2, DefaultBiomassLimit = 10, DefaultBiomassBase = 40, DefaultFMax = 0.3 },
                        new InitialHCRData { Species = new Species { SpeciesCode = "HAD" }, DefaultBiomassLimit = 1, DefaultBiomassBase = 2, DefaultFMax = 0.2 }
                    }
                }
            };
            SubmitExperimentRequest? submitted = null;
            var client = new Mock<ControllerService.ControllerServiceClient>();
            client.Setup(c => c.GetInitialHCRDataAsync(It.Is<GetInitialHCRDataRequest>(r => r.ScenarioName == "scenario"), null, null, cancellation.Token))
                .Returns(CreateCall(response));
            client.Setup(c => c.SubmitExperimentAsync(It.IsAny<SubmitExperimentRequest>(), null, null, cancellation.Token))
                .Callback<SubmitExperimentRequest, Metadata, DateTime?, CancellationToken>((request, _, _, _) => submitted = request)
                .Returns(CreateCall(new SubmitExperimentResponse { ExperimentId = "experiment" }));
            var service = new SurimiGUIService(client.Object, Mock.Of<ILogger<SurimiGUIService>>());

            // Act
            var rules = await service.GetInitialHCRDataAsync("scenario", cancellation.Token);
            Assert.All(rules, rule => Assert.False(rule.Selected));
            Assert.Equal(species, rules[0].Species);
            Assert.NotSame(species, rules[0].Species);
            Assert.Equal(10, rules[0].BiomassLimit);
            Assert.Equal(40, rules[0].BiomassBase);
            Assert.Equal(0.3, rules[0].FMax);
            rules[0].Selected = true;
            rules[0].BiomassLimit = 11;
            rules[0].BiomassBase = 44;
            rules[0].FMax = 0.4;
            var config = new SURIMI_gui.Models.ExperimentConfig { ScenarioName = "scenario", ClimateScenario = "climate", ExperimentId = "experiment", HarvestControlRules = rules };
            var result = await service.Submit(config, cancellation.Token);

            // Assert
            Assert.Equal("experiment", result);
            Assert.NotNull(submitted);
            Assert.Equal("scenario", submitted.ScenarioName);
            Assert.Equal("climate", submitted.ClimateScenario);
            var target = Assert.Single(submitted.RegulationsDefinitionsSummary.TargetFishingMortalities);
            Assert.Equal(species, target.Species);
            Assert.NotSame(rules[0].Species, target.Species);
            Assert.Equal(11, target.BiomassLimit);
            Assert.Equal(44, target.BiomassBase);
            Assert.Equal(0.4, target.FMax);
            client.VerifyAll();
        }

        [Fact]
        public async Task Submit_WithNoSelectedRules_SendsEmptySummary()
        {
            // Arrange
            SubmitExperimentRequest? submitted = null;
            var client = new Mock<ControllerService.ControllerServiceClient>();
            client.Setup(c => c.SubmitExperimentAsync(It.IsAny<SubmitExperimentRequest>(), null, null, CancellationToken.None))
                .Callback<SubmitExperimentRequest, Metadata, DateTime?, CancellationToken>((request, _, _, _) => submitted = request)
                .Returns(CreateCall(new SubmitExperimentResponse { ExperimentId = "experiment" }));
            var service = new SurimiGUIService(client.Object, Mock.Of<ILogger<SurimiGUIService>>());
            var config = new SURIMI_gui.Models.ExperimentConfig
            {
                HarvestControlRules = [new SURIMI_gui.Models.HarvestControlRule { Species = new Species { SpeciesCode = "COD" } }]
            };

            // Act
            await service.Submit(config, CancellationToken.None);

            // Assert
            Assert.NotNull(submitted);
            Assert.Empty(submitted.RegulationsDefinitionsSummary.TargetFishingMortalities);
        }

        [Fact]
        public async Task GetInitialHCRDataAsync_RejectsMissingSummary()
        {
            // Arrange
            var client = new Mock<ControllerService.ControllerServiceClient>();
            client.Setup(c => c.GetInitialHCRDataAsync(It.IsAny<GetInitialHCRDataRequest>(), null, null, CancellationToken.None))
                .Returns(CreateCall(new GetInitialHCRDataResponse()));
            var service = new SurimiGUIService(client.Object, Mock.Of<ILogger<SurimiGUIService>>());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(() => service.GetInitialHCRDataAsync("scenario", CancellationToken.None));
        }

        private static AsyncUnaryCall<T> CreateCall<T>(T response) => new(
            Task.FromResult(response), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });
    }
}
