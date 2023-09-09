using Azure.Core;
using Grpc.Core;
using ImminentCrash.Contracts.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;
using System.Threading.Channels;

namespace ImminentCrash.Server.Services
{
    public interface IGameService
    {
        Task<ContinueGameResponse> ContinueGameAsync(ContinueGameRequest request, CallContext context);

        Task<CreateNewGameResponse> CreateNewGameAsync(CreateNewGameRequest request, CallContext cancellationToken);

        Task<PauseGameResponse> PauseGameAsync(PauseGameRequest request, CallContext context);

        Task<QuitGameResponse> QuitGameAsync(QuitGameRequest request, CallContext context);

        IAsyncEnumerable<GameEvent> StartGameAsync(StartGameRequest request, CallContext context);

        Task<BuyCoinsResponse> BuyCoinsAsync(BuyCoinsRequest request, CallContext context);

        Task<SellCoinsResponse> SellCoinsAsync(SellCoinRequest request, CallContext context);
    }

    public class GameService : IGameService
    {
        private Dictionary<Guid, IGameSession> _sessions = new Dictionary<Guid, IGameSession>();

        private readonly ILogger<GameService> _logger;
        private readonly Func<GameSession> _gameSessionFactory;
        private readonly ICoinDataService _coinDataService;

        public GameService(ILogger<GameService> logger, Func<GameSession> gameSessionFactory, ICoinDataService coinDataService)
        {
            _logger = logger;
            _gameSessionFactory = gameSessionFactory;
            _coinDataService = coinDataService;
        }

        public Task<CreateNewGameResponse> CreateNewGameAsync(CreateNewGameRequest request, CallContext cancellationToken)
        {
            _logger.LogInformation($"{nameof(CreateNewGameAsync)} ({request})");

            // TODO: Maybe store in db?
            GameSession gameSession = _gameSessionFactory.Invoke();
            _sessions.Add(gameSession.Id, gameSession);

            DateOnly minDate = _coinDataService.GetMinDate();
            DateOnly maxDate = _coinDataService.GetMaxDate();
            IEnumerable<Model.CoinData> coinData = _coinDataService.Get(minDate, maxDate);

            gameSession.Initialize(
                startDate: minDate.AddDays(5),
                endDate: maxDate,
                coinData: coinData);

            return Task.FromResult(new CreateNewGameResponse
            {
                SessionId = gameSession.Id
            });
        }

        public IAsyncEnumerable<GameEvent> StartGameAsync(StartGameRequest request, CallContext context)
        {
            _logger.LogInformation($"{nameof(StartGameAsync)} ({request})");

            IGameSession gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            return gameSession.RunAsync();
        }

        public Task<ContinueGameResponse> ContinueGameAsync(ContinueGameRequest request, CallContext context)
        {
            IGameSession gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            return gameSession.ContinueGameAsync(context.CancellationToken);
        }

        public Task<PauseGameResponse> PauseGameAsync(PauseGameRequest request, CallContext context)
        {
            IGameSession gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            return gameSession.PauseGameAsync(context.CancellationToken);

        }

        public async Task<QuitGameResponse> QuitGameAsync(QuitGameRequest request, CallContext context)
        {
            IGameSession gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            await gameSession.QuitGameAsync(context.CancellationToken);

            _sessions.Remove(gameSession.Id);
            return new QuitGameResponse();
        }

        public async Task<BuyCoinsResponse> BuyCoinsAsync(BuyCoinsRequest request, CallContext context)
        {
            IGameSession gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            await gameSession.BuyCoinsAsync(request, context.CancellationToken);
            return new BuyCoinsResponse();
        }

        public async Task<SellCoinsResponse> SellCoinsAsync(SellCoinRequest request, CallContext context)
        {
            IGameSession gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            await gameSession.SellCoinsAsync(request, context.CancellationToken);
            return new SellCoinsResponse();
        }

        private IGameSession GetGameSessionByIdOrThrow(Guid sessionId)
        {

            if (_sessions.TryGetValue(sessionId, out IGameSession? gameSession) == false)
            {
                string msg = $"No game found with session id {sessionId}";
                _logger.LogError(msg);
                throw new RpcException(new Status(StatusCode.NotFound, msg), msg);
            }

            return gameSession;
        }
    }
}
