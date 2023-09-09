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
                        BinanceCoin = GetDecimal(coinValues, "Binance Coin"),
                        Bitcoin = GetDecimal(coinValues, "Bitcoin"),
                        Cardano = GetDecimal(coinValues, "Cardano"),
                        Chainlink = GetDecimal(coinValues, "Chainlink"),
                        CryptoComCoin = GetDecimal(coinValues, "Crypto.com Coin"),
                        Dogecoin = GetDecimal(coinValues, "Dogecoin"),
                        Eos = GetDecimal(coinValues, "EOS"),
                        Ethereum = GetDecimal(coinValues, "Ethereum"),
                        Iota = GetDecimal(coinValues, "IOTA"),
                        Litecoin = GetDecimal(coinValues, "Litecoin"),
                        Monero = GetDecimal(coinValues, "Monero"),
                        Nem = GetDecimal(coinValues, "NEM"),
                        Stellar = GetDecimal(coinValues, "Stellar"),
                        Tether = GetDecimal(coinValues, "Tether"),
                        Tron = GetDecimal(coinValues, "TRON"),
                        Xrp = GetDecimal(coinValues, "XRP")
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
