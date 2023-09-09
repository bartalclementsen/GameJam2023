namespace ImminentCrash.Server.Model
{
    public record CoinData
    {
        public DateOnly Date { get; init; }

        public decimal? BinanceCoin { get; init; }

        public decimal? Bitcoin { get; init; }

        public decimal? Cardano { get; init; }

        public decimal? Chainlink { get; init; }

        public decimal? CryptoComCoin { get; init; }

        public decimal? Dogecoin { get; init; }

        public decimal? Eos { get; init; }

        public decimal? Ethereum { get; init; }

        public decimal? Iota { get; init; }

        public decimal? Litecoin { get; init; }

        public decimal? Monero { get; init; }

        public decimal? Nem { get; init; }

        public decimal? Stellar { get; init; }

        public decimal? Tether { get; init; }

        public decimal? Tron { get; init; }

        public decimal? Xrp { get; init; }
    }
}
