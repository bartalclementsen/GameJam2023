using System.Reflection;

namespace ImminentCrash.Server.Model
{
    public record Coin : Enumeration
    {

        public static Coin BinanceCoin => new Coin(1, "Binance Coin");

        public static Coin Bitcoin => new Coin(2, "Bitcoin");

        public static Coin Cardano => new Coin(3, "Cardano");

        public static Coin Chainlink => new Coin(4, "Chainlink");

        public static Coin CryptoComCoin => new Coin(5, "Crypto.com Coin");

        public static Coin Dogecoin => new Coin(5, "Dogecoin");

        public static Coin Eos => new Coin(6, "EOS");

        public static Coin Ethereum => new Coin(7, "Ethereum");

        public static Coin Iota => new Coin(8, "IOTA");

        public static Coin Litecoin => new Coin(8, "Litecoin");

        public static Coin Monero => new Coin(9, "Monero");

        public static Coin Nem => new Coin(10, "NEM");

        public static Coin Stellar => new Coin(11, "Stellar");

        public static Coin Tether => new Coin(12, "Tether");

        public static Coin Tron => new Coin(13, "TRON");

        public static Coin Xrp => new Coin(14, "XRP");

        public Coin(int id, string name) : base(id, name) { }
    }

    public abstract record Enumeration 
    {
        public string Name { get; private set; }

        public int Id { get; private set; }

        protected Enumeration(int id, string name) => (Id, Name) = (id, name);

        public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
            typeof(T).GetFields(BindingFlags.Public |
                                BindingFlags.Static |
                                BindingFlags.DeclaredOnly)
                     .Select(f => f.GetValue(null))
                     .Cast<T>();
    }
}
