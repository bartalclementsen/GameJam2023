using ImminentCrash.Contracts.Model;
using System.Runtime.CompilerServices;

namespace ImminentCrash.Server.Services
{
    public interface IGameSession
    {
        Guid Id { get; }

        Task<ContinueGameResponse> ContinueGameAsync(CancellationToken cancellationToken);

        Task<PauseGameResponse> PauseGameAsync(CancellationToken cancellationToken);

        Task QuitGameAsync(CancellationToken cancellationToken);

        IAsyncEnumerable<GameEvent> RunAsync(CancellationToken cancellationToken = default);
    }

    public class GameSession : IGameSession
    {
        public Guid Id { get; set; }

        private ILogger<GameSession> _logger;


        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private bool _isPaused = false;

        public GameSession(ILogger<GameSession> logger)
        {
            Id = Guid.NewGuid();
            _logger = logger;
        }

        public async IAsyncEnumerable<GameEvent> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"{Id} {nameof(RunAsync)}");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), _cancellationTokenSource.Token);

                if (!_isPaused)
                {
                    _logger.LogTrace($"{Id} Send Event");
                    yield return new GameEvent { Time = DateTime.UtcNow };
                }
                else
                {
                    _logger.LogTrace($"{Id} PAUSED no send event");
                }
            }
        }

        public Task<ContinueGameResponse> ContinueGameAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"{Id} {nameof(ContinueGameAsync)}");

            _isPaused = false;
            return Task.FromResult(new ContinueGameResponse());
        }

        public Task<PauseGameResponse> PauseGameAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"{Id} {nameof(PauseGameAsync)}");

            _isPaused = true;
            return Task.FromResult(new PauseGameResponse());
        }

        public Task QuitGameAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug($"{Id} {nameof(QuitGameAsync)}");

            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
