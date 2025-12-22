using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Sessions;
using SwarmUI.ApiClient.WebSockets;

namespace SwarmUI.ApiClient.Extensions;

/// <summary>Extension methods for configuring the SwarmUI client in a dependency injection container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds SwarmUI client services to the dependency injection container.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Callback to configure client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwarmClient(this IServiceCollection services, Action<SwarmClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.Configure(configureOptions);
        services.AddSingleton<SwarmClientOptions>(provider =>
            {
                IOptions<SwarmClientOptions> optionsAccessor = provider.GetRequiredService<IOptions<SwarmClientOptions>>();
                return optionsAccessor.Value;
            });
        services.AddHttpClient<ISwarmClient, SwarmClient>((provider, httpClient) =>
        {
            SwarmClientOptions options = provider.GetRequiredService<SwarmClientOptions>();
            if (httpClient.BaseAddress == null)
            {
                httpClient.BaseAddress = new Uri(options.BaseUrl);
            }
            httpClient.Timeout = options.HttpTimeout;
            SwarmClient.ConfigureAuthorizationHeader(httpClient, options);
        });
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddTransient<ISwarmHttpClient, SwarmHttpClient>();
        services.AddTransient<ISwarmWebSocketClient, SwarmWebSocketClient>();
        return services;
    }

    // TODO: Add overload for IConfiguration binding
    // public static IServiceCollection AddSwarmClient(this IServiceCollection services, IConfiguration configuration)

    // TODO: Add overload for default options
    // public static IServiceCollection AddSwarmClient(this IServiceCollection services)

    // TODO: Add Polly policy helper methods (for retry and circuit breaker policies)
    // private static IAsyncPolicy{HttpResponseMessage} GetRetryPolicy()
    // private static IAsyncPolicy{HttpResponseMessage} GetCircuitBreakerPolicy()
}
