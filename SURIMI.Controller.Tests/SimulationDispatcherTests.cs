using FluentAssertions;
using Grpc.Core;
using Grpc.Surimi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SURIMI_controller.Services;

namespace SURIMI.Controller.Tests
{
    public class SimulationDispatcherTests
    {
        private readonly Mock<ILogger<SimulationDispatcher>> _loggerMock = new();
        private readonly Mock<ILogger<GrpcErrorDetailLoggingInterceptor>> _interceptorLoggerMock = new();

        // A fake AsyncUnaryCall that immediately returns a completed response – no network access.
        private static AsyncUnaryCall<T> FakeCall<T>(T response) =>
            new(Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });

        // A fake AsyncUnaryCall that immediately faults – used to trigger the retry path.
        private static AsyncUnaryCall<T> FailingCall<T>(RpcException ex) =>
            new(Task.FromException<T>(ex),
                Task.FromResult(new Metadata()),
                () => new Status(ex.StatusCode, ex.Message),
                () => new Metadata(),
                () => { });

        /// <summary>
        /// Creates a SimulationDispatcher "System Under Test" (SUT) with the specified options and mocked loggers.
        /// </summary>
        /// <param name="ecopathUrl">The URL of the Ecopath service.</param>
        /// <param name="podNamespace">The namespace for the pods.</param>
        /// <param name="nrOfPods">The number of pods.</param>
        /// <returns>A configured SimulationDispatcher instance.</returns>
        private SimulationDispatcher CreateSut(string ecopathUrl, string podNamespace, int nrOfPods) =>
            new(Options.Create(new SimulationDispatcherOptions
            {
                EcopathUrl = ecopathUrl,
                PodNamespace = podNamespace,
                NrOfPods = nrOfPods
            }),
            _loggerMock.Object,
            _interceptorLoggerMock.Object);

        // Convenience overload for the common localhost scenario System Under Test (SUT).
        private SimulationDispatcher CreateLocalSut(int nrOfPods = 1) =>
            CreateSut("http://localhost:5000", "test-ns", nrOfPods);

        private static Func<EcologyService.EcologyServiceClient, SimulateStepRequest, AsyncUnaryCall<SimulateStepResponse>> SuccessMethod =>
            (_, _) => FakeCall(new SimulateStepResponse());

        // ─── DispatchAsync ───────────────────────────────────────────────────────────

        [Fact]
        public void DispatchAsync_NewGuidSimulation_AssignsPodAndReturnsCall()
        {
            // Arrange
            var sut = CreateLocalSut();
            var simulationId = Guid.NewGuid().ToString();

            // Act
            var call = sut.DispatchAsync(new SimulateStepRequest(), simulationId, SuccessMethod);

            // Assert – the call is returned; verifying pod occupation is done indirectly below
            call.Should().NotBeNull();
        }

        [Fact]
        public void DispatchAsync_SameGuidSimulation_DoesNotThrowAndReusesOccupiedPod()
        {
            // Arrange
            var sut = CreateLocalSut();   // only one pod available
            var simulationId = Guid.NewGuid().ToString();

            // Act – first call occupies the single pod
            sut.DispatchAsync(new SimulateStepRequest(), simulationId, SuccessMethod);

            // Act – second call with the same simulationId must reuse the occupied pod, not throw
            var act = () => sut.DispatchAsync(new SimulateStepRequest(), simulationId, SuccessMethod);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void DispatchAsync_NoPodAvailable_ThrowsException()
        {
            // Arrange – only one pod; occupy it with simulationId1 first
            var sut = CreateLocalSut();
            var simulationId1 = Guid.NewGuid().ToString();
            var simulationId2 = Guid.NewGuid().ToString();
            sut.DispatchAsync(new SimulateStepRequest(), simulationId1, SuccessMethod);

            // Act
            var act = () => sut.DispatchAsync(new SimulateStepRequest(), simulationId2, SuccessMethod);

            // Assert
            act.Should().Throw<Exception>().WithMessage("No available pods");
        }

        [Fact]
        public void DispatchAsync_DnsResolutionFails_ReleasePodAndThrowsRpcUnavailable()
        {
            // Arrange – address contains "pod" so DNS resolution is attempted on the substituted host
            var sut = CreateSut(
                "http://pod.surimi-ecopath.namespace.svc.cluster.local:8080",
                "nonexistent-ns-xyz",
                1);
            var simulationId = Guid.NewGuid().ToString();

            // Act
            var act = () => sut.DispatchAsync(new SimulateStepRequest(), simulationId, SuccessMethod);

            // Assert
            act.Should().Throw<RpcException>()
                .Which.StatusCode.Should().Be(StatusCode.Unavailable);
        }

        [Fact]
        public void DispatchAsync_AfterDnsFailure_PodIsReleasedAndCanBeClaimedAgain()
        {
            // Arrange – one pod; DNS will fail on first call
            var sut = CreateSut(
                "http://pod.surimi-ecopath.namespace.svc.cluster.local:8080",
                "nonexistent-ns-xyz",
                1);
            var simulationId1 = Guid.NewGuid().ToString();
            var simulationId2 = Guid.NewGuid().ToString();

            try { sut.DispatchAsync(new SimulateStepRequest(), simulationId1, SuccessMethod); }
            catch (RpcException) { /* expected */ }

            // Act – pod must have been released, so a second simulation can still fail with DNS (not "No available pods")
            var act = () => sut.DispatchAsync(new SimulateStepRequest(), simulationId2, SuccessMethod);

            // Assert – should throw Unavailable (DNS), NOT a plain Exception("No available pods")
            act.Should().Throw<RpcException>()
                .Which.StatusCode.Should().Be(StatusCode.Unavailable);
        }

        [Fact]
        public void DispatchAsync_NonGuidSimulationId_UsesFirstPodWithoutReservingIt()
        {
            // Arrange
            var sut = CreateLocalSut();
            var nonGuidId = "not-a-guid";
            var guidId = Guid.NewGuid().ToString();

            // Act – non-GUID call should not consume the pod slot
            sut.DispatchAsync(new SimulateStepRequest(), nonGuidId, SuccessMethod);

            // A subsequent GUID call must still be able to claim pod1 (no "No available pods")
            var act = () => sut.DispatchAsync(new SimulateStepRequest(), guidId, SuccessMethod);

            // Assert
            act.Should().NotThrow();
        }

        // ─── ReleasePodFromSimulation ────────────────────────────────────────────────

        [Fact]
        public void ReleasePodFromSimulation_KnownSimulation_MakesPodAvailableAgain()
        {
            // Arrange – one pod; occupy it, then release it
            var sut = CreateLocalSut();
            var simulationId1 = Guid.NewGuid().ToString();
            var simulationId2 = Guid.NewGuid().ToString();
            sut.DispatchAsync(new SimulateStepRequest(), simulationId1, SuccessMethod);

            // Act
            sut.ReleasePodFromSimulation(simulationId1);

            // Assert – pod is free again, a new simulation can claim it
            var act = () => sut.DispatchAsync(new SimulateStepRequest(), simulationId2, SuccessMethod);
            act.Should().NotThrow();
        }

        [Fact]
        public void ReleasePodFromSimulation_UnknownSimulation_DoesNotThrow()
        {
            // Arrange
            var sut = CreateLocalSut();

            // Act & Assert
            var act = () => sut.ReleasePodFromSimulation(Guid.NewGuid().ToString());
            act.Should().NotThrow();
        }

        // ─── DispatchWithRetryAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task DispatchWithRetryAsync_UnavailableOnFirstAttempt_RetriesAndSucceeds()
        {
            // Arrange
            var sut = CreateLocalSut(2);
            var simulationId = Guid.NewGuid().ToString();
            var attempts = 0;
            Func<EcologyService.EcologyServiceClient, SimulateStepRequest, AsyncUnaryCall<SimulateStepResponse>> grpcMethod =
                (_, _) =>
                {
                    if (++attempts == 1)
                        return FailingCall<SimulateStepResponse>(new RpcException(new Status(StatusCode.Unavailable, "transient")));
                    return FakeCall(new SimulateStepResponse());
                };

            // Act
            var response = await sut.DispatchWithRetryAsync(new SimulateStepRequest(), simulationId, grpcMethod, maxRetries: 3, retryDelayMs: 0);

            // Assert
            response.Should().NotBeNull();
            attempts.Should().Be(2);
        }

        [Fact]
        public async Task DispatchWithRetryAsync_NonUnavailableException_DoesNotRetryAndThrows()
        {
            // Arrange
            var sut = CreateLocalSut(2);
            var simulationId = Guid.NewGuid().ToString();
            var attempts = 0;
            Func<EcologyService.EcologyServiceClient, SimulateStepRequest, AsyncUnaryCall<SimulateStepResponse>> grpcMethod =
                (_, _) =>
                {
                    attempts++;
                    return FailingCall<SimulateStepResponse>(new RpcException(new Status(StatusCode.Internal, "fatal")));
                };

            // Act
            var act = async () => await sut.DispatchWithRetryAsync(new SimulateStepRequest(), simulationId, grpcMethod, maxRetries: 3, retryDelayMs: 0);

            // Assert
            await act.Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == StatusCode.Internal);
            attempts.Should().Be(1);
        }

        [Fact]
        public async Task DispatchWithRetryAsync_ExceedsMaxRetries_PropagatesUnavailableException()
        {
            // Arrange
            var sut = CreateLocalSut(2);
            var simulationId = Guid.NewGuid().ToString();
            Func<EcologyService.EcologyServiceClient, SimulateStepRequest, AsyncUnaryCall<SimulateStepResponse>> grpcMethod =
                (_, _) => FailingCall<SimulateStepResponse>(new RpcException(new Status(StatusCode.Unavailable, "always down")));

            // Act
            var act = async () => await sut.DispatchWithRetryAsync(new SimulateStepRequest(), simulationId, grpcMethod, maxRetries: 2, retryDelayMs: 0);

            // Assert
            await act.Should().ThrowAsync<RpcException>().Where(e => e.StatusCode == StatusCode.Unavailable);
        }
    }
}
