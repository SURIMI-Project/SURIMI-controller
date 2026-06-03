using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace SURIMI_controller.Services
{
    public class DynamicGrpcClientFactory
    {
        public static TClient CreateClient<TClient>(string address, ILogger<GrpcErrorDetailLoggingInterceptor> logger)
            where TClient : ClientBase<TClient>
        {
            var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure,
                MaxReceiveMessageSize = 100 * 1024 * 1024, // 100 MB
                MaxSendMessageSize = 100 * 1024 * 1024,    // 100 MB
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true
                }
            });

            var invoker = channel.Intercept(new GrpcErrorDetailLoggingInterceptor(logger));
            return (TClient)Activator.CreateInstance(typeof(TClient), invoker)!;
        }
    }
}
