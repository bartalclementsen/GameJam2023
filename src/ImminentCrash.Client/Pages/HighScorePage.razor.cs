using Microsoft.AspNetCore.Components;

namespace ImminentCrash.Client.Pages;

public partial class HighScorePage
{
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    public void OnBackClicked()
    {
        NavigationManager.NavigateTo("/");
    }
}
