using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grpc.Net.Client;
using Maichess.Database.V1;
using Maichess.Engine.V1;
using Maichess.MatchManager.V1;
using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Clients;
using MaichessBotArenaService.Persistence;
using MaichessBotArenaService.Rest;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

DotNetEnv.Env.Load();
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string dbServiceUrl = builder.Configuration["Services:DatabaseService"]
    ?? throw new InvalidOperationException("Services:DatabaseService is not configured");
string matchManagerUrl = builder.Configuration["Services:MatchManager"]
    ?? throw new InvalidOperationException("Services:MatchManager is not configured");
string engineUrl = builder.Configuration["Services:Engine"]
    ?? throw new InvalidOperationException("Services:Engine is not configured");
string matchMakerUrl = builder.Configuration["Services:MatchMaker"]
    ?? throw new InvalidOperationException("Services:MatchMaker is not configured");
string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured");

builder.Services.AddSingleton(new Database.DatabaseClient(GrpcChannel.ForAddress(dbServiceUrl)));
builder.Services.AddSingleton(new Bots.BotsClient(GrpcChannel.ForAddress(engineUrl)));
builder.Services.AddSingleton(new Matches.MatchesClient(GrpcChannel.ForAddress(matchManagerUrl)));

builder.Services.AddSingleton<IArenaStore, ArenaStore>();
builder.Services.AddSingleton<IBotCatalog, EngineBotCatalog>();
builder.Services.AddSingleton<IMatchOutcomeReader, MatchManagerOutcomeReader>();
builder.Services.AddSingleton<IArenaRandomProvider, DefaultArenaRandomProvider>();
builder.Services.AddSingleton(new ServiceTokenMinter(jwtKey));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ArenaSettingsService>();
builder.Services.AddSingleton<CollectionService>();
builder.Services.AddSingleton<ResultViewBuilder>();
builder.Services.AddSingleton<Func<long>>(_ => () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

builder.Services.AddHttpClient<IGameLauncher, MatchMakerGameLauncher>(client =>
    client.BaseAddress = new Uri(matchMakerUrl));

builder.Services.AddHostedService<CollectionPoller>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("access_token", out string? token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

string otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://otel-collector:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("bot-arena-service"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint)));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok());
app.MapConcurrencyEndpoints();
app.MapCollectionEndpoints();

app.Run();
