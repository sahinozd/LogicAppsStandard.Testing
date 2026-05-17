using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace LogicApps.TestFramework.Specifications;

[ExcludeFromCodeCoverage]
public static class AppSettings
{
    private static readonly Lazy<IConfiguration> LazyConfiguration = new(() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build());

    /// <summary>
    /// Allows tests or host setup code to inject a custom <see cref="IConfiguration"/> instance,
    /// bypassing the default file-based configuration. Set this before any step definitions are
    /// constructed. Reset to <see langword="null"/> to restore the default behaviour.
    /// </summary>
    public static IConfiguration? Override { get; set; }

    /// <summary>
    /// The active configuration. Returns <see cref="Override"/> when set; otherwise falls back to
    /// the default JSON-file-based configuration built from <c>appsettings.json</c> and the
    /// optional <c>appsettings.local.json</c>.
    /// </summary>
    public static IConfiguration Configuration => Override ?? LazyConfiguration.Value;
}
