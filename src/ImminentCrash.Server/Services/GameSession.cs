﻿using ImminentCrash.Contracts.Model;
using ImminentCrash.Server.Model;
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

        private Dictionary<DateOnly, CoinData> _coinDataByDate = new();
        private DateOnly _startDate;
        private DateOnly _endDate;
        private DateOnly _currentDate;
        private bool _isPaused = false;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly PeriodicTimer _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        private readonly GameSessionState _gameSessionState;
        private readonly ILogger<GameSession> _logger;

        private GameEvent? _previousEvent;
        private Dictionary<DateOnly, GameEvent> _events = new Dictionary<DateOnly, GameEvent>();

        private List<LivingCost> livingCosts = new List<LivingCost>();

        // Constructor
        public GameSession(ILogger<GameSession> logger)
        {
            _logger = logger;

            Id = Guid.NewGuid();
            _gameSessionState = new GameSessionState
            {
                CurrentBalance = 1000,
            };
        }

        internal void Initialize(DateOnly startDate, DateOnly endDate, IEnumerable<CoinData> coinData)
        {
            _startDate = startDate;
            _endDate = endDate;
            _currentDate = startDate.AddDays(-1);
            _coinDataByDate = coinData.OrderBy(cd => cd.Date).GroupBy(cd => cd.Date).ToDictionary(cd => cd.Key, cd => cd.First());
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
                _currentDate = _currentDate.AddDays(1);

                // Get living costs
                LivingCost livingCost = new LivingCost()
                {
                    Amount = 20,
                    LivingCostType = LivingCostType.Daily,
                    Name = "Living Costs"
                };

                List<LivingCost> newLivingCosts = new List<LivingCost>();
                if (livingCosts.Contains(livingCost) == false)
                {
                    newLivingCosts.Add(livingCost);
                    livingCosts.Add(livingCost);
                }



                // Generate costs for tick


                var balanceMovement = new List<BalanceMovement>();
                balanceMovement.Add(new BalanceMovement()
                {
                    Amount = livingCost.Amount * -1,
                    Name = livingCost.Name
                });

                // Apply cost
                _gameSessionState.CurrentBalance += balanceMovement.Sum(o => o.Amount);

                // Generate Coin Movements
                List<CoinMovement> coinMovements = new List<CoinMovement>();
                List<Contracts.Model.Coin> newCoins = new List<Contracts.Model.Coin>();
                //List<Contracts.Model.Coin> RemoveCoins = new List<Contracts.Model.Coin>();

                if (_coinDataByDate.TryGetValue(_currentDate, out var coinData))
                {
                    if (coinData.BinanceCoin != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.BinanceCoin.Id, Amount = coinData.BinanceCoin.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.BinanceCoin.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.BinanceCoin.Id, Name = Model.Coin.BinanceCoin.Name });
                        }
                    }
                    if (coinData.Bitcoin != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Bitcoin.Id, Amount = coinData.Bitcoin.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Bitcoin.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Bitcoin.Id, Name = Model.Coin.Bitcoin.Name });
                        }
                    }
                    if (coinData.Cardano != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Cardano.Id, Amount = coinData.Cardano.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Cardano.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Cardano.Id, Name = Model.Coin.Cardano.Name });
                        }
                    }
                    if (coinData.Chainlink != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Chainlink.Id, Amount = coinData.Chainlink.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Chainlink.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Chainlink.Id, Name = Model.Coin.Chainlink.Name });
                        }
                    }
                    if (coinData.CryptoComCoin != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.CryptoComCoin.Id, Amount = coinData.CryptoComCoin.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.CryptoComCoin.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.CryptoComCoin.Id, Name = Model.Coin.CryptoComCoin.Name });
                        }
                    }
                    if (coinData.Dogecoin != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Dogecoin.Id, Amount = coinData.Dogecoin.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Dogecoin.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Dogecoin.Id, Name = Model.Coin.Dogecoin.Name });
                        }
                    }
                    if (coinData.Eos != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Eos.Id, Amount = coinData.Eos.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Eos.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Eos.Id, Name = Model.Coin.Eos.Name });
                        }
                    }
                    if (coinData.Ethereum != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Ethereum.Id, Amount = coinData.Ethereum.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Ethereum.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Ethereum.Id, Name = Model.Coin.Ethereum.Name });
                        }
                    }
                    if (coinData.Iota != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Iota.Id, Amount = coinData.Iota.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Iota.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Iota.Id, Name = Model.Coin.Iota.Name });
                        }
                    }
                    if (coinData.Litecoin != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Litecoin.Id, Amount = coinData.Litecoin.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Litecoin.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Litecoin.Id, Name = Model.Coin.Litecoin.Name });
                        }
                    }
                    if (coinData.Monero != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Monero.Id, Amount = coinData.Monero.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Monero.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Monero.Id, Name = Model.Coin.Monero.Name });
                        }
                    }
                    if (coinData.Nem != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Nem.Id, Amount = coinData.Nem.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Nem.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Nem.Id, Name = Model.Coin.Nem.Name });
                        }
                    }
                    if (coinData.Stellar != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Stellar.Id, Amount = coinData.Stellar.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Stellar.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Stellar.Id, Name = Model.Coin.Stellar.Name });
                        }
                    }
                    if (coinData.Tether != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Tether.Id, Amount = coinData.Tether.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Tether.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Tether.Id, Name = Model.Coin.Tether.Name });
                        }
                    }
                    if (coinData.Tron != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Tron.Id, Amount = coinData.Tron.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Tron.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Tron.Id, Name = Model.Coin.Tron.Name });
                        }
                    }
                    if (coinData.Xrp != null)
                    {
                        coinMovements.Add(new CoinMovement { Id = Model.Coin.Xrp.Id, Amount = coinData.Xrp.Value });
                        // Was not in last event
                        if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Xrp.Id) != true)
                        {
                            newCoins.Add(new Contracts.Model.Coin() { Id = Model.Coin.Xrp.Id, Name = Model.Coin.Xrp.Name });
                        }
                    }
                }

                var gameEvent = new GameEvent
                {
                    IsDead = _gameSessionState.IsDead,
                    CurrentBalance = _gameSessionState.CurrentBalance,
                    BalanceMovements = balanceMovement,
                    CurrentDateString = _currentDate.ToString(),
                    CoinMovements = coinMovements.Any() ? coinMovements : null,
                    NewCoins = newCoins.Any() ? newCoins : null,
                    RemoveCoins = null,
                    NewLivingCosts = newLivingCosts.Any() ? newLivingCosts : null
                };

                // Send Game Event
                yield return gameEvent;

                _events.Add(_currentDate, gameEvent);
                _previousEvent = gameEvent;

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
