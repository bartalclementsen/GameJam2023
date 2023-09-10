using ImminentCrash.Server.Model;
using System.Text.Json;

namespace ImminentCrash.Server.Services
{
    public interface ICoinDataService
    {
        IEnumerable<CoinData> Get(DateOnly from, DateOnly to);
        DateOnly GetMaxDate();
        DateOnly GetMinDate();
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }

    public class CoinDataService : ICoinDataService
    {
        private IEnumerable<CoinData> _coinData = Enumerable.Empty<CoinData>();
        private DateOnly _minDate;
        private DateOnly _maxDate;
        private int index = 1;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            List<CoinData> coinDatas = new List<CoinData>();
            using Stream? reader = File.OpenRead("Data/CoinData.json");

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            JsonElement coinDataJson = await JsonSerializer.DeserializeAsync<JsonElement>(reader, options, cancellationToken: cancellationToken);

            foreach (JsonElement item2 in coinDataJson.EnumerateArray())
            {
                foreach (JsonProperty item in item2.EnumerateObject())
                {
                    string dateString = item.Name;
                    JsonElement coinValues = item.Value;
                    CoinData coinData = new()
                    {
                        Date = DateOnly.Parse(dateString),
                        BinanceCoin = GetDecimal(coinValues, "Binance Coin") * index,
                        Bitcoin = GetDecimal(coinValues, "Bitcoin") * index,
                        Cardano = GetDecimal(coinValues, "Cardano") * index,
                        Chainlink = GetDecimal(coinValues, "Chainlink") * index,
                        CryptoComCoin = GetDecimal(coinValues, "Crypto.com Coin") * index,
                        Dogecoin = GetDecimal(coinValues, "Dogecoin") * index,
                        Eos = GetDecimal(coinValues, "EOS") * index,
                        Ethereum = GetDecimal(coinValues, "Ethereum"),
                        Iota = GetDecimal(coinValues, "IOTA") * index,
                        Litecoin = GetDecimal(coinValues, "Litecoin") * index,
                        Monero = GetDecimal(coinValues, "Monero") * index,
                        Nem = GetDecimal(coinValues, "NEM") * index,
                        Stellar = GetDecimal(coinValues, "Stellar") * index,
                        Tether = GetDecimal(coinValues, "Tether") * index,
                        Tron = GetDecimal(coinValues, "TRON") * index,
                        Xrp = GetDecimal(coinValues, "XRP") * index
                    };
                    coinDatas.Add(coinData);
                }
            }
            _coinData = coinDatas;


            _minDate = _coinData.Min(cd => cd.Date);
            _maxDate = _coinData.Max(cd => cd.Date);

        }

        public DateOnly GetMinDate()
        {
            return _minDate;
        }

        public DateOnly GetMaxDate()
        {
            return _maxDate;
        }

        public IEnumerable<CoinData> Get(DateOnly from, DateOnly to)
        {
            return _coinData.Where(cd => cd.Date >= from && cd.Date <= to);
        }

        private decimal? GetDecimal(JsonElement element, string name)
        {
            if (element.TryGetProperty(name, out JsonElement property))
            {
                if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out decimal value))
                {
                    return value;
                }
            }

            return null;
        }


        
    }
}
