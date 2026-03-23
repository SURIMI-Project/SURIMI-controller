using Grpc.Core;
using Grpc.Net.Client;

namespace SurimiController.Services
{
    public class DynamicGrpcClientFactory
    {
        public static TClient CreateClient<TClient>(string address)
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

            return (TClient)Activator.CreateInstance(typeof(TClient), channel)!;
        }
    }
}
