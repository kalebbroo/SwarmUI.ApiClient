# SwarmUI.ApiClient Changelog

## 0.9.1-beta

Follow-up to 0.9.0-beta from real-world generation testing. No API changes.

### Fixed

- A generation stream that ends without the server's terminal `socket_intention:"close"` frame now
  records a synthetic error in `CompletionInfo.Errors` (id `ErrorInfo.StreamEndedEarlyErrorId`,
  `"stream_ended_early"`) explaining that the connection dropped and the outcome is unconfirmed.
  Previously such a stream produced `Succeeded = false` with an **empty** `Errors` list, leaving
  consumers with a failure they could only report as "unknown error".
- Consumers can now distinguish a server-reported failure from a dropped connection by checking
  that error id, and should call `InterruptAllAsync` on the same session when they see it —
  SwarmUI keeps generating after a client disconnects, so abandoned work is otherwise orphaned.

## 0.9.0-beta

Correctness and hardening release built against the verified SwarmUI server contract (official API
docs plus server source). Fixes the class of bug where a SwarmUI restart or session loss permanently
broke WebSocket generation, and makes the client safe for both single-user setups and multi-tenant
hosts running many users across many GPUs.

### Breaking changes

- **Keyed session pool.** `ISessionManager` is rewritten: all methods take a `sessionKey`
  (default `SwarmSessionKeys.Default`), invalidation is compare-and-swap on the observed session id,
  and `CurrentSessionId` is replaced by `GetCachedSession()` returning a `SwarmSessionInfo` record
  (session id, user id, server version, server id). One session key per logical user gives per-user
  interrupt and status isolation, because SwarmUI scopes `InterruptAll` and queue counters to a session.
- **`ISwarmClient.ForSession(key)`** returns a session-scoped view of the client; new members
  `Sessions` and `DisconnectAllAsync` added. `GetHealthAsync` now performs a real server probe on
  every call (it previously reported cached success) and populates `ServerVersion`/`ServerId`.
- **`ISwarmWebSocketClient`** now streams raw `JObject` frames via `StreamFramesAsync`
  (parsing moved to endpoints); `GracefulCloseAsync(ClientWebSocket)` is no longer public.
- **`GenerationUpdate.Type` values are now** `status | progress | image | discard | error | complete`.
  Every stream ends with exactly one terminal `"complete"` update carrying `CompletionInfo`
  (`Succeeded`, `ImagesReceived`, `DiscardedIndices`, `Errors`). `keep_alive` frames are consumed by
  the transport and no longer surface as updates. `ErrorInfo` gains `ErrorId`; `ImageInfo` gains
  `RequestId`, nullable `Metadata`, and an `IsDataUrl` helper.
- **`GenerationRequest` semantics corrected:** `Images` now maps to the server's `images` (number of
  images) and `BatchSize` to `batchsize` (per-backend batch) — previously `BatchSize` was sent as
  `images` and `Images` did nothing. `Seed` is now `long` (default -1 = random, omitted), and
  `FluxGuidanceScale` is `double?`. `StylePreset` is removed (no such server parameter existed);
  `Presets` (list of preset names) and `DoNotSaveIntermediates`/`AspectRatio` are added.
- **Extension (API-Backends) parameters renamed to their real server wire names.** The previous
  `openai_*`/`ideogram_*`/`google_*`/`grok_*` names did not match any registered server parameter and
  were silently dropped. Properties without a server counterpart were removed
  (`OpenAIN`, `IdeogramResolution`, `IdeogramNegativePrompt`, `IdeogramNumImages`,
  `IdeogramStyleCodes`, `IdeogramStylePreset`, `GoogleGeminiResponseModalities`,
  `GoogleImagenNumImages`, `GoogleImagenNegativePrompt`, `GrokN`, `GrokQuality`,
  `GrokResponseFormat`, `GrokUser`, `WebhookUrl`, `WebhookSecret`); `GoogleAspectRatio`,
  `IdeogramV4RenderingSpeed`, `GrokAspectRatio`, and `GrokOutputResolution` were added.
- **Endpoint constructors** take a `string sessionKey` instead of the (unused) `ISessionManager`.
- **`ISwarmHttpClient`** is a single `PostJsonAsync<TResponse>(endpoint, payload, sessionKey, ct)`.
- **DI rewrite:** `AddSwarmClient` now registers a working singleton `ISwarmClient` over
  `IHttpClientFactory` (the previous registrations were unresolvable, and the typed-client pattern
  created a new SwarmUI session per resolution). A configuration-binding overload was added.
- Standalone `SwarmClient` constructors take an optional `ILoggerFactory` (was `ILogger<SwarmClient>`).

### Fixed

- **WebSocket streams now detect `error_id=invalid_session_id`, CAS-invalidate the pooled session,
  transparently acquire a fresh one, and reconnect** (bounded by `SessionRefreshCap`) — per the
  official API docs' mandated pattern. Previously a stale session (e.g. after a SwarmUI restart with
  cleared sessions) permanently broke all generation until the consuming process restarted.
- Generation completion is now driven by the server's `socket_intention:"close"` terminal frame
  instead of counting images — discarded indices, grid composites (`batch_index` "-1"),
  intermediates (≤ -10), and API-backend image counts no longer hang the stream. `error` frames end
  the batch, not the socket.
- ~30 of ~45 `GenerationRequest` properties that never reached the wire are now serialized from
  `[JsonProperty]` attributes, with a reflection round-trip test preventing regression.
- LoRAs are sent as parallel JSON arrays (comma-containing names are safe) with full-precision
  weights (0.85 no longer truncated to 0.9).
- Session state is a single volatile-published immutable reference (no more torn/stale reads on
  weak memory models); session creation is single-flight per key; concurrent invalidation of one
  stale session converges on exactly one `GetNewSession` call; failed creation backs off
  (`SessionCreateFailureBackoff`) instead of storming a down server.
- HTTP retry honors `MaxRetryAttempts` via Polly (exponential + jitter), retries transient
  transport failures (opt-out `RetryTransientHttpErrors`), and never retries deterministic
  handler errors; the caller's payload `JObject` is no longer mutated; responses are parsed once.
- WebSocket layer: honors `AuthorizationHeaderName` (was hardcoded to `Authorization`), supports
  `swarm_token` cookie auth (`AuthMode`), propagates caller cancellation as
  `OperationCanceledException` (previously swallowed), enforces a receive timeout
  (`WebSocketReceiveTimeout`), preserves inner exceptions, skips malformed frames (3 consecutive
  aborts), pools receive buffers, caps message size (`MaxWebSocketMessageBytes`), tracks
  connections by id (disposed-socket leak and same-session collision fixed), and normalizes
  trailing-slash base URLs.
- Passwords, API keys, and webhook secrets are redacted from debug logs; session ids truncated;
  base64 image payloads summarized.
- Standalone clients use `SocketsHttpHandler` with pooled connection lifetime (DNS changes are
  picked up); `DisposeAsync` is idempotent; injected `HttpClient`s are no longer mutated after use.
- `PresetsEndpoint` throws `SwarmException` (was bare `InvalidOperationException`);
  `DescribeModelAsync` throws on unusable responses instead of fabricating a success object;
  missing `ConfigureAwait(false)` added on streaming loops.
- Publish workflow is tag-triggered, builds and unit-tests before packing, packs to a temp
  directory, and verifies the tag matches the project version (it previously failed on every run
  and would have pushed stale committed packages). Committed `.nupkg` artifacts removed from git.

### Added

- `Polly.Core` dependency; `HttpResiliencePipeline` option as a full override hook.
- New options: `AuthMode`, `RetryBaseDelay`, `RetryTransientHttpErrors`, `SessionRefreshCap`,
  `SessionCreateFailureBackoff`, `SessionIdleEviction` (off by default), `WebSocketReceiveTimeout`,
  `MaxWebSocketMessageBytes`.
- `SwarmHttpException` (transport failures, with status code and body snippet) distinct from
  handler-level `SwarmException`.
- Test suite rewritten: the real WebSocket client is now unit-tested through an internal socket
  seam (session-refresh recovery, completion semantics, cancellation, timeouts, reassembly);
  session-pool contention tests; payload reflection round-trip; HTTP retry/error-precedence tests.

## 0.6.0-beta / 0.6.1-beta

Interim packages published from the 0.5.x line with model enums, typed download overloads, and
Ideogram parameter additions (see git history); no changelog entries were written at release time.

## 0.8.0-beta

Adds typed coverage for the AudioLab and LLM Assistant extensions, using the per-extension layout
introduced in 0.7.0-beta.

- Added `Extensions/AudioLab` with 18 endpoints reached through `client.Extensions.AudioLab`: speech
  synthesis and transcription, provider routed processing, chained workflows, provider and engine
  status, streaming engine and bulk model installs, uninstall and weight removal, format conversion,
  time stretch, and DAW project storage.
- Added `Extensions/LLMAssistant` with 51 endpoints reached through
  `client.Extensions.LLMAssistant`: non-streaming completion, streaming send, edit-into-branch and
  regenerate-into-branch chat, thread and message management, per-thread assets, assistants,
  instructions, tools and direct tool execution, settings and audit log, LLM model listing and
  unloading, session state, companion context, and per-user memory.
- Streaming install and chat operations run over the shared WebSocket client, so `SwarmExtensions`
  now takes an `ISwarmWebSocketClient` alongside the HTTP client and session manager.
- Chat stream frames are surfaced as `ChatStreamUpdate`, which types the guaranteed fields and
  preserves the complete frame in `Raw`, since frame bodies vary by model, tool activity, and
  compare mode.
- Extension contracts model the servers' own envelopes, including AudioLab's `error_code` responses
  and the LLM Assistant's in-band `success` and `error` fields, so failures are readable without
  inspecting raw JSON.
- Added unit tests for both extensions, plus shared test doubles under `Tests/Extensions` that the
  MagicPrompt suite now uses as well.

Neither extension's generation path is duplicated here: AudioLab registers audio T2I parameters and
a backend type, so prompt-driven audio generation still runs through `client.Generation`, and LLM
Assistant registers `LLM` as a SwarmUI model type, so listing LLM model files still runs through
`client.Models`. See `Extensions/README.md`.

## 0.7.0-beta

Restructures the library so endpoints backed by a SwarmUI server extension are structurally separate
from stock SwarmUI, in preparation for the AudioLab and LLMAssistant extension endpoints.

- Added `Extensions/`, holding one folder per SwarmUI extension with its endpoint interface,
  implementation, and owned contracts. `Endpoints/` is now stock SwarmUI only.
- Added `ISwarmClient.Extensions`, `ISwarmExtensions`, `ISwarmExtensionEndpoint`, and
  `SwarmExtensionInfo`. `ISwarmExtensions.All` reports every supported extension for runtime
  capability reporting.
- Added `Extensions/README.md` as the supported extension registry and the checklist for adding one.
- Added unit tests for the MagicPrompt endpoint and extension metadata under `Tests/Extensions/`.

### Breaking changes

- `ISwarmClient.LLM` is removed. Use `ISwarmClient.Extensions.MagicPrompt`.
- `ILLMEndpoint` / `LLMEndpoint` are renamed to `IMagicPromptEndpoint` / `MagicPromptEndpoint` and
  moved from `SwarmUI.ApiClient.Endpoints.LLM` to `SwarmUI.ApiClient.Extensions.MagicPrompt`. The
  capability-based `LLM` grouping was replaced with per-extension grouping because MagicPrompt,
  LLMAssistant, and AudioLab are separate extensions.
- `MagicPromptRequest`, `MessageContent`, and `MagicPromptResponse` move to
  `SwarmUI.ApiClient.Extensions.MagicPrompt.Contracts`.
- The `Models/` folder is renamed to `Contracts/`; `SwarmUI.ApiClient.Models.*` namespaces become
  `SwarmUI.ApiClient.Contracts.*`. `Models` was ambiguous against SwarmUI's own use of "model" for
  checkpoints and LoRAs.
- `ServiceCollectionExtensions` is renamed to `SwarmClientServiceCollectionExtensions` and moved from
  `SwarmUI.ApiClient.Extensions` to the `Microsoft.Extensions.DependencyInjection` namespace, matching
  the framework convention. Hosts that referenced `using SwarmUI.ApiClient.Extensions;` solely for
  `AddSwarmClient` can drop that directive.

## 0.5.0-beta (release)
- Released `SwarmUI.ApiClient` beta v0.5.0 to NuGet.org.
- Finalized admin endpoint implementations: user management, system stats, and backend monitoring.
- Updated documentation and examples, including real-world usage patterns from HartsyWeb.
- Addressed minor bugs and improved stability based on early beta feedback.

## 0.4.0-beta
- Expanded integration test coverage for generation, models, backends, and user endpoints.
- Improved WebSocket streaming reliability and cancellation behavior.
- Refined error handling and exception types for HTTP and WebSocket failures.

## 0.3.0-beta
- Implemented user endpoints: profile management, preferences, and API keys.
- Added request/response models for user-related operations.
- Updated documentation to cover user API usage.

## 0.2.0-beta
- Added support for admin endpoints: user management, system stats, and backend monitoring.
- Improved error handling with detailed exceptions for HTTP and WebSocket errors.
- Enhanced unit test coverage for admin endpoints and error scenarios.

## 0.1.0-alpha

- First alpha of `SwarmUI.ApiClient`: a typed C# wrapper around SwarmUI's HTTP + WebSocket APIs for text‑to‑image, models, presets, backends, user, and admin.
- Core infrastructure wired up: `SwarmClientOptions`, `SessionManager` with caching and refresh, `SwarmHttpClient` with error mapping, `SwarmWebSocketClient` for streaming, and the high-level `SwarmClient` facade.
- Added unit tests for HTTP behavior, session management, WebSocket generation streaming, model management, presets, and client wiring.
- Introduced DI extensions (`AddSwarmClient`) so ASP.NET Core apps can configure the client via `SwarmClientOptions`.

## Pre-0.1.0 (internal scaffolding)

- Initial scaffolding pass: created endpoint interfaces, request/response models, and implementation guide docs based on existing Hartsy SwarmUI integration.
