using ImminentCrash.Client.Models;
using ImminentCrash.Contracts.Model;
using pax.BlazorChartJs;
using System.Data;

namespace ImminentCrash.Client.Components
{
    public partial class LineChartComponent
    {
        /* Fields */
        ChartJsConfig chartJsConfig = default!;
        private bool chartReady;

        private int _daysToSeeInThePast = 10;
        private int _daysToSeeInTheFuture = 5;
        private DateOnly CurrentDate = default!;

        /* Overrides */
        protected override void OnInitialized()
        {
            chartJsConfig = new ChartJsConfig()
            {
                Type = ChartType.line,
                Data = new ChartJsData()
                {
                    Labels = new List<string>(),
                    Datasets = new List<ChartJsDataset>()
                },
                Options = new()
                {
                    Animation = false,
                    Scales = new ChartJsOptionsScales()
                    {
                        X = new ChartJsAxis()
                        {
                            Display = true,
                            Ticks = new ChartJsAxisTick()
                            {
                                Color = "black",
                                Font = new Font()
                                {
                                    Family = "'Helvetica Neue', Helvetica, Arial, sans-serif",
                                    Size = 12d,
                                }
                            },
                            Grid = new()
                            {
                                Display = false
                            }
                        },
                        Y = new ChartJsAxis()
                        {
                            Display = true,
                            Type = "logarithmic",
                            Ticks = new ChartJsAxisTick()
                            {
                                Color = "black",
                                Font = new Font()
                                {
                                    Family = "'Helvetica Neue', Helvetica, Arial, sans-serif",
                                    Size = 15d,
                                }
                            },
                            Grid = new()
                            {
                                TickColor = "black",
                                Color = "black",
                            }
                        },
                    },

                    Plugins = new()
                    {
                        Legend = new()
                        {
                            Display = false
                        },
                        ArbitraryLines = new List<ArbitraryLineConfig>()
                        {
                            new ArbitraryLineConfig()
                            {
                                ArbitraryLineColor = "black",
                                XPosition = _daysToSeeInThePast,
                                XWidth = 2,
                                Text = string.Empty
                            }
                        }
                    }
                }
            };

            // Add ur line :)
            //AddArbitraryLine();

            base.OnInitialized();
        }

        /* Public */
        public void OnGameEvent(GameEvent gameEvent)
        {
            CurrentDate = DateOnly.ParseExact(gameEvent.CurrentDateString, "dd.MM.yyyy");

            if (gameEvent.NewCoins?.Any() is true)
            {
                foreach (Coin coin in gameEvent.NewCoins)
                {
                    AddCoin((CoinType)coin.Id);
                }
            }

            if (gameEvent.CoinMovements?.Any() is true)
            {
                foreach (CoinMovement coinMovement in gameEvent.CoinMovements)
                {
                    AddCoinData((CoinType)coinMovement.Id, coinMovement.Amount);
                }
            }

            if (gameEvent.RemoveCoins?.Any() is true)
            {
                foreach (Coin removeCoin in gameEvent.RemoveCoins)
                {
                    RemoveCoin((CoinType)removeCoin.Id);
                }
            }

            Tick();
        }

        /* Private */
        private void Tick()
        {
            if (chartReady is not true)
                return;

            Dictionary<ChartJsDataset, SetDataObject> chartData = new();
            foreach (var dataset in chartJsConfig.Data.Datasets)
            {
                if (dataset is LineDataset lineDataSet)
                {
                    int itemsToRemove = lineDataSet.Data.Count - (int)(_daysToSeeInThePast + 1);
                    while (itemsToRemove-- > 0)
                        lineDataSet.Data.RemoveAt(0);

                    chartData[dataset] = new SetDataObject(lineDataSet.Data);
                }
            }

            // Update our Chart labels
            chartJsConfig.SetLabels(BuildDays(CurrentDate));

            // Update our Chart data
            chartJsConfig.SetData(chartData);

            chartJsConfig.ReinitializeChart();
        }

        private void AddCoin(CoinType coinType)
        {
            var emptyData = Enumerable.Range(0, _daysToSeeInThePast)
                .Select(e => (object)null)
                .Cast<object>()
                .ToList();

            chartJsConfig.Data.Datasets.Add(new LineDataset()
            {
                Label = CoinTypeToString(coinType),
                CubicInterpolationMode = "monotone",
                BorderColor = CoinColors.GetColor((int)coinType - 1),
                PointRadius = new IndexableOption<double>(0),
                Data = emptyData
            });
        }

        private void RemoveCoin(CoinType coinType)
        {
            var coinToRemove = chartJsConfig.Data.Datasets.FirstOrDefault(e => e.Type == ChartType.line && ((LineDataset)e).Label == CoinTypeToString(coinType));
            if (coinToRemove is null)
                return;

            chartJsConfig.Data.Datasets.Remove(coinToRemove);
        }

        private void AddCoinData(CoinType coinType, decimal dataEntry)
        {
            foreach (var dataset in chartJsConfig.Data.Datasets)
            {
                if (dataset is LineDataset lineDataSet)
                {
                    if (lineDataSet.Label == CoinTypeToString(coinType))
                        lineDataSet.Data.Add(Math.Round(dataEntry, 2));
                }
            }
        }

        private List<string> BuildDays(DateOnly date)
        {
            List<string> days = new();

            for (int i = _daysToSeeInThePast; i > 0; i--)
            {
                DateOnly offset = date.AddDays(-i);
                days.Add($"{offset.Day}/{offset.Month}");
            }

            days.Add($"{date.Day}/{date.Month}");

            for (int i = 0; i < _daysToSeeInTheFuture; ++i)
            {
                DateOnly offset = date.AddDays(i);
                days.Add($"{offset.Day}/{offset.Month}");
            }

            return days;
        }

        private List<string> GetPointStyle(CoinType coinType, IList<object> dataset)
        {
            if (dataset.Count < 1)
                return new();

            List<string> ret = new(dataset.Count);
            Enumerable.Range(0, dataset.Count() - 1).ToList().ForEach(e => ret.Add("false"));

            // For now until we figure out how to add Images to the PointStyle using C# we're just going to default to a triangle :-(
            ret.Add(coinType switch
            {
                _ => "triangle"
            });
            return ret;
        }

        private string CoinTypeToString(CoinType type) => type switch
        {
            CoinType.BinanceCoin => "Binance Coin",
            CoinType.Bitcoin => "Bitcoin",
            CoinType.Cardano => "Cardano",
            CoinType.Chainlink => "Chainlink",
            CoinType.CryptoComCoin => "Crypto.com Coin",
            CoinType.DogeCoin => "Doge Coin",
            CoinType.EOS => "EOS",
            CoinType.Ethereum => "Ethereum",
            CoinType.IOTA => "IOTA",
            CoinType.LiteCoin => "Lite coin",
            CoinType.Monero => "Monero",
            CoinType.NEM => "NEM",
            CoinType.Stellar => "Stellar Lumen",
            CoinType.Tether => "Tether",
            CoinType.TRON => "TRON",
            CoinType.XRP => "XRP",
            _ => throw new NotImplementedException()
        };

        private CoinType CoinTypeToEnum(string type) => type switch
        {
            "Binance Coin" => CoinType.BinanceCoin,
            "Bitcoin" => CoinType.Bitcoin,
            "Cardano" => CoinType.Cardano,
            "Chainlink" => CoinType.Chainlink,
            "Crypto.com Coin" => CoinType.CryptoComCoin,
            "Doge Coin" => CoinType.DogeCoin,
            "EOS" => CoinType.EOS,
            "Ethereum" => CoinType.Ethereum,
            "IOTA" => CoinType.IOTA,
            "Lite coin" => CoinType.LiteCoin,
            "Monero" => CoinType.Monero,
            "NEM" => CoinType.NEM,
            "Stellar Lumen" => CoinType.Stellar,
            "Tether" => CoinType.Tether,
            "TRON" => CoinType.TRON,
            "XRP" => CoinType.XRP,
            _ => throw new NotImplementedException()
        };

        /* Events */
        private void ChartEventTriggered(ChartJsEvent chartJsEvent)
        {
            if (chartJsEvent is ChartJsInitEvent initEvent)
            {
                chartReady = true;
            }
        }
    }
}
