using Grpc.Core;
using Grpc.Surimi;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SURIMI.Common.gRPC.Services;
using SURIMI_controller.ConfigurationService;
using SURIMI_controller.Services;

namespace SURIMI.Controller.Tests
{
    public class ControllerServiceTests
    {
        [Fact]
        public async Task GetInitialHCRData_ForwardsRequestTokenAndSummary()
        {
            // Arrange
            var request = new GetInitialHCRDataRequest { ScenarioName = "scenario" };
            var response = new GetInitialHCRDataResponse
            {
                InitialHcrDataSummary = new InitialHCRDataSummary
                {
                    InitialHcrData = { new InitialHCRData { InitialBiomass = 42, InitialFishingMortality = 0.3 } }
                }
            };
            using var cancellation = new CancellationTokenSource();
            var context = new Mock<ServerCallContext>();
            context.Protected().Setup<CancellationToken>("CancellationTokenCore").Returns(cancellation.Token);
            var ecopath = new Mock<IEcopathServiceClient>();
            ecopath.Setup(c => c.GetInitialHCRDataAsync(request, cancellation.Token)).ReturnsAsync(response);
            var sut = new SURIMI_controller.Services.ControllerService(
                Mock.Of<IExperimentManager>(), Mock.Of<ILogger<SURIMI_controller.Services.ControllerService>>(),
                Mock.Of<IConfigurationService>(), ecopath.Object, new ProtocolVersionService());

            // Act
            var actual = await sut.GetInitialHCRData(request, context.Object);

            // Assert
            Assert.Same(response, actual);
            Assert.Same(response.InitialHcrDataSummary, actual.InitialHcrDataSummary);
            ecopath.Verify(c => c.GetInitialHCRDataAsync(request, cancellation.Token), Times.Once);
        }

        [Fact]
        public async Task GetInitialHCRData_RejectsEmptyScenario()
        {
            // Arrange
            var ecopath = new Mock<IEcopathServiceClient>();
            var sut = new SURIMI_controller.Services.ControllerService(
                Mock.Of<IExperimentManager>(), Mock.Of<ILogger<SURIMI_controller.Services.ControllerService>>(),
                Mock.Of<IConfigurationService>(), ecopath.Object, new ProtocolVersionService());

            // Act & Assert
            await Assert.ThrowsAsync<RpcException>(() => sut.GetInitialHCRData(new GetInitialHCRDataRequest(), Mock.Of<ServerCallContext>()));
            ecopath.Verify(c => c.GetInitialHCRDataAsync(It.IsAny<GetInitialHCRDataRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
