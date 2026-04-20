using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace LogicApps.TestFramework.IntegrationTests;

[ExcludeFromCodeCoverage]
public static class AppSettings
{
    private static IConfiguration? _configuration;
    public static IConfiguration Configuration
    {
        get
        {
            _configuration ??= new ConfigurationBuilder()
#if DEBUG
                // ReSharper disable once StringLiteralTypo
                .AddJsonFile("appsettings.local.json")
#else
                    .AddJsonFile("appsettings.json")
#endif
                .Build();
            return _configuration;
        }
    }
}