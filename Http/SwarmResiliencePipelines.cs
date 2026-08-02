using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using SwarmUI.ApiClient.Exceptions;

namespace SwarmUI.ApiClient.Http;

/// <summary>Builds the Polly resilience pipelines used by the HTTP and WebSocket layers.</summary>
/// <remarks>The HTTP pipeline wraps entire request attempts (session acquisition through response parsing), which is why the session-expiry retry lives here rather than in an HttpClient handler: retrying invalid_session_id requires the parsed response body and a session invalidation side effect between attempts. Deterministic SwarmUI handler errors (HTTP 200 + error key) are never retried.</remarks>
internal static class SwarmResiliencePipelines
{
    /// <summary>Builds the pipeline for HTTP API calls, honoring the retry options, or returns the caller-supplied override pipeline.</summary>
    public static ResiliencePipeline BuildHttpPipeline(SwarmClientOptions options)
    {
        if (options.HttpResiliencePipeline is not null)
        {
            return options.HttpResiliencePipeline;
        }
        if (options.MaxRetryAttempts <= 0)
        {
            return ResiliencePipeline.Empty;
        }
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = options.RetryBaseDelay,
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception switch
                {
                    SwarmSessionException => true,
                    SwarmHttpException httpEx when options.RetryTransientHttpErrors => httpEx.IsTransient,
                    HttpRequestException when options.RetryTransientHttpErrors => true,
                    // HttpClient surfaces its own timeout as TaskCanceledException wrapping a TimeoutException.
                    TaskCanceledException { InnerException: TimeoutException } when options.RetryTransientHttpErrors => true,
                    _ => false
                })
            })
            .Build();
    }

    /// <summary>Builds the pipeline for WebSocket connection establishment. Covers all WebSocketException variants during connect; never used mid-stream.</summary>
    public static ResiliencePipeline BuildWebSocketConnectPipeline(SwarmClientOptions options)
    {
        if (options.MaxRetryAttempts <= 0)
        {
            return ResiliencePipeline.Empty;
        }
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = options.RetryBaseDelay,
                ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is WebSocketException)
            })
            .Build();
    }
}
