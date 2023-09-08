using ImminentCrash.Contracts;
using ImminentCrash.Contracts.Model;
using ProtoBuf.Grpc;

namespace ImminentCrash.Server.Services
{
    public class ImminentCrashService : IImminentCrashService
    {
        public Task<SayHelloResponse> SayHelloAsync(SayHelloRequest request, CallContext context = default)
        {
            return Task.FromResult( new SayHelloResponse
            {
                Message = $"Hello {request?.Name}"
            });
        }
    }
}
