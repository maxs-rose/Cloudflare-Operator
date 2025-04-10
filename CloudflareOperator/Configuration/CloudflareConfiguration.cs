namespace CloudflareOperator.Configuration;

public sealed class CloudflareConfiguration
{
    public const string ConfigurationSection = "Cloudflare";

    public required Uri ApiUrl { get; init; }
    public TimeSpan PolicyCacheTime { get; init; } = TimeSpan.FromMinutes(10);
}