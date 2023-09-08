using ImminentCrash.Contracts.Model;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace ImminentCrash.Contracts
{
    [ServiceContract]
    public interface IImminentCrashService
    {
        [OperationContract]
        Task<SayHelloResponse> SayHelloAsync(SayHelloRequest request, CallContext context = default);

    }
}
