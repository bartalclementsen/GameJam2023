using ImminentCrash.Contracts.Model;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace ImminentCrash.Contracts
{
    [ServiceContract]
    public interface IImminentCrashService
    {
        [OperationContract]
        Task<CreateNewGameResponse> CreateNewGameAsync(CreateNewGameRequest request, CallContext context = default);

        [OperationContract]
        IAsyncEnumerable<GameEvent> StartNewGameAsync(StartGameRequest request, CallContext context = default);

        [OperationContract]
        Task<PauseGameResponse> PauseGameAsync(PauseGameRequest request, CallContext context = default);

        [OperationContract]
        Task<ContinueGameResponse> ContinueGameAsync(ContinueGameRequest request, CallContext context = default);

        [OperationContract]
        Task<QuitGameResponse> QuitGameAsync(QuitGameRequest request, CallContext context = default);
    }
}
