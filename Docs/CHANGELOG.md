# SwarmUI.ApiClient Changelog

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
