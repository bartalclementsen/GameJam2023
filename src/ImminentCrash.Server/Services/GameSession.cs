using ImminentCrash.Contracts.Model;
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

        Task SellCoinsAsync(SellCoinRequest request, CancellationToken cancellationToken);

        Task BuyCoinsAsync(BuyCoinsRequest request, CancellationToken cancellationToken);
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
        private CancellationTokenSource _cancellationTokenSource = new();

        private readonly Dictionary<int, int> _coinOwnage = new();
        private readonly List<CoinBuyOrder> _coinBuyOrders = new();
        private readonly List<CoinSellOrder> _coinSellOrders = new();

        private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(1));
        private readonly GameSessionState _gameSessionState;
        private readonly ILogger<GameSession> _logger;

        private GameEvent? _previousEvent;
        private readonly Dictionary<DateOnly, GameEvent> _events = new();

        private readonly List<LivingCost> livingCosts = new();

        // Constructor
        public GameSession(ILogger<GameSession> logger)
        {
            _logger = logger;

            Id = Guid.NewGuid();
            _gameSessionState = new GameSessionState
            {
                CurrentBalance = 50000,
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
                if (_isPaused)
                {
                    // Skip this tick
                    continue;
                }

                List<BalanceMovement> balanceMovement = new();
                List<LivingCost> newLivingCosts = new();
                List<CoinMovement> coinMovements = new();
                List<Contracts.Model.Coin> newCoins = new();

                // Apply By Order from previous Day
                List<CoinSellOrder> sellOrders = _coinSellOrders.ToList();
                _coinSellOrders.Clear();
                foreach (CoinSellOrder? sellOrder in sellOrders)
                {
                    if (_coinOwnage.ContainsKey(sellOrder.CoinMovement.Id) == false || _coinOwnage[sellOrder.CoinMovement.Id] < sellOrder.Amount)
                    {
                        continue;
                    }

                    decimal price = sellOrder.CoinMovement.Amount * sellOrder.Amount;
                    _coinOwnage[sellOrder.CoinMovement.Id] -= sellOrder.Amount;

                    balanceMovement.Add(new BalanceMovement
                    {
                        Amount = price,
                        Name = ""
                    });
                }


                List<CoinBuyOrder> buyOrders = _coinBuyOrders.ToList();
                _coinBuyOrders.Clear();

                foreach (CoinBuyOrder buyOrder in buyOrders)
                {
                    if (_coinOwnage.ContainsKey(buyOrder.CoinMovement.Id) == false)
                    {
                        _coinOwnage.Add(buyOrder.CoinMovement.Id, 0);
                    }
                    _coinOwnage[buyOrder.CoinMovement.Id] += buyOrder.Amount;

                    balanceMovement.Add(new BalanceMovement
                    {
                        Amount = buyOrder.Price * -1,
                        Name = ""
                    });
                }

                // Apply tick
                _logger.LogTrace($"{Id} Send Event");
                _currentDate = _currentDate.AddDays(1);

                // Get living costs
                LivingCost livingCost = new()
                {
                    Amount = 20,
                    LivingCostType = LivingCostType.Daily,
                    Name = "Living Costs"
                };

                if (livingCosts.Contains(livingCost) == false)
                {
                    newLivingCosts.Add(livingCost);
                    livingCosts.Add(livingCost);
                }

                // Generate costs for tick
                balanceMovement.Add(new BalanceMovement()
                {
                    Amount = livingCost.Amount * -1,
                    Name = livingCost.Name
                });

                // Apply cost
                _gameSessionState.CurrentBalance += balanceMovement.Sum(o => o.Amount);

                // Generate Coin Movements
                //List<Contracts.Model.Coin> RemoveCoins = new List<Contracts.Model.Coin>();

                if (_coinDataByDate.TryGetValue(_currentDate, out CoinData? coinData))
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

                // Generate Event for tick
                List<Event> newEvents = new List<Event>();
                IEnumerable<GameEvent> lastFiveGameEvents = _events.Values.TakeLast(5);
                // Check if a Event has been generated for the last five GameEvents
                if (lastFiveGameEvents.Any(v => v.NewEvents != null) == false && coinData != null)
                {
                    (string highestIncreaseCoin, decimal highestIncreaseValue) = await ReturnCoinWithLargestIncrease(coinData);
                    if (highestIncreaseValue > 0.2m)
                    {
                        newEvents.Add(new Event() { Title = "Coin rising!", Details = $"{highestIncreaseCoin} is going to the moon!" });
                    }
                }

                
                GameEvent gameEvent = new()
                {
                    IsDead = _gameSessionState.IsDead,
                    CurrentBalance = _gameSessionState.CurrentBalance,
                    BalanceMovements = balanceMovement,
                    CurrentDateString = _currentDate.ToString("dd.MM.yyyy"),
                    CoinMovements = coinMovements.Any() ? coinMovements : null,
                    NewCoins = newCoins.Any() ? newCoins : null,
                    RemoveCoins = null,
                    NewLivingCosts = newLivingCosts.Any() ? newLivingCosts : null,
                    NewEvents = newEvents.Any() ? newEvents : null,
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

        private Task<(string, decimal)> ReturnCoinWithLargestIncrease(CoinData coinData)
        {
            Dictionary<string, decimal> keyValuePairs = new Dictionary<string, decimal>();

            if (coinData.BinanceCoin != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.BinanceCoin.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.BinanceCoin.Name, coinData.BinanceCoin.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.BinanceCoin.Id).Amount);
                }
            }
            if (coinData.Bitcoin != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Bitcoin.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Bitcoin.Name, coinData.Bitcoin.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Bitcoin.Id).Amount);
                }
            }
            if (coinData.Cardano != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Cardano.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Cardano.Name, coinData.Cardano.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Cardano.Id).Amount);
                }
            }
            if (coinData.Chainlink != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Chainlink.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Chainlink.Name, coinData.Chainlink.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Chainlink.Id).Amount);
                }
            }
            if (coinData.CryptoComCoin != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.CryptoComCoin.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.CryptoComCoin.Name, coinData.CryptoComCoin.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.CryptoComCoin.Id).Amount);
                }
            }
            if (coinData.Dogecoin != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Dogecoin.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Dogecoin.Name, coinData.Dogecoin.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Dogecoin.Id).Amount);
                }
            }
            if (coinData.Eos != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Eos.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Eos.Name, coinData.Eos.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Eos.Id).Amount);
                }
            }
            if (coinData.Ethereum != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Ethereum.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Ethereum.Name, coinData.Ethereum.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Ethereum.Id).Amount);
                }
            }
            if (coinData.Iota != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Iota.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Iota.Name, coinData.Iota.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Iota.Id).Amount);
                }
            }
            if (coinData.Litecoin != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Litecoin.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Litecoin.Name, coinData.Litecoin.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Litecoin.Id).Amount);
                }
            }
            if (coinData.Monero != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Monero.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Monero.Name, coinData.Monero.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Monero.Id).Amount);
                }
            }
            if (coinData.Nem != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Nem.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Nem.Name, coinData.Nem.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Nem.Id).Amount);
                }
            }
            if (coinData.Stellar != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Stellar.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Stellar.Name, coinData.Stellar.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Stellar.Id).Amount);
                }
            }
            if (coinData.Tether != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Tether.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Tether.Name, coinData.Tether.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Tether.Id).Amount);
                }
            }
            if (coinData.Tron != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Tron.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Tron.Name, coinData.Tron.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Tron.Id).Amount);
                }
            }
            if (coinData.Xrp != null)
            {
                if (_previousEvent?.CoinMovements?.Any(c => c.Id == Model.Coin.Xrp.Id) != true)
                {
                    keyValuePairs.Add(Model.Coin.Xrp.Name, coinData.Xrp.Value - _previousEvent!.CoinMovements!.First(cm => cm.Id == Model.Coin.Xrp.Id).Amount);
                }
            }

            return Task.FromResult(keyValuePairs.OrderByDescending(kvp => kvp.Value).First().Key);
        }

        public Task SellCoinsAsync(SellCoinRequest request, CancellationToken cancellationToken)
        {
            if (_previousEvent == null)
            {
                return Task.CompletedTask;
            }

            CoinMovement? coinMovement = _previousEvent.CoinMovements?.FirstOrDefault(cm => cm.Id == request.CoinId);

            if (coinMovement == null)
            {
                return Task.CompletedTask;
            }

            _coinSellOrders.Add(new CoinSellOrder(coinMovement, request.Amount));
            return Task.CompletedTask;
        }

        public Task BuyCoinsAsync(BuyCoinsRequest request, CancellationToken cancellationToken)
        {
            if (_previousEvent == null)
            {
                return Task.CompletedTask;
            }

            CoinMovement? coinMovement = _previousEvent.CoinMovements?.FirstOrDefault(cm => cm.Id == request.CoinId);

            if (coinMovement == null)
            {
                return Task.CompletedTask;
            }

            decimal price = coinMovement.Amount * request.Amount;
            _coinBuyOrders.Add(new CoinBuyOrder(coinMovement, request.Amount, price));

            return Task.CompletedTask;
        }

        public record CoinBuyOrder(CoinMovement CoinMovement, int Amount, decimal Price);

        public record CoinSellOrder(CoinMovement CoinMovement, int Amount);

    }

    public class GameSessionState
    {
        public bool IsDead => CurrentBalance < 0;

        public decimal CurrentBalance { get; set; }
    }
}
