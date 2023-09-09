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
        // Fields
        public Guid Id { get; set; }

        private bool _isPaused = false;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly PeriodicTimer _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        private readonly ILogger<GameSession> _logger;
        private readonly GameSessionState _gameSessionState;

        // Constructor
        public GameSession(ILogger<GameSession> logger)
        {
            Id = Guid.NewGuid();
            _logger = logger;

            _gameSessionState = new GameSessionState
            {
                CurrentBalance = 1000,
            };
        }

        public async IAsyncEnumerable<GameEvent> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"{Id} {nameof(RunAsync)}");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            while (!_cancellationTokenSource.Token.IsCancellationRequested && await _periodicTimer.WaitForNextTickAsync(_cancellationTokenSource.Token))
            {
                await Task.Delay(TimeSpan.FromSeconds(1), _cancellationTokenSource.Token);
                if (_isPaused)
                {
                    // Skip this tick
                    continue;
                }

                // Apply tick
                _logger.LogTrace($"{Id} Send Event");

                // Generate costs for tick
                var balanceMovement = new List<BalanceMovement>();
                balanceMovement.Add(new BalanceMovement()
                {
                    Amount = -50,
                    Name = "Living Costs"
                });

                // Apply cost
                _gameSessionState.CurrentBalance += balanceMovement.Sum(o => o.Amount);

                // Send Game Event
                yield return new GameEvent
                {
                    IsDead = _gameSessionState.IsDead,
                    CurrentBalance = _gameSessionState.CurrentBalance,
                    BalanceMovements = balanceMovement
                };

                if (_gameSessionState.IsDead)
                {
                    break;  // Stop the loop
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

    public class GameSessionState
    {
        public bool IsDead => CurrentBalance < 0;

        public decimal CurrentBalance { get; set; }
    }
}
