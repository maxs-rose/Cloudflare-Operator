using CloudflareOperator.Clients;
using CloudflareOperator.Configuration;
using CloudflareOperator.Services;
using CloudflareOperator.Watchers;
using KubeOps.Operator;
using Microsoft.Extensions.Options;
using Refit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CloudflareConfiguration>(builder.Configuration.GetSection(CloudflareConfiguration.ConfigurationSection));

builder.Services.AddSerilog((ctx, configuration) =>
{
    configuration
        .ReadFrom.Services(ctx)
        .ReadFrom.Configuration(builder.Configuration);
});

builder.Services
    .AddRefitClient<ICloudflareClient>()
    .ConfigureHttpClient((ctx, c) => c.BaseAddress = ctx.GetRequiredService<IOptions<CloudflareConfiguration>>().Value.ApiUrl);

builder.Services
    .AddHostedService<RemoteTunnelWatcher>()
    .AddHostedService<TunnelSecretWatcher>()
    .AddHostedService<TunnelDnsWatcher>()
    .AddHostedService<TunnelDeploymentWatcher>()
    .AddHostedService<TunnelAccessDeploymentWatcher>()
    .AddHostedService<TunnelAccessServiceWatcher>()
    .AddHostedService<RemoteAccessApplicationWatcher>()
    .AddSingleton<TunnelDeploymentService>()
    .AddSingleton<ApiTokenService>()
    .AddSingleton<DnsService>()
    .AddSingleton<PolicyService>()
    .AddMemoryCache()
    .AddKubernetesOperator()
    .RegisterComponents();

await using var app = builder.Build();

await app.RunAsync();