using System.Reflection;

namespace ImminentCrash.Client.Pages
{
    public partial class Index
    {
        private string _version = "Unknown";

        protected override void OnInitialized()
        {
            _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }

        protected override Task OnInitializedAsync()
        {
            return base.OnInitializedAsync();
        }
    }
}
