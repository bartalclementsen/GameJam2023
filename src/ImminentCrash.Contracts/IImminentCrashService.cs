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

        [OperationContract]
        Task<BuyCoinsResponse> BuyCoinsAsync(BuyCoinsRequest request, CallContext context = default);

        [OperationContract]
        Task<SellCoinsResponse> SellCoinsAsync(SellCoinRequest request, CallContext context = default);

        [OperationContract]
        Task CreateHighscoreAsync(CreateHighscoreRequest createHighscoreRequest, CallContext context = default);

        [OperationContract]
        Task<HighscoreResponse> GetHighscoreAsync(GetHighscoreRequest getHighscoreRequest, CallContext context = default);

        [OperationContract]
        Task<GetTopHighscoresResponse> GetTopHighscoresAsync(GetTopHighscoresRequest getTopHighscoresRequest, CallContext context = default);
    }
}
