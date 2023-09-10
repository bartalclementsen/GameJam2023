using ImminentCrash.Contracts;
using ImminentCrash.Contracts.Model;
using ProtoBuf.Grpc;

namespace ImminentCrash.Server.Services
{
    public class ImminentCrashService : IImminentCrashService
    {
        private readonly IGameService _gameService;
        private readonly ILogger<ImminentCrashService> _logger;

        public ImminentCrashService(ILogger<ImminentCrashService> logger, IGameService gameService)
        {
            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ContinueGameResponse> ContinueGameAsync(ContinueGameRequest request, CallContext context = default)
        {
            return _gameService.ContinueGameAsync(request, context);
        }

        public Task<CreateNewGameResponse> CreateNewGameAsync(CreateNewGameRequest request, CallContext context = default)
        {
            _logger.LogInformation($"{nameof(CreateNewGameAsync)} ({request})");
            return _gameService.CreateNewGameAsync(request, context);
        }

        public Task<PauseGameResponse> PauseGameAsync(PauseGameRequest request, CallContext context = default)
        {
            return _gameService.PauseGameAsync(request, context);
        }

        public Task<QuitGameResponse> QuitGameAsync(QuitGameRequest request, CallContext context = default)
        {
            return _gameService.QuitGameAsync(request, context);
        }

        public IAsyncEnumerable<GameEvent> StartNewGameAsync(StartGameRequest request, CallContext context)
        {
            _logger.LogInformation($"{nameof(StartNewGameAsync)} ({request})");
            return _gameService.StartGameAsync(request, context);
        }

        public Task<SellCoinsResponse> SellCoinsAsync(SellCoinRequest request, CallContext context = default)
        {
            return _gameService.SellCoinsAsync(request, context);
        }

        public Task<BuyCoinsResponse> BuyCoinsAsync(BuyCoinsRequest request, CallContext context = default)
        {
            return _gameService.BuyCoinsAsync(request, context);
        }

        public Task CreateHighscoreAsync(CreateHighscoreRequest request, CallContext context = default)
        {
            return _gameService.CreateHighscoreAsync(request, context);
        }

        public Task<GetTopHighscoresResponse> GetTopHighscoresAsync(GetTopHighscoresRequest request, CallContext context = default)
        {
            return _gameService.GetTopHighscoresAsync(request, context);
        }

        public Task<HighscoreResponse> GetHighscoreAsync(GetHighscoreRequest request, CallContext context = default)
        {
            return _gameService.GetHighscoreAsync(request, context.CancellationToken);
        }
    }
}