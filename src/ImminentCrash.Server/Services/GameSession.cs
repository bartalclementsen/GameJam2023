using ImminentCrash.Contracts.Model;
using ImminentCrash.Server.Model;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

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

        Task<HighscoreResponse> GetHighscoreAsync(CancellationToken cancellationToken);
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

        private readonly Dictionary<int, int> _coinAmounts = new();
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
                List<LivingCost> changedLivingCosts = new(); 
                List<CoinMovement> coinMovements = new();
                List<Contracts.Model.Coin> newCoins = new();
                List<Event> newEvents = new List<Event>();
                List<CoinAmount> coinAmounts = new List<CoinAmount>();

                ApplyBuyOrderFromPreviousDay(balanceMovement);

                ApplySellOrderFromPreviousDay(balanceMovement);

                // Apply tick
                _logger.LogTrace($"{Id} Send Event");
                _currentDate = _currentDate.AddDays(1);

                // Get living costs
                GenerateLivingCostForTicks(newLivingCosts, changedLivingCosts);

                
                // Generate Coin Movements
                //List<Contracts.Model.Coin> RemoveCoins = new List<Contracts.Model.Coin>();

                CoinData? coinData = null;
                if (_coinDataByDate.ContainsKey(_currentDate))
                {
                    coinData = _coinDataByDate[_currentDate];
                }

                GenerateCoinMovements(coinMovements, newCoins, coinData);

                // Generate Coin Amounts and
                foreach(var coinAmount in _coinAmounts)
                {
                    CoinMovement? foundMovement = coinMovements.FirstOrDefault(c => c.Id == coinAmount.Key);
                    var previousCoinAmount = _previousEvent?.CoinAmounts?.FirstOrDefault(c => c.CoinId == coinAmount.Key);

                    var currentCoinAmount = new CoinAmount()
                    {
                        CoinId = coinAmount.Key,
                        Amount = coinAmount.Value,
                        Value = foundMovement == null ? 0 : foundMovement.Amount * coinAmount.Value
                    };

                    if(previousCoinAmount != currentCoinAmount)
                    {
                        coinAmounts.Add(currentCoinAmount);
                    }
                }

                // Generate Event for tick
                GenerateEventsForTicks(newEvents, coinData, balanceMovement, newLivingCosts, changedLivingCosts);

                
                int daysDifference = _currentDate.DayNumber - _startDate.DayNumber;
                // Generate costs for tick
                foreach (LivingCost livingCost in livingCosts)
                {
                    if (livingCost.LivingCostType == LivingCostType.Daily)
                    {
                        balanceMovement.Add(new BalanceMovement()
                        {
                            Amount = livingCost.Amount * -1,
                            Name = livingCost.Name
                        });
                    }

                    if (livingCost.LivingCostType == LivingCostType.Weekly && daysDifference % 7 == 0)
                    {
                        balanceMovement.Add(new BalanceMovement()
                        {
                            Amount = livingCost.Amount * -1,
                            Name = livingCost.Name
                        });
                    }

                    if (livingCost.LivingCostType == LivingCostType.Monthly && daysDifference % 31 == 0)
                    {
                        balanceMovement.Add(new BalanceMovement()
                        {
                            Amount = livingCost.Amount * -1,
                            Name = livingCost.Name
                        });
                    }
                }

                if (_gameSessionState.CurrentBalance > _gameSessionState.HighestBalance)
                    _gameSessionState.HighestBalance = _gameSessionState.CurrentBalance;

                // Apply cost
                _gameSessionState.CurrentBalance += balanceMovement.Sum(o => o.Amount);

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
                    RemoveLivingCosts = null,
                    ChangedLivingCosts = changedLivingCosts.Any() ? changedLivingCosts : null,
                    NewEvents = newEvents.Any() ? newEvents : null,
                    CoinAmounts = coinAmounts.Any() ? coinAmounts : null,
                    IsWinner = _currentDate >= _endDate
                };

                // Send Game Event
                yield return gameEvent;

                _events.Add(_currentDate, gameEvent);
                _previousEvent = gameEvent;

                if (_gameSessionState.IsDead || gameEvent.IsWinner)
                    break;  // Stop the loop
            }
        }

        private void GenerateCoinMovements(List<CoinMovement> coinMovements, List<Contracts.Model.Coin> newCoins, CoinData? coinData)
        {
            if (coinData != null)
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
        }

        private void GenerateEventsForTicks(List<Event> newEvents, CoinData? coinData, List<BalanceMovement> balanceMovement, List<LivingCost> newLivingCosts, List<LivingCost> changedLivingCosts)
        {
            //IEnumerable<GameEvent> lastFiveGameEvents = _events.Values.TakeLast(5);
            // Check if a Event has been generated for the last five GameEvents
            //if (lastFiveGameEvents.Any(v => v.NewEvents != null) == false && coinData != null)
            //{
            //    (string highestIncreaseCoin, decimal highestIncreaseValue) = ReturnCoinWithLargestIncrease(coinData);
            //    if (highestIncreaseValue > 0.2m)
            //    {
            //        newEvents.Add(new Event() { Title = "Coin rising!", Details = $"{highestIncreaseCoin} is going to the moon!" });
            //    }
            //}

            int daysDifference = _currentDate.DayNumber - _startDate.DayNumber;
            
            if(daysDifference == 0)
            {
                newEvents.Add(new Event() { Title = "Game started!", Details = $"The player has entered the crpyto market!" });
            }

            if (daysDifference == 5)
            {
                newEvents.Add(new Event() { Title = "Freezer!", Details = $"Tú ert ein frystiboks! You felt a sudden urge to buy a freezer costing $1000." });
                balanceMovement.Add(new BalanceMovement()
                {
                    Amount = 1000 * -1,
                    Name = "Bought a freezer"
                });
            }

            if (daysDifference == 15)
            {
                newEvents.Add(new Event() { Title = "New Car!", Details = $"You bought a new can because the investing is doing so well! New monthly payment of $2000." });
                LivingCost car = new()
                {
                    Amount = 2000,
                    LivingCostType = LivingCostType.Monthly,
                    Name = "Car payments"
                };

                newLivingCosts.Add(car);
                livingCosts.Add(car);
            }

            if (daysDifference == 25)
            {
                newEvents.Add(new Event() { Title = "Message from Ex-Girlfriend!", Details = $"I'm pragnant! You need to buy diapers. They cost £500 dollars a week." });
                LivingCost car = new()
                {
                    Amount = 2000,
                    LivingCostType = LivingCostType.Weekly,
                    Name = "Diapers"
                };
            }

            if (daysDifference == 35)
            {
                newEvents.Add(new Event() { Title = "Birthday!", Details = $"It's your birthday today. Your grandmother gave you $2000 dollars." });
                balanceMovement.Add(new BalanceMovement()
                {
                    Amount = 2000,
                    Name = "Birthday"
                });
            }

            if (daysDifference == 50)
            {
                newEvents.Add(new Event() { Title = "Parking Ticket!", Details = $"You parked in a disabled spot and got a parking ticket for $200!" });
                balanceMovement.Add(new BalanceMovement()
                {
                    Amount = 200 * -1,
                    Name = "Parking Ticket"
                });
            }

            if (daysDifference == 60)
            {
                newEvents.Add(new Event() { Title = "Dentist!", Details = $"You needed to go to the dentist. The cost was $1500 dollars" });
                balanceMovement.Add(new BalanceMovement()
                {
                    Amount = 1500 * -1,
                    Name = "Dentist"
                });
            }

            if (daysDifference == 70)
            {
                newEvents.Add(new Event() { Title = "Computer!", Details = $"Oh no! Your gaming computer doesn't work anymore. A new Computer costs $10000." });
                balanceMovement.Add(new BalanceMovement()
                {
                    Amount = 10000 * -1,
                    Name = "Bought a freezer"
                });
            }
        }

        private void GenerateLivingCostForTicks(List<LivingCost> newLivingCosts, List<LivingCost> changedLivingCosts)
        {
            int daysDifference = _currentDate.DayNumber - _startDate.DayNumber;

            LivingCost daily = new()
            {
                Amount = 100,
                LivingCostType = LivingCostType.Daily,
                Name = "Daily costs"
            };

            LivingCost car = new()
            {
                Amount = 2000,
                LivingCostType = LivingCostType.Monthly,
                Name = "Rent payments"
            };

            if (livingCosts.Any(lc => lc.Name == "Daily costs") == false)
            {
                newLivingCosts.Add(daily);
                livingCosts.Add(daily);
            }
            else if (daysDifference % 7 == 0)
            {
                LivingCost tempDaily = livingCosts.First(lc => lc.Name == daily.Name);
                tempDaily = tempDaily with { Amount = decimal.Multiply(tempDaily.Amount, 1.1m) };
                livingCosts.RemoveAll(lc => lc.Name == daily.Name);
                livingCosts.Add(tempDaily);
                changedLivingCosts.Add(tempDaily);
            }

            if (livingCosts.Any(lc => lc.Name == "Rent payments") == false)
            {
                newLivingCosts.Add(car);
                livingCosts.Add(car);
            }
        }

        private void ApplySellOrderFromPreviousDay(List<BalanceMovement> balanceMovement)
        {
            List<CoinBuyOrder> buyOrders = _coinBuyOrders.ToList();
            _coinBuyOrders.Clear();

            foreach (CoinBuyOrder buyOrder in buyOrders)
            {
                if (_coinAmounts.ContainsKey(buyOrder.CoinMovement.Id) == false)
                {
                    _coinAmounts.Add(buyOrder.CoinMovement.Id, 0);
                }
                _coinAmounts[buyOrder.CoinMovement.Id] += buyOrder.Amount;

                balanceMovement.Add(new BalanceMovement
                {
                    Amount = buyOrder.Price * -1,
                    Name = $"Buy ${buyOrder.Amount} of coin"
                });
            }
        }

        private void ApplyBuyOrderFromPreviousDay(List<BalanceMovement> balanceMovement)
        {
            List<CoinSellOrder> sellOrders = _coinSellOrders.ToList();
            _coinSellOrders.Clear();
            foreach (CoinSellOrder? sellOrder in sellOrders)
            {
                if (_coinAmounts.ContainsKey(sellOrder.CoinMovement.Id) == false || _coinAmounts[sellOrder.CoinMovement.Id] < sellOrder.Amount)
                {
                    continue;
                }

                decimal price = sellOrder.CoinMovement.Amount * sellOrder.Amount;
                _coinAmounts[sellOrder.CoinMovement.Id] -= sellOrder.Amount;

                balanceMovement.Add(new BalanceMovement
                {
                    Amount = price,
                    Name = $"Sell ${sellOrder.Amount} of coin"
                });
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

        private (string, decimal) ReturnCoinWithLargestIncrease(CoinData coinData)
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

            var first = keyValuePairs.OrderByDescending(kvp => kvp.Value).First();
            return (first.Key, first.Value);
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

        public Task<HighscoreResponse> GetHighscoreAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new HighscoreResponse()
            {
                DaysAlive = (_currentDate.ToDateTime(new TimeOnly(12)) - _startDate.ToDateTime(new TimeOnly(12))).Days,
                CurrentBalance = _gameSessionState.CurrentBalance,
                HighestBalance = _gameSessionState.HighestBalance,
                IsDead = _gameSessionState.IsDead
            });
        }

        public record CoinBuyOrder(CoinMovement CoinMovement, int Amount, decimal Price);

        public record CoinSellOrder(CoinMovement CoinMovement, int Amount);

    }

    public class GameSessionState
    {
        public bool IsDead => CurrentBalance < 0;

        public decimal CurrentBalance { get; set; }

        public decimal HighestBalance { get; set; }
    }
}
