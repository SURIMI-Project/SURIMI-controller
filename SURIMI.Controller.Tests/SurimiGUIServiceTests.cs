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

        private static AsyncUnaryCall<T> CreateCall<T>(T response) => new(
            Task.FromResult(response), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });
    }
}
