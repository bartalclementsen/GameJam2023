using ImminentCrash.Contracts;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using System;
using System.IO;
using ImminentCrash.Contracts.Model;
using Microsoft.Extensions.Logging;

namespace ImminentCrash.Client.Pages;

public partial class GamePage
{
    [Parameter] public Guid SessionId { get; set; } = default!;

    [Inject] protected ILogger<Index> Logger { get; set; } = default!;

    [Inject] public IImminentCrashService Client { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private bool _disposedValue;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

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
            await foreach (var gameEvent in stream)
            {
                Logger.LogInformation(gameEvent.ToString());

                if (gameEvent.IsDead)
                {
                    // TODO: Show dead screen
                    Logger.LogInformation("You are dead");
                    NavigationManager.NavigateTo("/");
                }
            }
        }
        catch(OperationCanceledException)
        {
            // Game quit
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handeling Game Event");
            throw;
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
    }

    private async void OnContinueGameClicked()
    {
        await Client.ContinueGameAsync(new ContinueGameRequest() { SessionId = SessionId }, _cancellationTokenSource.Token);
    }
}
