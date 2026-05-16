using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace LogicApps.TestFramework.IntegrationTests;

[ExcludeFromCodeCoverage]
public static class AppSettings
{
    private static readonly Lazy<IConfiguration> LazyConfiguration = new(() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build());

    public static IConfiguration Configuration => LazyConfiguration.Value;
}