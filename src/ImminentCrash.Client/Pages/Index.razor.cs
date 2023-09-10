using ImminentCrash.Contracts;
using ImminentCrash.Contracts.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Reflection;

namespace ImminentCrash.Client.Pages;

public partial class Index : IDisposable
{
    [Inject] protected ILogger<Index> Logger { get; set; } = default!;

    [Inject] public IImminentCrashService Client { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    private bool hasEnabledAudio = false;


    private bool _disposedValue;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private string _version = "Unknown";

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
        _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
    }

    protected override Task OnInitializedAsync()
    {
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

    // Events
    public async void OnStartClicked()
    {
        Logger.LogInformation("Trying to create game");
        CreateNewGameResponse gameSession = await Client.CreateNewGameAsync(new Contracts.Model.CreateNewGameRequest(), _cancellationTokenSource.Token);

        Logger.LogInformation("Game created. Navigating to Game Page");
        NavigationManager.NavigateTo("/Game/" + gameSession.SessionId);
    }

    public void OnHighScoreClicked()
    {
        Logger.LogInformation("Navigating to HighScore");
        NavigationManager.NavigateTo("/HighScore");
    }

    public async void OnEnableAudio()
    {
        hasEnabledAudio = true;
        await JSRuntime.InvokeVoidAsync("audioFunctions.playAudio", "MenuMusic");
    }
}

