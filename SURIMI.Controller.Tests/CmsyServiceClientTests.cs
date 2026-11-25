using FluentAssertions;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Surimi;
using Microsoft.Extensions.Logging;
using Moq;
using SURIMI_controller.Services;

namespace SURIMI.Controller.Tests
{
    public class CmsyServiceClientTests
    {
        [Fact]
        public async Task AddInitialise_HasNoDeadlineAndPreservesCancellationAndResponseTracking()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var response = new InitialiseExperimentResponse();
            var responseSource = new TaskCompletionSource<InitialiseExperimentResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var call = new AsyncUnaryCall<InitialiseExperimentResponse>(
                responseSource.Task,
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
            var request = new InitialiseExperimentRequest { ExperimentId = Guid.NewGuid().ToString() };
            var callInvokerMock = new Mock<CallInvoker>();
            CallOptions? capturedOptions = null;
            callInvokerMock
                .Setup(invoker => invoker.AsyncUnaryCall(
                    It.IsAny<Method<InitialiseExperimentRequest, InitialiseExperimentResponse>>(),
                    It.IsAny<string>(),
                    It.IsAny<CallOptions>(),
                    request))
                .Callback<Method<InitialiseExperimentRequest, InitialiseExperimentResponse>, string, CallOptions, InitialiseExperimentRequest>(
                    (method, host, options, message) => capturedOptions = options)
                .Returns(call);
            var clientFactoryMock = new Mock<GrpcClientFactory>();
            clientFactoryMock
                .Setup(factory => factory.CreateClient<StockAssessmentService.StockAssessmentServiceClient>("Cmsy"))
                .Returns(new StockAssessmentService.StockAssessmentServiceClient(callInvokerMock.Object));
            var initialisationTasks = new List<Task<InitialiseExperimentResponse>>();
            var sut = new CmsyServiceClient(clientFactoryMock.Object, Mock.Of<ILogger<CmsyServiceClient>>());

            // Act
            var result = sut.AddInitialise(initialisationTasks, request, cts.Token);

            // Assert
            result.Should().BeSameAs(call);
            initialisationTasks.Should().ContainSingle().Which.Should().BeSameAs(responseSource.Task);
            responseSource.Task.IsCompleted.Should().BeFalse();
            capturedOptions.Should().NotBeNull();
            capturedOptions!.Value.Deadline.Should().BeNull();
            capturedOptions.Value.CancellationToken.Should().Be(cts.Token);
            cts.Cancel();
            capturedOptions.Value.CancellationToken.IsCancellationRequested.Should().BeTrue();
            responseSource.SetResult(response);
            (await initialisationTasks[0]).Should().BeSameAs(response);
            clientFactoryMock.Verify(factory => factory.CreateClient<StockAssessmentService.StockAssessmentServiceClient>("Cmsy"), Times.Once);
        }
    }
}
