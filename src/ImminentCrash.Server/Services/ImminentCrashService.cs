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
            _gameService = gameService;
            _logger = logger;
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
    }
}
