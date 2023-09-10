using ImminentCrash.Contracts;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using System;
using System.IO;
using ImminentCrash.Contracts.Model;
using Microsoft.Extensions.Logging;
using Grpc.Core;
using ImminentCrash.Client.Components;
using static ImminentCrash.Client.Components.CoinOverviewComponent;

namespace ImminentCrash.Client.Pages;

public partial class GamePage
{
    [Parameter] public Guid SessionId { get; set; } = default!;

    [Inject] protected ILogger<Index> Logger { get; set; } = default!;

    [Inject] public IImminentCrashService Client { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private bool _isPaused;

    private bool _isDead;

    private bool _isWinner;

    private bool _disposedValue;

    private string _highScoreName = default!;

    private HighscoreResponse? _highscoreResponse;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private EventOverviewComponent EventOverviewComponentRef = default!;
    private CoinOverviewComponent CoinOverviewComponentRef = default!;
    private LivingCostComponent LivingCostComponentRef = default!;
    private BoosterOverviewComponent BoosterOverviewComponentRef = default!;
    private BalanceComponent BalanceComponentRef = default!;
    private LineChartComponent LineChartComponentRef = default!;

    // Public
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    // Protected
    protected override void OnInitialized()
    {
        Logger.LogInformation("Page Initialized");

        Logger.LogInformation("Starting game");
        IAsyncEnumerable<GameEvent> stream = Client.StartNewGameAsync(new StartGameRequest
        {
            SessionId = SessionId,
        }, _cancellationTokenSource.Token);

        Logger.LogInformation("Game started");

        HandleStream(stream);
    }

    protected override Task OnInitializedAsync()
    {
        Logger.LogInformation("Trying to start game");

        return base.OnInitializedAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }
            }

            _disposedValue = true;
        }
    }

    // Private
    private async void HandleStream(IAsyncEnumerable<GameEvent> stream)
    {
        Logger.LogInformation("Handeling stream");

        try
        {
            await foreach (GameEvent gameEvent in stream)
            {
                Logger.LogInformation(gameEvent.ToString());

                await HandleEvent(gameEvent);

                if (gameEvent.IsDead)
                {
                    // TODO: Show dead screen
                    Logger.LogInformation("You are dead");
                    //NavigationManager.NavigateTo("/");
                }
            }
        }
        catch(RpcException rpcException)
        {
            // Game quit
            Logger.LogError(rpcException, "Error handeling Game Event");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handeling Game Event");
            throw;
        }
    }

    private async Task HandleEvent(GameEvent gameEvent)
    {
        EventOverviewComponentRef.HandleNewGameEvent(gameEvent);
        CoinOverviewComponentRef.HandleNewGameEvent(gameEvent);
        LivingCostComponentRef.HandleNewGameEvent(gameEvent);
        BoosterOverviewComponentRef.HandleNewGameEvent(gameEvent);
        BalanceComponentRef.HandleNewGameEvent(gameEvent);
        LineChartComponentRef.OnGameEvent(gameEvent);

        if (gameEvent.IsDead)
            _isDead = true;

        if (gameEvent.IsWinner == true)
            _isWinner = true;

        if(_isDead || _isWinner)
        {
            _highscoreResponse = await Client.GetHighscoreAsync(new GetHighscoreRequest()
            {
                SessionId = SessionId
            });

            StateHasChanged();
        }
    }

    private async void OnQuitGameClicked()
    {
        await Client.QuitGameAsync(new QuitGameRequest() { SessionId = SessionId }, _cancellationTokenSource.Token);
        NavigationManager.NavigateTo("/");
    }

    private async void OnPauseGameClicked()
    {
        await Client.PauseGameAsync(new PauseGameRequest() { SessionId = SessionId }, _cancellationTokenSource.Token);
        _isPaused = true;
        StateHasChanged();
    }

    private async void OnContinueGameClicked()
    {
        await Client.ContinueGameAsync(new ContinueGameRequest() { SessionId = SessionId }, _cancellationTokenSource.Token);
        _isPaused = false;
        StateHasChanged();
    }

    private async void OnBuy(CoinOrder order)
    {
        await Client.BuyCoinsAsync(new BuyCoinsRequest
        {
            Amount = order.Amount,
            CoinId = order.CoinId,
            SessionId = SessionId
        });
    }

    private async void OnSell(CoinOrder order)
    {
        await Client.SellCoinsAsync(new SellCoinRequest
        {
            Amount = order.Amount,
            CoinId = order.CoinId,
            SessionId = SessionId
        });
    }

    private async void OnSaveHighScore()
    {
        if(_isDead == false && _isWinner == false)
        {
            // Can not save unless dead or winner
            return;
        }

        if (string.IsNullOrWhiteSpace(_highScoreName) || _highScoreName.Length < 1)
        {
            // send some alert stating name needed
            return;
        }

        await Client.CreateHighscoreAsync(new CreateHighscoreRequest()
        {
            SessionId = SessionId,
            Name = _highScoreName
        });

        NavigationManager.NavigateTo("/HighScore");
    }

    private void OnBackToMainMenuClicked()
    {
        NavigationManager.NavigateTo("/");
    }
}
