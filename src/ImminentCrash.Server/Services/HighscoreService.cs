using ImminentCrash.Contracts.Model;
using ImminentCrash.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using ProtoBuf.Grpc;

namespace ImminentCrash.Server.Services
{
    public interface IHighscoreService
    {
        Task CreateHighscoreAsync(CreateHighscoreRequest createHighscoreRequest, IGameSession gameSession, CancellationToken cancellationToken = default);

        Task<GetTopHighscoresResponse> GetTopHighscoresAsync(GetTopHighscoresRequest getTopHighscoresRequest, CancellationToken cancellationToken = default);
    }

    //public record Highscore(DateTime HighscoreTime, string Name, int DaysAlive, decimal CurrentBalance, decimal HighestBalance, bool IsDead);

    public class HighscoreService : IHighscoreService
    {
        private readonly ILogger<HighscoreService> _logger;
        private readonly IDbContextFactory<ImminentCrashDbContext> _dbContextFactory;

        public HighscoreService(ILogger<HighscoreService> logger, IDbContextFactory<ImminentCrashDbContext> dbContextFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task CreateHighscoreAsync(CreateHighscoreRequest request, IGameSession gameSession, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"MethodCalled({nameof(CreateHighscoreAsync)})");

            HighscoreResponse gameScore = await gameSession.GetHighscoreAsync(cancellationToken);

            using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            _dbContext.HighScores.Add(new Infrastructure.Model.HighScore
            {
                Id = Guid.NewGuid(),
                HighscoreTime = DateTime.Now,
                Name = request.Name,
                DaysAlive = gameScore.DaysAlive,
                CurrentBalance = gameScore.CurrentBalance,
                HighestBalance = gameScore.HighestBalance,
                IsDead = gameScore.IsDead
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<GetTopHighscoresResponse> GetTopHighscoresAsync(GetTopHighscoresRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"MethodCalled({nameof(GetTopHighscoresAsync)})");

            using var _dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            List<HighscoreResponse> winningHighscores = await _dbContext.HighScores
                .Where(e => e.IsDead == false)
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
                }).ToListAsync(cancellationToken);

            List<HighscoreResponse> loosingHighscores = await _dbContext.HighScores
                .Where(e => e.IsDead == true)
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
                }).ToListAsync(cancellationToken);

            return new GetTopHighscoresResponse() 
            {
                WinningHighscores = winningHighscores,
                LoosingHighscores = loosingHighscores
            };
        }
    }
}