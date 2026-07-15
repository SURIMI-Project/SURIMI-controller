using Grpc.Core;
using Grpc.Surimi;
using Microsoft.Extensions.Logging;
using Moq;
using SURIMI_controller.Services;

namespace SURIMI.Controller.Tests
{
    public class EcopathServiceClientTests
    {
        private readonly Mock<ILogger<EcopathServiceClient>> _loggerMock = new();
        private readonly Mock<ISimulationDispatcher> _dispatcherMock = new();
        private readonly string _simulationId = Guid.NewGuid().ToString();

        [Fact]
        public async Task CancelSimulationAsync_ReleasesPod_AfterGrpcCall()
        {
            // Arrange
            var cancelRequest = new CancelSimulationRequest { SimulationId = _simulationId };
            _dispatcherMock
                .Setup(d => d.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, CancelSimulationRequest, CancelSimulationResponse>(
                    cancelRequest, _simulationId, It.IsAny<Func<EcologyService.EcologyServiceClient, CancelSimulationRequest, AsyncUnaryCall<CancelSimulationResponse>>>(),
                    It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new CancelSimulationResponse());

            var sut = new EcopathServiceClient(_loggerMock.Object, _dispatcherMock.Object);

            // Act
            await sut.CancelSimulationAsync(cancelRequest, CancellationToken.None);

            // Assert
            _dispatcherMock.Verify(d => d.ReleasePodFromSimulation(_simulationId), Times.Once);
        }

        [Fact]
        public async Task FinaliseSimulationAsync_ReleasesPod_AfterGrpcCall()
        {
            // Arrange
            var finaliseRequest = new FinaliseSimulationRequest { SimulationId = _simulationId };
            _dispatcherMock
                .Setup(d => d.DispatchWithRetryAsync<EcologyService.EcologyServiceClient, FinaliseSimulationRequest, FinaliseSimulationResponse>(
                    finaliseRequest, _simulationId, It.IsAny<Func<EcologyService.EcologyServiceClient, FinaliseSimulationRequest, AsyncUnaryCall<FinaliseSimulationResponse>>>(),
                    It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new FinaliseSimulationResponse());

            var sut = new EcopathServiceClient(_loggerMock.Object, _dispatcherMock.Object);

            // Act
            await sut.FinaliseSimulationAsync(finaliseRequest, CancellationToken.None);

            // Assert
            _dispatcherMock.Verify(d => d.ReleasePodFromSimulation(_simulationId), Times.Once);
        }
    }
}
