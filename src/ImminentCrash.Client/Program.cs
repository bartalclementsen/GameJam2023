using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using ImminentCrash.Client;
using ImminentCrash.Contracts;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProtoBuf.Grpc.Client;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Logging
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

string? serverBaseAddress = builder.Configuration["App:ServerUrl"]?.ToString();
if (string.IsNullOrEmpty(serverBaseAddress))
{
    throw new ApplicationException("App:ServerUrl in appsettings.json must be a valid address");
}

// gRPC Channel
// Credits: https://github.com/grpc/grpc-dotnet/blob/master/examples/Blazor/Client/Program.cs
builder.Services.AddSingleton(services =>
{
    IConfiguration config = services.GetRequiredService<IConfiguration>();
    string backendUrl = serverBaseAddress;

    return GrpcChannel.ForAddress(backendUrl, new GrpcChannelOptions
    {
        HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
    });
});

builder.Services.AddTransient(services => services
    .GetRequiredService<GrpcChannel>()
    .CreateGrpcService<IImminentCrashService>()
);

WebAssemblyHost host = builder.Build();

ILogger<Program> logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger<Program>();

logger.LogInformation("Started");

await host.RunAsync();