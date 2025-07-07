using Grpc.Core.Interceptors;
using Grpc.Core;

namespace SurimiController
{
    public class ExceptionMetadataInterceptor : Interceptor
    {
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await continuation(request, context);
            }
            catch (RpcException ex)
            {
                if(ex.Trailers?.GetValue("method") == null || ex.Trailers?.GetValue("application") == null)
                {
                    // If the RpcException does not have metadata, we add it
                    // This is useful for logging and debugging purposes
                    var status = new Status(ex.StatusCode, ex.Message);
                    
                    // Create metadata with method and application name
                    var metadata = new Metadata
                    {
                        { "method", context.Method },
                        { "application", typeof(Program).Assembly.GetName().Name }
                    };
                    
                    throw new RpcException(status, metadata);
                }
                // If it's a RpcException from another model, we don't need to do anything special
                // meta data is allready set
                throw;
            }
            catch (Exception ex)
            {
                var status = new Status(StatusCode.Internal, ex.Message);
                var metadata = new Metadata
                {
                    { "method", context.Method },
                    { "application", typeof(Program).Assembly.GetName().Name }
                };
                throw new RpcException(status, metadata);
            }
        }
    }
}
