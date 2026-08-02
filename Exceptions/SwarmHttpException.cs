using System;
using System.Net;

namespace SwarmUI.ApiClient.Exceptions;

/// <summary>Exception thrown when an HTTP request to SwarmUI fails at the transport level (non-success status code with no parseable SwarmUI error object).</summary>
/// <remarks>Distinguishes transport failures (proxies, gateways, server crashes) from SwarmUI handler errors, which arrive as HTTP 200 with an <c>error</c>/<c>error_id</c> body and surface as <see cref="SwarmException"/>. 5xx instances are considered transient and retried when <c>SwarmClientOptions.RetryTransientHttpErrors</c> is enabled.</remarks>
public class SwarmHttpException : SwarmException
{
    /// <summary>The HTTP status code returned by the server or intermediary.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>A truncated snippet of the response body, for diagnostics. May be empty.</summary>
    public string BodySnippet { get; }

    /// <summary>Creates a new SwarmHttpException.</summary>
    /// <param name="statusCode">HTTP status code of the failed response.</param>
    /// <param name="message">The error message.</param>
    /// <param name="bodySnippet">Truncated response body for diagnostics.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public SwarmHttpException(HttpStatusCode statusCode, string message, string bodySnippet = "", Exception? innerException = null) : base(message, innerException)
    {
        StatusCode = statusCode;
        BodySnippet = bodySnippet;
    }

    /// <summary>True when the status code indicates a transient server-side failure (5xx) worth retrying.</summary>
    public bool IsTransient => (int)StatusCode >= 500;
}
