using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Endpoints.Presets;
using SwarmUI.ApiClient.Http;
using SwarmUI.ApiClient.Models.Requests;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Endpoints
{
    /// <summary>Unit tests for <see cref="PresetsEndpoint"/> verifying payload shaping and error handling.</summary>
    public class PresetsEndpointTests
    {
        /// <summary>Test HTTP client that records the last endpoint and payload and returns a configurable JObject.</summary>
        private sealed class RecordingHttpClient : ISwarmHttpClient
        {
            public string? LastEndpoint { get; private set; }
            public JObject? LastPayload { get; private set; }
            public JObject ResponseToReturn { get; set; } = new JObject();

            public Task<TResponse> PostJsonAsync<TResponse>(string endpoint, object? payload = null, CancellationToken cancellationToken = default) where TResponse : class
            {
                LastEndpoint = endpoint;
                LastPayload = payload as JObject ?? (payload != null ? JObject.FromObject(payload) : new JObject());
                if (typeof(TResponse) == typeof(JObject))
                {
                    return Task.FromResult((TResponse)(object)ResponseToReturn);
                }
                throw new NotSupportedException("RecordingHttpClient only supports JObject responses.");
            }

            public Task<TResponse> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
            {
                LastEndpoint = endpoint;
                LastPayload = JObject.FromObject(request);
                if (typeof(TResponse) == typeof(JObject))
                {
                    return Task.FromResult((TResponse)(object)ResponseToReturn);
                }
                throw new NotSupportedException("RecordingHttpClient only supports JObject responses.");
            }
        }

        /// <summary>Test implementation of <see cref="ISessionManager"/> that returns fixed session IDs.</summary>
        private sealed class DummySessionManager : ISessionManager
        {
            public Task<string> GetOrCreateSessionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult("session-1");
            }

            public Task<string> RefreshSessionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult("session-2");
            }

            public void InvalidateSession()
            {
            }

            public string? CurrentSessionId => "session-1";
        }

        [Fact]
        public async Task AddNewPresetAsync_MapsRequestToExpectedPayload()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            DummySessionManager sessionManager = new DummySessionManager();
            PresetsEndpoint endpoint = new PresetsEndpoint(httpClient, sessionManager, logger: null);

            PresetRequest request = new PresetRequest
            {
                Title = "My Preset",
                Description = "Desc",
                Parameters = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "model", "flux" },
                    { "steps", 20 }
                },
                PreviewImage = "base64data",
                IsEdit = true,
                EditingName = "Old Preset"
            };

            await endpoint.AddNewPresetAsync(request, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("AddNewPreset", httpClient.LastEndpoint);
            Assert.NotNull(httpClient.LastPayload);
            Assert.Equal("My Preset", httpClient.LastPayload!["title"]?.ToString());
            Assert.Equal("Desc", httpClient.LastPayload!["description"]?.ToString());
            Assert.Equal("base64data", httpClient.LastPayload!["preview_image"]?.ToString());
            Assert.True(httpClient.LastPayload!["is_edit"]?.ToObject<bool>() ?? false);
            Assert.Equal("Old Preset", httpClient.LastPayload!["editing"]?.ToString());

            JObject? raw = httpClient.LastPayload!["raw"] as JObject;
            Assert.NotNull(raw);
            JObject? paramMap = raw!["param_map"] as JObject;
            Assert.NotNull(paramMap);
            Assert.Equal("flux", paramMap!["model"]?.ToString());
        }

        [Fact]
        public async Task AddNewPresetAsync_ThrowsWhenServerReturnsPresetFail()
        {
            RecordingHttpClient httpClient = new RecordingHttpClient();
            httpClient.ResponseToReturn = new JObject
            {
                ["preset_fail"] = "Name already exists"
            };
            DummySessionManager sessionManager = new DummySessionManager();
            PresetsEndpoint endpoint = new PresetsEndpoint(httpClient, sessionManager, logger: null);

            PresetRequest request = new PresetRequest
            {
                Title = "My Preset",
                Parameters = new System.Collections.Generic.Dictionary<string, object>()
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await endpoint.AddNewPresetAsync(request, CancellationToken.None).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }
}
