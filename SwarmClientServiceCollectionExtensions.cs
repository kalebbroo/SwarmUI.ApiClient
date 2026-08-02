using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwarmUI.ApiClient;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering the SwarmUI client in a dependency injection container.</summary>
/// <remarks>Placed in the <c>Microsoft.Extensions.DependencyInjection</c> namespace so <c>AddSwarmClient</c> resolves without an extra using directive in hosts that already reference the DI abstractions.</remarks>
public static class SwarmClientServiceCollectionExtensions
{
    /// <summary>The named HttpClient registration used by the SwarmUI client.</summary>
    public const string HttpClientName = "SwarmUI.ApiClient";

    /// <summary>Adds a singleton <see cref="ISwarmClient"/> configured via a callback.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Callback to configure client options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>The client is a singleton (one session pool per server); HttpClients come from IHttpClientFactory so connection handlers rotate and DNS changes are honored.</remarks>
    public static IServiceCollection AddSwarmClient(this IServiceCollection services, Action<SwarmClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.AddOptions<SwarmClientOptions>()
            .Configure(configureOptions)
            .Validate(options => Uri.TryCreate(options.NormalizedBaseUrl, UriKind.Absolute, out _), "SwarmClientOptions.BaseUrl must be an absolute URL")
            .ValidateOnStart();
        return AddSwarmClientCore(services);
    }

    /// <summary>Adds a singleton <see cref="ISwarmClient"/> bound to a configuration section (keys matching <see cref="SwarmClientOptions"/> property names).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">Configuration section to bind, e.g. <c>builder.Configuration.GetSection("Swarm")</c>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwarmClient(this IServiceCollection services, IConfiguration configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);
        services.AddOptions<SwarmClientOptions>()
            .Bind(configurationSection)
            .Validate(options => Uri.TryCreate(options.NormalizedBaseUrl, UriKind.Absolute, out _), "SwarmClientOptions.BaseUrl must be an absolute URL")
            .ValidateOnStart();
        return AddSwarmClientCore(services);
    }

    private static IServiceCollection AddSwarmClientCore(IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName, (provider, httpClient) =>
        {
            SwarmClientOptions options = provider.GetRequiredService<IOptions<SwarmClientOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.NormalizedBaseUrl);
            httpClient.Timeout = options.HttpTimeout;
            SwarmClient.ConfigureAuth(httpClient, options);
        });
        services.AddSingleton<ISwarmClient>(provider =>
        {
            SwarmClientOptions options = provider.GetRequiredService<IOptions<SwarmClientOptions>>().Value;
            IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            ILoggerFactory? loggerFactory = provider.GetService<ILoggerFactory>();
            return new SwarmClient(options, () => httpClientFactory.CreateClient(HttpClientName), loggerFactory);
        });
        return services;
    }
}
