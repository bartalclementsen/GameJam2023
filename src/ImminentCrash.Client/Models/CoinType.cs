using static System.Net.Mime.MediaTypeNames;
using System.ServiceModel.Channels;

namespace ImminentCrash.Client.Models
{
    public enum CoinType
    {
        BinanceCoin = 1,
        Bitcoin = 2,
        Cardano = 3,
        Chainlink = 4,
        CryptoComCoin = 5,
        DogeCoin = 6,
        EOS = 7,
        Ethereum = 8,
        IOTA = 9,
        LiteCoin = 10,
        Monero = 11,
        NEM = 12,
        Stellar = 13,
        Tether = 14,
        TRON = 15,
        XRP = 16
    }

    public static class CoinColors
    {
        /*
        private static List<string> colors = new List<string>
        {
            "#FF6384",
            "#36A2EB",
            "#FFCE56",
            "#4BC0C0",
            "#9966FF",
            "#FF9F40",
            "#FFCD56",
            "#C9CBCF",
            "#FF9999",
            "#66FF66",
            "#FF5A7A",
            "#2D92D7",
            "#FFD166",
            "#3CC8C8",
            "#8D58E5",
            "#FF8D30",
            "#FFDA66",
            "#B0B3C1",
            "#FF8989",
            "#59FF59"
        };
        */

        private static List<string> colors = new List<string>
        {
            "#F0B90B",  // BinanceCoin
            "#F7931A",  // Bitcoin
            "#2CCACD",  // Cardano
            "#375FA1",  // Chainlink
            "#103F68",  // CryptoComCoin
            "#BA9F33",  // DogeCoin
            "#000000",  // EOS
            "#3C3C3D",  // Ethereum
            "#0050E9",  // IOTA
            "#A4A9B3",  // LiteCoin
            "#FF6600",  // Monero
            "#414141",  // NEM
            "#07B5E5",  // Stellar
            "#26A17B",  // Tether
            "#121B74",  // TRON
            "#23292F",  // XRP
            "#1E2A35",  // Additional Color 1
            "#33495E",  // Additional Color 2
            "#506C7F",  // Additional Color 3
            "#6C8FA1"   // Additional Color 4
        };

        public static string GetColor(int index)
        {
            return colors[(index % colors.Count)];
        }
    }
}
