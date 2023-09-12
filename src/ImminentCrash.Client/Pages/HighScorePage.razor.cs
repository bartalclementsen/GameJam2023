using ImminentCrash.Contracts;
using Microsoft.AspNetCore.Components;
using ImminentCrash.Contracts.Model;

namespace ImminentCrash.Client.Pages;

public partial class HighScorePage
{
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Inject] public IImminentCrashService Client { get; set; } = default!;

    private List<HighscoreResponse>? _winningHighscores;

    private List<HighscoreResponse>? _loosingHighscores;

    private bool _showWinners = false;

    protected override async Task OnInitializedAsync()
    {
        var result = await Client.GetTopHighscoresAsync(new GetTopHighscoresRequest() { });

        _winningHighscores = result.WinningHighscores;
        _loosingHighscores = result.LoosingHighscores;
        
        await base.OnInitializedAsync();
    }

    public void OnBackClicked()
    {
        NavigationManager.NavigateTo("/");
    }
}
