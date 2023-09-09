using ImminentCrash.Contracts;
using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace ImminentCrash.Client.Pages;

public partial class Index
{
    [Inject] public IImminentCrashService Client { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private string _version = "Unknown";

    protected override void OnInitialized()
    {
        _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
    }

    protected override Task OnInitializedAsync()
    {
        return base.OnInitializedAsync();
    }

    public void OnStartClicked()
    {
        NavigationManager.NavigateTo("/Game");
    }

    public void OnHighScoreClicked()
    {
        NavigationManager.NavigateTo("/HighScore");

    }
}
