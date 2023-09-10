using ImminentCrash.Contracts.Model;
using ProtoBuf.Grpc;

namespace ImminentCrash.Server.Services
{
    public interface IHighscoreService
    {
        Task CreateHighscoreAsync(CreateHighscoreRequest createHighscoreRequest, IGameSession gameSession, CancellationToken cancellationToken = default);

        Task<GetTopHighscoresResponse> GetTopHighscoresAsync(GetTopHighscoresRequest getTopHighscoresRequest, CancellationToken cancellationToken = default);
    }

    public record Highscore(DateTime HighscoreTime, string Name, int DaysAlive, decimal CurrentBalance, decimal HighestBalance, bool IsDead);

    public class HighscoreService : IHighscoreService
    {
        private readonly ILogger<HighscoreService> _logger;
        private readonly List<Highscore> _highscores;

        public HighscoreService(ILogger<HighscoreService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _highscores = new List<Highscore>();
            //SeedData();
        }

        public async Task CreateHighscoreAsync(CreateHighscoreRequest request, IGameSession gameSession, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"MethodCalled({nameof(CreateHighscoreAsync)})");

            var gameScore = await gameSession.GetHighscoreAsync(cancellationToken);

            _highscores.Add(new Highscore(
                HighscoreTime: DateTime.Now,
                Name: request.Name, 
                DaysAlive: gameScore.DaysAlive, 
                CurrentBalance: gameScore.CurrentBalance, 
                HighestBalance: gameScore.HighestBalance, 
                IsDead: gameScore.IsDead
            ));
        }

        public Task<GetTopHighscoresResponse> GetTopHighscoresAsync(GetTopHighscoresRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"MethodCalled({nameof(GetTopHighscoresAsync)})");

            return Task.FromResult(new GetTopHighscoresResponse() 
            {
                WinningHighscores = 
                    _highscores.Where(e => e.IsDead == false)
                    .OrderByDescending(e => e.DaysAlive)
                    .Take(10)
                    .Select(e => new HighscoreResponse()
                    {
                        HighscoreDate = e.HighscoreTime.ToString("dd/MM/yyyy HH:mm"),
                        Name = e.Name,
                        DaysAlive = e.DaysAlive,
                        CurrentBalance = Math.Round(e.CurrentBalance, 2),
                        HighestBalance = Math.Round(e.HighestBalance, 2),
                        IsDead = e.IsDead
                    }).ToList(),
                LoosingHighscores =
                    _highscores.Where(e => e.IsDead == true)
                    .OrderByDescending(e => e.DaysAlive)
                    .Take(10)
                    .Select(e => new HighscoreResponse()
                    {
                        HighscoreDate = e.HighscoreTime.ToString("dd/MM/yyyy HH:mm"),
                        Name = e.Name,
                        DaysAlive = e.DaysAlive,
                        CurrentBalance = Math.Round(e.CurrentBalance, 2),
                        HighestBalance = Math.Round(e.HighestBalance, 2),
                        IsDead = e.IsDead
                    }).ToList()
            });
        }

        private void SeedData()
        {
            for(int i = 0; i < 20; i++)
            {
                int random = Random.Shared.Next(-10, 10);
                _highscores.Add(new Highscore(DateTime.Now.AddMinutes(random), $"Player{random}", Math.Abs(random* Random.Shared.Next(1, 100)), 50000, 60000, i % 2 == 0));
            }
        }
    }
}