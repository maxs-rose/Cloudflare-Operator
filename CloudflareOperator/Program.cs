using CloudflareOperator.Clients;
using CloudflareOperator.Services;
using CloudflareOperator.Watchers;
using KubeOps.Operator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((ctx, configuration) =>
{
    configuration
        .ReadFrom.Services(ctx)
        .ReadFrom.Configuration(builder.Configuration);
});

builder.Services
    .AddRefitClient<ICloudflareClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.cloudflare.com/client/v4"));

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

using var app = builder.Build();

await app.RunAsync();