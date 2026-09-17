# SwarmUI.ApiClient Changelog

## 0.11.1-beta

Ships the half of 0.11.0-beta that never made it into the commit.

### Fixed

- **The six stock Text2Audio parameters 0.11.0-beta's notes describe were missing from the package.** A `git
  checkout` of that file during an unrelated test reverted them while they were still uncommitted, so the release
  removed AudioLab's retired names without adding the replacements: `Text2AudioStyle`, `Text2AudioBpm`,
  `Text2AudioKeyScale`, `Text2AudioTimeSignature`, `Text2AudioLanguage` and `AudioFormat` are now actually present.
  0.11.0-beta could set music lyrics, via `Prompt`, but had no way to set genre at all.
- `Prompt` now carries the warning that for music models it holds the lyrics rather than the style.

### Changed

- **Dangling doc references now fail the build** (`CS1570`, `CS1574`, `CS1580`, `CS1581`, `CS1584` as errors). The
  compiler had already spotted this: the release build warned that `Text2AudioStyle` could not be resolved,
  because the docs still described a property that was no longer there. Nothing failed on a warning, so it shipped.
- A test asserts the stock Text2Audio group is carried in full, since that group is the whole music contract.

## 0.11.0-beta

Catches up with SwarmUI as of 2026-09-16. One change there is a silent wrong-output bug for anyone generating
music through 0.10.0-beta, which is why this is a same-week follow-up rather than a batched release.

### Breaking changes

- **Seven properties removed, because the server stopped registering their names.** AudioLab retired its own
  lyrics, BPM, key, time-signature and vocal-language parameters on 2026-09-16 so that ACE-Step, YuE2 and MiniMax
  Music 3 speak core's convention instead: `Lyrics`, `Yue2Lyrics`, `MiniMaxMusic3Lyrics`, `Bpm`, `KeyScale`,
  `TimeSignature`, `VocalLanguage`.
- **Lyrics now go in `Prompt`, and genre in the new `Text2AudioStyle`** — the reverse of what those three models
  took before. SwarmUI drops an unrecognised name with only a server-side log, so a 0.10.0-beta caller who put
  genre in `Prompt` and lyrics in `Lyrics` gets a plausible song that sings the genre tags, with no error anywhere.
  The four non-lyrics parameters are remapped server-side and still function under their old names; they are
  repointed here anyway, because a remap is not a contract.
- YuE v1 (`YuELyrics`) and HeartMuLa (`HeartLibLyrics`) keep their own lyrics parameters and are unaffected.

### Added

- **`Text2AudioStyle`**, **`Text2AudioBpm`**, **`Text2AudioKeyScale`**, **`Text2AudioTimeSignature`** and
  **`Text2AudioLanguage`**, the stock parameters that took over from the retired AudioLab ones. The four ACE
  inputs are gated behind the new `audio_ace_inputs` feature flag. Note `Text2AudioBpm` has a floor of 10 and no
  "0 means let the model choose", which the retired `bpm` had.
- **`AudioFormat`** (`audioformat`), a new stock parameter: mp3, wav, flac, ogg. Distinct from AudioLab's own
  `AudioOutputFormat`.
- **`T2IParamDefinition.FeatureFlags`**, splitting `feature_flag` into its parts. It is a comma-separated list,
  not one value — the ACE inputs now carry `"text2audio,audio_ace_inputs"` — so code comparing the raw string to
  a single flag silently stops matching them.

### Changed

- The shipped `SwarmParamRegistrySnapshot` is recaptured against the current server: 615 ids, down from 620.

### Notes

- Checked and unchanged: `T2IModel.ToNetObject` (so all 29 `ModelInfo` fields still hold), both `ToNet` methods
  (28 parameter keys, 9 group keys), the `ListT2IParams` top-level shape, every `IgnoreIf` sentinel in
  `SwarmParamOffValues`, and the endpoint lists of both wrapped extensions.
- Core's own YuE2 support (SwarmUI #1539) registers no new parameters — it adds a model class and workflow
  generation and reuses the existing Text2Audio group.

## 0.10.0-beta

Contract release. Every gap here is a field the server already sent and the client discarded, or a parameter the
server registers and the client could not carry. Names were diffed against a live `ListT2IParams` registry and the
SwarmUI source rather than taken on trust, and the diff is now a test.

### Breaking changes

- **`ModelInfo` is rewritten.** Six of its eight properties were bound to keys SwarmUI never sends —
  `type`, `version`, `path`, `date_created`, `date_modified` and `metadata` — so only `Name` and `Description`
  were ever populated. They are removed and replaced with the 29 fields `T2IModel.ToNetObject` actually emits,
  verified against 336 live entries across five subtypes. `Description` is now nullable, because the server sends
  null for a model that has none.
- **`[JsonExtensionData]` moved from `ModelDescription` up to `ModelInfo`**, so `ListModels` and
  `ListLoadedModels` keep unmapped fields too rather than only `DescribeModel`. `ModelDescription` now adds
  nothing of its own; the endpoint returns the same object `ListModels` puts in `files`.
- **`T2IParamsResponse.ModelsBySubtype`** is `Dictionary<string, List<T2IParamModelEntry>>` instead of lists of
  bare strings. Each server entry is a `[name, classId]` pair and the class id was being discarded.
- **`Priority` is `double`** on both `T2IParamDefinition` and `T2IParamGroup`. The server sends fractional values
  and means them: 42 parameters and 5 groups sit on half-steps to order between their neighbours, and rounding
  collapsed those into ties.
- **`GenerationRequest.InitImageCreativity` defaults to 0.6**, matching the server. It was 0.7.

### Added

- **`ModelInfo.CompatClass`.** This is what decides adapter compatibility: SwarmUI treats a LoRA, VAE or
  ControlNet as fitting a base model when their `compat_class` values match. Comparing architecture ids instead
  rejects valid pairings, because variants of one lineage share a compatibility class but not an architecture id.
- **The Refine / Upscale parameter group**, previously unreachable: `refinermodel`,
  `refinercontrolpercentage`, `refinermethod`, `refinerupscale`, `refinerupscalemethod`, `refinersteps`,
  `refinercfgscale`, `refinersampler`, `refinerscheduler`, `refinervae`, `refinerdotiling`, `refinerhypertile`.
  There is no "use refiner" switch — naming a refiner model is what enables the stage, and the server reads
  `"(Use Base)"` as off.
- **`vae`, `negativemodelincludeloras`, `loratencweights` and `lorasectionconfinement`**, the last two being the
  optional arrays that ride alongside a LoRA list.
- **`videoframes`**, image-to-video's frame count, which is a different parameter from the `textvideoframes`
  this client already carried.
- **`textaudioduration`**, the stock clip-length parameter AudioLab's backend reads before its own `maxduration`.
- **Five registry fields**: `value_names` (every dropdown's display labels — without them a UI can only render
  raw ids), `nonreusable` (the filter a reuse-these-settings action needs), `depend_non_default`, `view_min` and
  `can_sectionalize`.
- **`model_classes` and `model_compat_classes`**, the two top-level registry tables that together answer which
  adapters fit a base model without another request.
- **Two extension endpoint groups.** `client.Extensions.APIBackends.ListModelCapabilitiesAsync` reports each API
  backed model's family, modality, init-image and batch support, and feature flags.
  `client.Extensions.HartsyInference` covers all five endpoints that extension registers: supported
  architectures with their composition features and accepted samplers and schedulers, a per-checkpoint probe,
  loaded pipelines, device placement, and cache clearing.
- **`SwarmSubType.Audio`**, registered by AudioLab the way `LLM` is by LLMAssistant.

### Fixed

- **Wire names are now checked mechanically.** A test diffs every `[JsonProperty]` on `GenerationRequest` against
  a checked-in `ListT2IParams` snapshot. SwarmUI drops a name it does not recognise without erroring, so a typo
  costs a parameter that never applies and never complains; the rule was previously enforced by review alone.
  The snapshot was captured with the API-Backends, AudioLab and HartsyInference extensions loaded, and a test
  asserts ids from each are present so the suite cannot pass vacuously.

- **`SwarmParamOffValues`**, the value each parameter carries to mean "off" (SwarmUI's `IgnoreIf`), for all 106
  parameters that declare one — not only the four refiner sentinels. `ToNet` does not emit `ignore_if`, so this
  cannot be discovered from `ListT2IParams` or verified at runtime; it is extracted from `T2IParamTypes.cs` and
  `ComfyUIBackendExtension.cs` and mirrored here so consumers do not each hand-maintain the same list.
- **`SwarmParamRegistrySnapshot`**, the registered parameter ids this package was built against, shipped as an
  embedded resource so a consuming codebase can run its own drift test against the same fixture this library
  uses rather than capturing a second one that drifts independently.

### Notes

- **"Equals default" is not the same as "off".** It holds for 103 of the 106 parameters that declare an off
  value, and fails for three: `maskblur` defaults to 4 and is off at 0, `easycachestart` defaults to 0.15 and is
  off at 0, `easycacheend` defaults to 0.95 and is off at 1. That is why `SwarmParamOffValues` is a table rather
  than a rule. Emitting `ignore_if` from `ToNet` would be a one-line change in SwarmUI core and would retire it.

## 0.9.8-beta

Music generation parameters for the AudioLab extension. Every name in this release was diffed against a live
server's `ListT2IParams` registry, and a YuE2 song was generated through the client to confirm the server
consumes them (`unused_parameters` reported only the stock image params a music model ignores).

### Added

- **54 AudioLab music parameters** on `GenerationRequest`, covering the providers the client previously could
  not drive at all: **YuE2** (17), **YuE v1** (8), **HeartMuLa** (4), **MiniMax Music 3** (3), and ACE-Step's
  solver, LM-planner and audio-to-audio task groups (22). Each block names its server feature flag.
- **`Text2AudioDuration`** (`textaudioduration`), the stock SwarmUI clip-length parameter. AudioLab's backend
  reads it *first* and falls back to `MaxDuration`, so a caller setting only the latter was on a different code
  path than the UI.

### Fixed

- `MaxDuration` was documented as "Server range 1–300" under an AudioCraft heading. It is **1–900** and applies
  to every AudioLab generation provider, not just AudioCraft.
- `Lyrics` was documented as the lyrics parameter for "music models that sing". It is **ACE-Step only** — YuE2,
  YuE v1, HeartMuLa and MiniMax Music 3 each have their own, and sending this one to them does nothing.

### Notes

- YuE2 names its parameters `Song *` (the pass that renders audio) and `Score *` (the pass that plans an ABC
  score) rather than a `YuE2` prefix, because SwarmUI strips digits from parameter ids and a prefixed name would
  collide with YuE v1. The C# properties keep the `Yue2` prefix; the wire names do not.
- ~100 AudioLab TTS, STT and cloud-provider parameters remain uncovered. Music was this release's scope.

## 0.9.7-beta

### Changed

- `GenerationRequest` became a partial class and the AudioLab-only parameters moved to
  `Extensions/AudioLab/Contracts/GenerationRequest.AudioLab.cs`, so each extension's surface sits with the rest
  of that extension. Same wire payload and same names.
- Dropped the image-to-3D parameters added in 0.9.6. They were named after HartsyInference's own request type
  rather than a registered SwarmUI parameter, and no SwarmUI extension exposes image-to-3D yet.

## 0.9.6-beta

### Added

- Image-to-3D parameters on `GenerationRequest`. Reverted in 0.9.7.

## 0.9.5-beta

### Fixed

- `StreamGenerationAsync` accepts a request carrying an audio input instead of a prompt, because speech-to-text
  and voice conversion are driven by a clip rather than words.
- `Sampler` is nullable so it can be omitted entirely. Several video models sample with their own solver and
  SwarmUI refuses any request that names a sampler at all.

## 0.9.4-beta / 0.9.3-beta

Never released. The version counter jumped 0.9.2 to 0.9.5 in a single commit.

## 0.9.2-beta

### Added

- Text-to-video parameters (`textvideoframes`, `videofps`, `videoformat`) and AudioLab's music, sound-effect and
  speech parameters on `GenerationRequest`, each carrying its exact SwarmUI wire name. All nullable, so
  `NullValueHandling.Ignore` keeps them out of payloads that do not set them.

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
