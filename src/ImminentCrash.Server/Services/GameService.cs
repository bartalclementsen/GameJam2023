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
            var gameSession = _gameSessionFactory.Invoke();
            _sessions.Add(gameSession.Id, gameSession);

            var minDate = _coinDataService.GetMinDate();
            var maxDate = _coinDataService.GetMaxDate();
            var coinData = _coinDataService.Get(minDate, maxDate);

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

            var gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            return gameSession.RunAsync();
        }

        public Task<ContinueGameResponse> ContinueGameAsync(ContinueGameRequest request, CallContext context)
        {
            var gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            return gameSession.ContinueGameAsync(context.CancellationToken);
        }

        public Task<PauseGameResponse> PauseGameAsync(PauseGameRequest request, CallContext context)
        {
            var gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            return gameSession.PauseGameAsync(context.CancellationToken);

        }

        public async Task<QuitGameResponse> QuitGameAsync(QuitGameRequest request, CallContext context)
        {
            var gameSession = GetGameSessionByIdOrThrow(request.SessionId);
            await gameSession.QuitGameAsync(context.CancellationToken);

            _sessions.Remove(gameSession.Id);
            return new QuitGameResponse();
        }

        private IGameSession GetGameSessionByIdOrThrow(Guid sessionId)
        {

            if (_sessions.TryGetValue(sessionId, out var gameSession) == false)
            {
                string msg = $"No game found with session id {sessionId}";
                _logger.LogError(msg);
                throw new RpcException(new Status(StatusCode.NotFound, msg), msg);
            }

            return gameSession;
        }
    }
}
