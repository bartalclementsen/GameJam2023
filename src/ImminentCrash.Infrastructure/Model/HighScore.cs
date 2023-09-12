namespace ImminentCrash.Infrastructure.Model
{
    public class HighScore
    {
        public Guid Id { get; set; }

        public DateTime HighscoreTime { get; set; }

        public string Name { get; set; }

        public int DaysAlive { get; set; }

        public decimal CurrentBalance { get; set; }

        public decimal HighestBalance { get; set; }

        public bool IsDead { get; set; }
    }
}
