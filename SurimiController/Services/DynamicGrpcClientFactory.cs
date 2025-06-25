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
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true
                }
            });

            return (TClient)Activator.CreateInstance(typeof(TClient), channel)!;
        }
    }
}
