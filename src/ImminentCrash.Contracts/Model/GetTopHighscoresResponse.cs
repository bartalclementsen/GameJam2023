using System.Runtime.Serialization;

namespace ImminentCrash.Contracts.Model
{
    [DataContract]
    public class GetTopHighscoresResponse
    {
        [DataMember(Order = 1)]
        public List<HighscoreResponse> WinningHighscores { get; set; } = default!;

        [DataMember(Order = 2)]
        public List<HighscoreResponse> LoosingHighscores { get; set; } = default!;
    }

    [DataContract]
    public class HighscoreResponse
    {
        [DataMember(Order = 1)]
        public string HighscoreDate { get; set; } = default!;

        [DataMember(Order = 2)]
        public string Name { get; set; } = string.Empty;

        [DataMember(Order = 3)]
        public int DaysAlive { get; set; }

        [DataMember(Order = 4)]
        public decimal CurrentBalance { get; set; }

        [DataMember(Order = 5)]
        public decimal HighestBalance { get; set; }

        [DataMember(Order = 6)]
        public bool IsDead { get; set; }
    }
}