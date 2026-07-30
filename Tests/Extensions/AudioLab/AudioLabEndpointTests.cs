using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Extensions;
using SwarmUI.ApiClient.Extensions.AudioLab;
using SwarmUI.ApiClient.Extensions.AudioLab.Contracts;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Extensions.AudioLab
{
    /// <summary>Unit tests for <see cref="AudioLabEndpoint"/> covering payload shaping, validation, and streamed install frames.</summary>
    public class AudioLabEndpointTests
    {
        /// <summary>Builds an endpoint over recording doubles.</summary>
        private static AudioLabEndpoint CreateEndpoint(RecordingExtensionHttpClient httpClient, RecordingExtensionWebSocketClient webSocketClient)
        {
            return new AudioLabEndpoint(httpClient, webSocketClient, new StubSessionManager(), logger: null);
        }

        [Fact]
        public async Task SynthesizeSpeechAsync_MapsRequestToExpectedPayload()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject
                {
                    ["success"] = true,
                    ["audio_data"] = "QUJD",
                    ["duration"] = 1.5
                }
            };
            AudioLabEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            TextToSpeechRequest request = new TextToSpeechRequest
            {
                Text = "hello world",
                Voice = "af_heart",
                Language = "en-US",
                Volume = 0.5f,
                ProviderId = "kokoro",
                ReferenceAudio = "UkVG",
                ReferenceText = "reference transcript",
                Options = new TextToSpeechOptions { Speed = 1.25f, Pitch = 0.9f, Format = "mp3" }
            };

            TextToSpeechResponse response = await endpoint.SynthesizeSpeechAsync(request, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("ProcessTTS", httpClient.LastEndpoint);
            Assert.NotNull(httpClient.LastPayload);
            Assert.Equal("hello world", httpClient.LastPayload!["text"]?.ToString());
            Assert.Equal("af_heart", httpClient.LastPayload!["voice"]?.ToString());
            Assert.Equal("kokoro", httpClient.LastPayload!["provider_id"]?.ToString());
            Assert.Equal("UkVG", httpClient.LastPayload!["reference_audio"]?.ToString());
            Assert.Equal("reference transcript", httpClient.LastPayload!["ref_text"]?.ToString());
            Assert.Equal("mp3", httpClient.LastPayload!["options"]?["format"]?.ToString());
            Assert.Equal(1.25f, httpClient.LastPayload!["options"]?["speed"]?.ToObject<float>());
            Assert.True(response.Success);
            Assert.Equal("QUJD", response.AudioData);
            Assert.Equal(1.5, response.Duration);
        }

        [Fact]
        public async Task SynthesizeSpeechAsync_OmitsProviderWhenUnset()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            AudioLabEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            await endpoint.SynthesizeSpeechAsync(new TextToSpeechRequest { Text = "hi" }, CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(httpClient.LastPayload);
            Assert.Null(httpClient.LastPayload!["provider_id"]);
            Assert.Null(httpClient.LastPayload!["reference_audio"]);
        }

        [Fact]
        public async Task TranscribeAudioAsync_MapsRequestToExpectedPayload()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject
                {
                    ["success"] = true,
                    ["transcription"] = "spoken words",
                    ["confidence"] = 0.92
                }
            };
            AudioLabEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            SpeechToTextResponse response = await endpoint.TranscribeAudioAsync(new SpeechToTextRequest
            {
                AudioData = "QUJD",
                Language = "en",
                ProviderId = "whisper"
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("ProcessSTT", httpClient.LastEndpoint);
            Assert.Equal("QUJD", httpClient.LastPayload!["audio_data"]?.ToString());
            Assert.Equal("whisper", httpClient.LastPayload!["provider_id"]?.ToString());
            Assert.Equal("spoken words", response.Transcription);
            Assert.Equal(0.92f, response.Confidence);
        }

        [Fact]
        public async Task ProcessAudioAsync_ForwardsProviderArgumentsAndCapturesUnknownFields()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject
                {
                    ["success"] = true,
                    ["audio_data"] = "T1VU",
                    ["stems"] = new JArray("vocals", "drums")
                }
            };
            AudioLabEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            AudioProcessResponse response = await endpoint.ProcessAudioAsync(new AudioProcessRequest
            {
                ProviderId = "demucs",
                Arguments = new Dictionary<string, object> { { "overlap", 0.25 } }
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("ProcessAudio", httpClient.LastEndpoint);
            Assert.Equal("demucs", httpClient.LastPayload!["provider_id"]?.ToString());
            Assert.Equal(0.25, httpClient.LastPayload!["args"]?["overlap"]?.ToObject<double>());
            Assert.Equal("T1VU", response.AudioData);
            Assert.True(response.AdditionalData.ContainsKey("stems"));
        }

        [Fact]
        public async Task TimeStretchAsync_RejectsRateOutsideServerRange()
        {
            AudioLabEndpoint endpoint = CreateEndpoint(new RecordingExtensionHttpClient(), new RecordingExtensionWebSocketClient());

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await endpoint.TimeStretchAsync(new AudioTimeStretchRequest { AudioData = "QUJD", Rate = 8.0 }, CancellationToken.None).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task UninstallEngineAsync_IncludesModelIdOnlyWhenSupplied()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient();
            AudioLabEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            await endpoint.UninstallEngineAsync("kokoro", deleteWeights: true, modelId: null, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("AudioLabUninstallEngine", httpClient.LastEndpoint);
            Assert.Equal("kokoro", httpClient.LastPayload!["provider_id"]?.ToString());
            Assert.True(httpClient.LastPayload!["delete_weights"]?.ToObject<bool>());
            Assert.Null(httpClient.LastPayload!["model_id"]);

            await endpoint.UninstallEngineAsync("kokoro", deleteWeights: false, modelId: "kokoro-v1", CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("kokoro-v1", httpClient.LastPayload!["model_id"]?.ToString());
        }

        [Fact]
        public async Task StreamEngineInstallAsync_ParsesProgressAndTerminalFrames()
        {
            RecordingExtensionWebSocketClient webSocketClient = new RecordingExtensionWebSocketClient
            {
                FramesToReturn =
                [
                    new JObject { ["info"] = "Downloading weights" },
                    new JObject { ["info"] = "kokoro-v1 installed.", ["model_id"] = "kokoro-v1", ["model_done"] = true },
                    new JObject { ["success"] = true, ["provider_id"] = "kokoro", ["message"] = "Kokoro installed successfully!" }
                ]
            };
            AudioLabEndpoint endpoint = CreateEndpoint(new RecordingExtensionHttpClient(), webSocketClient);

            List<AudioEngineInstallUpdate> updates = [];
            await foreach (AudioEngineInstallUpdate update in endpoint.StreamEngineInstallAsync("kokoro", "kokoro-v1", CancellationToken.None))
            {
                updates.Add(update);
            }

            Assert.Equal("AudioLabInstallEngine", webSocketClient.LastEndpoint);
            Assert.Equal("kokoro", webSocketClient.LastPayload!["provider_id"]?.ToString());
            Assert.Equal("kokoro-v1", webSocketClient.LastPayload!["model_id"]?.ToString());
            Assert.Equal(3, updates.Count);
            Assert.Equal("Downloading weights", updates[0].Info);
            Assert.False(updates[0].IsTerminal);
            Assert.True(updates[1].ModelDone);
            Assert.True(updates[2].IsTerminal);
            Assert.True(updates[2].Success);
        }

        [Fact]
        public async Task StreamEngineInstallAsync_OmitsModelIdWhenInstallingDefaultSet()
        {
            RecordingExtensionWebSocketClient webSocketClient = new RecordingExtensionWebSocketClient();
            AudioLabEndpoint endpoint = CreateEndpoint(new RecordingExtensionHttpClient(), webSocketClient);

            await foreach (AudioEngineInstallUpdate _ in endpoint.StreamEngineInstallAsync("kokoro", modelId: null, CancellationToken.None))
            {
            }

            Assert.Null(webSocketClient.LastPayload!["model_id"]);
        }

        [Fact]
        public async Task SaveProjectAsync_MapsNameAndProjectJson()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject { ["success"] = true, ["name"] = "my set", ["size"] = 42 }
            };
            AudioLabEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            DawProjectSaveResponse response = await endpoint.SaveProjectAsync("my set", "{\"tracks\":[]}", CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("AudioLabSaveProject", httpClient.LastEndpoint);
            Assert.Equal("my set", httpClient.LastPayload!["name"]?.ToString());
            Assert.Equal("{\"tracks\":[]}", httpClient.LastPayload!["project_json"]?.ToString());
            Assert.Equal(42, response.Size);
        }

        [Fact]
        public async Task ErrorEnvelopeIsSurfacedOnTheSharedResponseBase()
        {
            RecordingExtensionHttpClient httpClient = new RecordingExtensionHttpClient
            {
                ResponseToReturn = new JObject
                {
                    ["success"] = false,
                    ["error"] = "No TTS provider available",
                    ["error_code"] = "no_provider"
                }
            };
            AudioLabEndpoint endpoint = CreateEndpoint(httpClient, new RecordingExtensionWebSocketClient());

            TextToSpeechResponse response = await endpoint.SynthesizeSpeechAsync(new TextToSpeechRequest { Text = "hi" }, CancellationToken.None).ConfigureAwait(false);

            Assert.False(response.Success);
            Assert.Equal("No TTS provider available", response.Error);
            Assert.Equal("no_provider", response.ErrorCode);
        }

        [Fact]
        public void Endpoint_ExposesExtensionMetadata()
        {
            ISwarmExtensionEndpoint endpoint = CreateEndpoint(new RecordingExtensionHttpClient(), new RecordingExtensionWebSocketClient());

            Assert.Equal("AudioLab", endpoint.Extension.Name);
            Assert.Contains("ProcessTTS", endpoint.Extension.Endpoints);
            Assert.Contains("AudioLabInstallEngine", endpoint.Extension.Endpoints);
            Assert.False(string.IsNullOrWhiteSpace(endpoint.Extension.RepositoryUrl));
        }
    }
}
