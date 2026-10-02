# SwarmUI.ApiClient Changelog

## 0.13.1-beta

Completes the AudioLab and HartsyInference parameter coverage 0.13.0-beta's move disclosed as a pre-existing
gap. Additive and non-breaking: every property from 0.13.0-beta keeps its exact wire name, type, and location.

### Added

- **99 AudioLab parameters**, closing its coverage from 73/172 to 172/172. Added to
  `AudioLabGenerationParams` (`Extensions/AudioLab/Contracts/AudioLabGenerationParams.cs`), grouped into 33
  new `#region`s matching the server's own `AudioLabParams.cs` region-per-provider layout exactly: the
  shared TTS sampling knobs (`Volume`, `Temperature`, `TopP`, `TopK`, `MinP`, `RepetitionPenalty`,
  `StreamChunkSize`), then one region per TTS/STT/clone/FX provider --
  Bark, Chatterbox, Kokoro, Piper, Orpheus, CSM, VibeVoice, Dia, F5-TTS, ZipVoice, Zonos, Fish Speech,
  Qwen3-TTS, MeloTTS, StyleTTS 2, Spark-TTS, CosyVoice, Pocket TTS, Kyutai TTS, Whisper, AssemblyAI,
  ElevenLabs, Azure, Deepgram, Google, OpenAI, Cartesia, PlayHT, Dolby, Amazon Polly, RVC, GPT-SoVITS, and
  Resemble Enhance. Every wire id, type, and default was read from the registration call itself in the
  server source (`SwarmUI-AudioLab` at commit `83edfcb`), not guessed from the display name; a handful of
  C# property names were given a provider prefix the server's own field name lacks (e.g. `Speaker` ->
  `CsmSpeaker`, `Speed` -> `F5Speed`) to stay unambiguous in one flat class -- the `[JsonProperty]` wire id is
  what reaches the server either way and is unchanged from the registration.
- **2 HartsyInference parameters**, closing its coverage from 51/53 to 53/53: `H3ChainTotalFrames`
  (`hchaintotalframes`) and `H3ChainContextFrames` (`hchaincontextframes`), MiniMax-H3's long-form chaining
  controls, added to a new `#region MiniMax-H3 Chaining` in `HartsyInferenceGenerationParams`.
- **`GenerationRequestWireNameTests.AudioLabCoverageAdditions_AreAllCarried`,
  `HartsyInferenceCoverageAdditions_AreAllCarried`,
  `AudioLabGenerationParams_ModelsEveryServerRegisteredParameter`, and
  `HartsyInferenceGenerationParams_ModelsEveryServerRegisteredParameter`**: the first two assert every id
  this release adds is carried to the wire; the latter two pin the exact modeled-parameter count (172, 53) so
  a future accidental removal or duplicate fails loudly. The pre-existing reflection-based tests
  (`EveryWireName_IsRegisteredOnTheServer`, `NoTwoPropertiesClaimTheSameWireName`,
  `CreateGenerationPayload_EveryPropertyReachesTheWire`) needed no changes to cover the new properties --
  they already walk every property on these types.

### Fixed

- **`Snapshots/t2i-param-ids.txt` was missing `hchaincontextframes`/`hchaintotalframes`.** The snapshot's own
  `EveryWireName_IsRegisteredOnTheServer` test caught it immediately: MiniMax-H3 chaining was added to the
  HartsyInference server extension after this snapshot's 2026-09-17 live capture, so those two ids were
  genuinely absent from it even though the server registers them today. No live server was available to
  redo the capture; added both ids by hand, confirmed directly against the registration call in
  `SwarmUIHartsyInference.cs` rather than assumed. Every other id in the snapshot is still the unmodified
  2026-09-17 capture.

### Notes

- `Tests/Fixtures/wire/exhaustive-every-property.json` is regenerated to include the 101 new properties.
  The other seven fixtures are unaffected (each sets only specific, already-covered properties) and are
  unchanged byte-for-byte.
- `AudioLabGenerationParams`'s and `HartsyInferenceGenerationParams`'s coverage notes (XML doc remarks) are
  updated to 172/172 and 53/53.

## 0.13.0-beta

Typed support for AudioLab's new real-time voice agent session (SwarmUI-AudioLab PR #34) and LLM Assistant's
matching stateless turn endpoint; a factory for reaching that voice session client from
`client.Extensions.AudioLab`; and a breaking cleanup that moves every extension-registered `GenerationRequest`
parameter off the core type and into its own extension's parameter group.

### Breaking changes

- **Extension-registered generation parameters no longer live on `GenerationRequest` itself.** Three server
  extensions were contributing parameters straight onto the core type through a C# `partial class` (AudioLab,
  SwarmUI-API-Backends, HartsyInference), and a fourth — five LLMAssistant parameters — had no partial file at
  all and sat directly in `GenerationRequest.cs`. That put every extension's full parameter surface on
  `GenerationRequest`'s own public API regardless of which extensions a given server or caller actually uses.
  They now live on a typed params class per extension — `AudioLabGenerationParams`, `APIBackendsGenerationParams`,
  `HartsyInferenceGenerationParams`, and the new `LLMAssistantGenerationParams` — attached through one slot,
  `GenerationRequest.Extensions` (type `GenerationRequestExtensionParams`, one property per extension, each
  defaulting to an empty instance rather than null). **The wire format is unchanged**: every property still
  serializes under the exact same top-level JSON key as before, flattened into the same payload by
  `GenerationEndpoint.CreateGenerationPayload` — only the C# shape moved, proven by byte-identical-JSON tests
  against fixtures captured from the pre-move code. See the PR body for the full before/after member map.
  - **Migration:** `request.Foo` for an extension-owned `Foo` becomes `request.Extensions.<Extension>.Foo` —
    for example `request.AudioOutputFormat` becomes `request.Extensions.AudioLab.AudioOutputFormat`,
    `request.SafetyTolerance` becomes `request.Extensions.APIBackends.SafetyTolerance`,
    `request.CfgRescale` becomes `request.Extensions.HartsyInference.CfgRescale`, and
    `request.LlmModelId` becomes `request.Extensions.LLMAssistant.LlmModelId`. Stock SwarmUI parameters such as
    `request.Text2AudioStyle` or `request.InitImage` are unaffected. Each extension's own params type also moved
    namespace, from `SwarmUI.ApiClient.Contracts.Requests` to `SwarmUI.ApiClient.Extensions.<Extension>.Contracts`.
  - Also moved for the same reason, having landed on the core type by mistake rather than by design:
    `FaceIDV2Weight` (`faceidvweight`) is HartsyInference-registered, not stock SwarmUI/ComfyUI like the
    IP-Adapter parameters it sat beside — it now lives on `HartsyInferenceGenerationParams`.
- **`GetInstallationProgressAsync` called a route the server has never registered.** `AudioLabEndpoint` and
  `IAudioLabEndpoint` had a `GetInstallationProgressAsync` method posting to `"GetInstallationProgress"`, with an
  `AudioInstallationProgressResponse` contract shaped like a percent/step/package progress poll. Grepping the
  AudioLab server's actual `API.RegisterAPICall` calls (`AudioAPI/AudioLabAPI.cs`) turns up no such route at
  all -- only `GetInstallationStatus`, whose response is `{success, engine_available, engine_ready, providers}`,
  already correctly modeled by the existing `AudioInstallationStatusResponse` and already wired up as
  `GetInstallationStatusAsync`. This was never a shape mismatch to reconcile: every call through
  `GetInstallationProgressAsync` would have failed outright against a real server, 100% of the time. Removed the
  method and its dead contract class; added a test pinning `GetInstallationStatusAsync`'s real request (no body)
  and response shape, which had no test at all before this.

### Added

- **`AudioLabVoiceSessionClient`** (`Extensions/AudioLab/AudioLabVoiceSessionClient.cs`, new): a duplex client
  for `AudioLabVoiceSession`, AudioLab's phone-call-style WebSocket route. Its wire protocol mixes JSON (the
  `start` handshake, events, the client's `end`) with binary audio (mono PCM16 both directions, server replies
  additionally turn-tagged with a 4-byte little-endian id), so it cannot go through the existing
  `ISwarmWebSocketClient.StreamFramesAsync`, which only ever UTF8-decodes every message. Exposes `Events`
  (`IAsyncEnumerable<VoiceSessionEvent>`: state/transcript/bargein/tool_call/tool_result/notice/metrics/error) and
  `ReplyAudio` (`IAsyncEnumerable<VoiceSessionAudioFrame>`: decoded, turn-tagged reply audio) as two independent
  streams fed by one background pump, `SendAudioAsync` for mic input, and `EndAsync`/`DisposeAsync` that both
  complete the WebSocket close handshake rather than aborting the connection. Shares `SwarmWebSocketClient`'s own
  connect-retry, auth, and close-handshake logic through a new internal `WebSocketConnectionHelpers` (extracted
  from `SwarmWebSocketClient`'s private `ApplyAuth`/connect-pipeline/`GracefulCloseAsync`, which now delegate to
  it) so both clients get the same behavior rather than a second implementation that could drift.
- **`VoiceTurnRequest`/`VoiceTurnUpdate`** and **`ILLMAssistantEndpoint.StreamVoiceTurnAsync`**
  (`Extensions/LLMAssistant/`): typed support for `LLMAssistantVoiceTurnWS`, a stateless turn endpoint (the
  caller sends the full conversation every turn; nothing is persisted to a thread) streaming
  chunk/native_tool_call/tool_result/notice/done/error frames. Pure JSON, unlike the AudioLab route above, so it
  reuses the existing `StreamFramesAsync` plumbing directly -- the close handshake it already performs in its own
  `finally` block needed nothing added for this route specifically (pinned by a new test that drives the real
  `SwarmWebSocketClient` over a scripted socket and asserts the close, not just the happy-path frames).
- **`IAudioLabEndpoint.CreateVoiceSession`**: a factory that reaches `AudioLabVoiceSessionClient` from
  `client.Extensions.AudioLab.CreateVoiceSession()` instead of constructing it by hand. Required threading
  `SwarmClientOptions` and `ISessionManager` through the composition root — `SwarmClient`'s two `SwarmExtensions`
  construction sites, `SwarmExtensions` itself, down to `AudioLabEndpoint` — as new constructor overloads
  alongside the existing ones, so direct construction of `AudioLabEndpoint`, `SwarmExtensions`, and
  `AudioLabVoiceSessionClient` itself all keep working unchanged. An `AudioLabEndpoint` built through the old,
  options-less constructor throws `InvalidOperationException` from `CreateVoiceSession` instead of failing some
  other way. The created client's own `ConnectAsync(start, sessionKey, ...)` still takes its session key
  per-call exactly as before — `CreateVoiceSession` does not implicitly bind it to whatever session key the
  endpoint itself was scoped to (e.g. via `client.ForSession(...)`); pass it explicitly to `ConnectAsync` if that
  matters for the caller.
- **`AudioLabGenerationParams`, `APIBackendsGenerationParams`, `HartsyInferenceGenerationParams`,
  `LLMAssistantGenerationParams`** and **`GenerationRequest.Extensions`** (`GenerationRequestExtensionParams`):
  see Breaking changes above.

### Notes

- `Tests/TestDoubles.cs`'s `FakeClientWebSocket` gained a `BinaryFrame` step (and tracks sent binary messages
  separately from text ones) for the new AudioLab voice session tests; existing text-only scripts and assertions
  are unaffected.
- Parameters registered by SwarmUI's own built-in backend extensions — `ComfyUIBackendExtension`,
  `DynamicThresholdingExtension`, `GridGeneratorExtension`, `AutoWebUIBackendExtension` under
  `BuiltinExtensions/` in the SwarmUI source tree — stay on the core `GenerationRequest`. They ship inside the
  main SwarmUI repository and are present on every install, unlike the separately-distributed `src/Extensions/*`
  projects (AudioLab, API-Backends, HartsyInference, LLMAssistant, MagicPrompt) that `ISwarmExtensions` models;
  there is no server-extension slot for a built-in to move into. This includes all three ControlNet units, whose
  wire names (`controlnet[two/three]*`) are loop-registered with an interpolated name
  (`$"ControlNet{suffix} ..."`) in `ComfyUIBackendExtension.cs` rather than a literal string — which is also why
  an automated id-vs-registration diff initially missed them; they were hand-verified against the source instead.
- Out of scope for this pass, noted for whoever next works on parameter coverage: AudioLab's server extension
  registers 172 T2I parameters; `AudioLabGenerationParams` carries 73 of them (the gap predates this release).
  `HartsyInferenceGenerationParams` carries 51 of the server's 53.

## 0.12.0-beta

Closes the gap between `GenerationRequest` and SwarmUI's actual registered parameter list. A diff of this file
against a live `ListT2IParams` response (620 registered ids) turned up 368 with no property at all — everything
from `maskimage` and `initimagemode` to entire ControlNet/IP-Adapter/VRAM-management families and two dozen
fal.ai-fronted video providers. 362 of those are added here; the remaining 4 are internal UI-only placeholder
params (`placeholderparamgroup*`) that do nothing and were correctly left out.

### Added

- **206 stock/ComfyUI properties on `GenerationRequest`**: the mask/init-image family (`MaskImage`,
  `MaskBlur`/`MaskGrow`/`MaskShrinkGrow`, `InitImageNoise`, `InitImageRecompositeMask`, ...), `PromptImages` /
  `PromptAudios` / `PromptVideos`, `VideoEndImage`, ControlNet units 1–3, IP-Adapter, FreeU, Dynamic Thresholding,
  EasyCache/TeaCache, VAE tiling controls, regional prompting, segment refining, variation seed, SeedVR restore,
  Grid Generator overrides, and the Swarm-internal/LLM-prompt-processing groups. All verified end-to-end with real
  generations against a local ComfyUI backend, not just checked against the registry.
- **`Extensions/APIBackends/Contracts/GenerationRequest.APIBackends.cs`** (new file): 106 new properties for the
  ~25 additional providers the SwarmUI-API-Backends extension fronts through fal.ai (Kling, Luma, Veo, Pika, Sora,
  Wan 2.2–2.7, Seedance, Hunyuan, Vidu, PixVerse, Kandinsky, Recraft, Nano Banana 2, GPT-Image-2, FLUX.2/3, and
  more), grouped by provider. The five providers previously declared inline on `GenerationRequest` (BFL, OpenAI,
  Ideogram, Google, Grok — 22 properties) move into this same file unchanged, so every paid-API-backend parameter
  now lives in one place instead of being split between the core file and here.
- **`Extensions/HartsyInference/Contracts/GenerationRequest.HartsyInference.cs`** (new file): 50 properties
  specific to that extension — ACE-Step editing/planner, Wan-Animate, Ideogram 4, MiniMax Music, audio reference,
  and (see Fixed) `CfgRescale`, `InitImageMode`, the Restore/SeedVR family, and the VRAM & Memory family.

### Fixed

- **16 properties were briefly placed on the core `GenerationRequest` under the assumption that CFG Rescale, Init
  Image Mode, Restore/SeedVR and VRAM management are generic ComfyUI features.** Live-testing against a real,
  vanilla ComfyUI backend proved otherwise: all 16 come back with "Request requires flag 'hartsyinference' which
  is not present on the backend". Checked against SwarmUI's own source, `SwarmUIHartsyInference.cs` is the only
  place any of them are registered. They now live on the HartsyInference extension file instead, where a backend
  mismatch is expected rather than surprising.

### Notes

- Live-verified with real generations (small SD1.5 checkpoint, low step count) rather than registry inspection
  alone: FreeU, Dynamic Thresholding, Self-/Perturbed-Attention Guidance, Rescale CFG, Swarm Internal flags,
  Variation Seed, the mask/init-image family, EasyCache/TeaCache, Grid Generator overrides, and ControlNet units
  1–3 with a real canny model. IP-Adapter's parameters reach the server with exactly the value SwarmUI's own
  dropdown advertised, but the installed `ComfyUI_IPAdapter_plus` node version rejects it — a SwarmUI/ComfyUI-node
  compatibility issue, not a defect here.
- Not live-exercised (verified against the registry only, same bar as every property added before this release):
  video-gated properties, SAM2/segment refining, regional prompting/GLIGEN, the text-encoder-swap family
  (SD3/Flux-class models), LLM prompt processing, and — per the paid-API-backend properties' own nature — every
  fal.ai/BFL/OpenAI/Ideogram/Google/Grok and HartsyInference-only property.
- `loras` / `loraweights` were excluded from the added set: they are already handled by `Loras` (`[JsonIgnore]`)
  and `GenerationEndpoint.CreateGenerationPayload`'s post-pass, so a direct property would have raced it for the
  same wire keys.

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
