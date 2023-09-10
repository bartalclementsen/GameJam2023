using ImminentCrash.Server.Services;
using ProtoBuf.Grpc.Server;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
IServiceCollection services = builder.Services;


services.AddSingleton<IGameService, GameService>();
services.AddSingleton<ICoinDataService, CoinDataService>();
services.AddSingleton<IHighscoreService, HighscoreService>();
services.AddTransient<GameSession>();
services.AddSingleton<Func<GameSession>>((s) => s.GetRequiredService<GameSession>);


// Add health check
services.AddHealthChecks();

// CORS
services.AddCors(policy =>
{
    policy.AddDefaultPolicy(opt => opt
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("X-Pagination", "Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding", "validation-errors-text")
    );
});

//string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//if (string.IsNullOrWhiteSpace(connectionString))
//    throw new ApplicationException($"{nameof(connectionString)} is required");

//services.AddInfrastructure(connectionString, builder.Environment.IsDevelopment());

// GRPC
services.AddCodeFirstGrpc(config =>
{
    config.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});
services.AddCodeFirstGrpcReflection();

WebApplication app = builder.Build();

ICoinDataService coinDataService = app.Services.GetRequiredService<ICoinDataService>();
await coinDataService.InitializeAsync();


//await app.UseInfrastructureAsync();

app.UseHttpsRedirection();
app.UseCors();

app.UseRouting();

app.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });
app.MapGrpcService<ImminentCrashService>();
app.MapCodeFirstGrpcReflectionService();

app.MapHealthChecks("/healthz");

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

services.AddSingleton<ICoinDataService, CoinDataService>();