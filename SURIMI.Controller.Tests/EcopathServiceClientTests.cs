using FluentAssertions;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using Microsoft.Extensions.Logging;
using Moq;
using SURIMI_controller.Services;

namespace SURIMI.Controller.Tests
{
    public class EcopathServiceClientTests
    {
        [Fact]
        public void AddInitialise_UsesNamedEcopathClientAndTracksResponseTask()
        {
            // Arrange
            var response = new InitialiseSimulationResponse();
            var call = new AsyncUnaryCall<InitialiseSimulationResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
            var callInvokerMock = new Mock<CallInvoker>();
            var ecologyClientMock = new Mock<EcologyService.EcologyServiceClient>(callInvokerMock.Object);
            var clientFactoryMock = new Mock<GrpcClientFactory>();
            clientFactoryMock
                .Setup(factory => factory.CreateClient<EcologyService.EcologyServiceClient>("Ecopath"))
                .Returns(ecologyClientMock.Object);
            var request = new InitialiseSimulationRequest { SimulationId = Guid.NewGuid().ToString() };
            ecologyClientMock
                .Setup(client => client.InitialiseSimulationAsync(request, null, null, It.IsAny<CancellationToken>()))
                .Returns(call);
            var initialisationTasks = new List<Task<InitialiseSimulationResponse>>();
            var sut = new EcopathServiceClient(clientFactoryMock.Object, Mock.Of<ILogger<EcopathServiceClient>>());

            // Act
            var result = sut.AddInitialise(initialisationTasks, request);

            // Assert
            result.Should().BeSameAs(call);
            initialisationTasks.Should().ContainSingle().Which.Should().BeSameAs(call.ResponseAsync);
            clientFactoryMock.Verify(factory => factory.CreateClient<EcologyService.EcologyServiceClient>("Ecopath"), Times.Once);
            ecologyClientMock.Verify(client => client.InitialiseSimulationAsync(request, null, null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
