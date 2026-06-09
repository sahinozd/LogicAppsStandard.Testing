using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace LogicApps.Management.Tests;

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