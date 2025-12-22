# SwarmUI.ApiClient Coding Guidelines and Implementation Notes

This document describes how `SwarmUI.ApiClient` is actually structured and implemented, and sets expectations for new contributions. It focuses on the client library only (not HartsyWeb in general).

## Goals

- Provide a **predictable, stable public API** via `ISwarmClient` and endpoint interfaces.
- Keep the implementation **testable**, **async-first**, and **resilient**.
- Maintain a consistent, readable coding style across all components.

## Coding style & naming

- **Indentation and whitespace**
  - Source files in this project use **spaces, not tabs** (4-space indentation). New code should follow this convention.
- **Explicit local types**
  - Do not use the `var` keyword in `SwarmUI.ApiClient` code. Use explicit types for locals, as seen throughout the library.
- **`Impl` struct pattern & internal state**
  - Core types (for example `SwarmClient`, `SwarmHttpClient`, `SwarmWebSocketClient`, `SessionManager`, endpoint classes) define a nested `public struct Impl` and a `public Impl Internal;` field.
  - Dependencies and shared state live inside `Impl` as public fields instead of private fields on the class.
  - Private helpers are expressed as private methods or properties that use `Internal`, not as private backing fields.
- **XML documentation**
  - Public types and members are expected to have XML documentation.
  - `SwarmUI.ApiClient.csproj` enables XML doc generation and suppresses warning 1591, so missing docs do not fail builds but should be treated as something to fix when touching that code.
- **Naming**
  - Use clear, concise names that describe what the thing *is* or *does* (for example `GenerationEndpoint`, `SessionManager`, `GetModelHashAsync`).
  - Avoid cryptic or overly abbreviated names except in well-understood cases (`i` in small loops, `X/Y/Z` for coordinates, etc.).
  - Do not encode long explanations into identifiers; put detail in XML docs instead.
- **`nameof` instead of magic strings**
  - When referencing members or types in exceptions, logging, or reflection, prefer `nameof(...)` over string literals (for example `nameof(settings)`).

## Async and ConfigureAwait

- Public surface for network operations is **async-first**:
  - HTTP methods return `Task` / `Task<T>`.
  - Streaming operations use `IAsyncEnumerable<T>`.
  - There are no synchronous wrappers around HTTP/WebSocket operations in this library.
- Inside library code, awaited calls use `.ConfigureAwait(false)` (see `SwarmClient`, `SwarmHttpClient`, `SessionManager`, WebSocket and endpoint implementations).
  - When editing internals, continue this pattern.

## Globalization

- For **machine-oriented data** (JSON payloads, IDs, logs, comparison keys):
  - Use `CultureInfo.InvariantCulture` when formatting/parsing numbers or dates (for example `lora.Weight.ToString("F1", CultureInfo.InvariantCulture)` in the generation endpoint).
  - Use ordinal string comparisons (`StringComparison.Ordinal` / `StringComparison.OrdinalIgnoreCase`) where applicable.
- Only localize text when it is truly **end-user UI**; most of `SwarmUI.ApiClient` does not produce end-user UI.

## Public API surface

- **`ISwarmClient`**
  - Primary entry point for consumers.
  - Exposes endpoint properties:
    - `Generation`, `Models`, `Backends`, `Presets`, `User`, `Admin`.
  - Implements `IAsyncDisposable` via `SwarmClient.DisposeAsync`.
  - Provides `GetHealthAsync` for a basic server health check (currently implemented via session creation and timing).

- **Endpoints**
  - Endpoint interfaces and concrete classes group related API areas:
    - Generation: `IGenerationEndpoint` / `GenerationEndpoint`.
    - Models: `IModelsEndpoint` / `ModelsEndpoint`.
    - Similar patterns for backends, presets, user, and admin.
  - HTTP-only operations delegate to `ISwarmHttpClient`.
  - Streaming operations delegate to `ISwarmWebSocketClient`.

- **Models / DTOs**
  - Request/response types live under:
    - `SwarmUI.ApiClient.Models.Requests`
    - `SwarmUI.ApiClient.Models.Responses`
    - `SwarmUI.ApiClient.Models.Common`
  - `GenerationRequest` (plus `LoraModel`) is the primary high-level model for generation requests and is mapped into the WebSocket payload by `GenerationEndpoint.CreateGenerationPayload`.

## HTTP layer (SwarmHttpClient)

- Wraps `HttpClient` and applies SwarmUI-specific behavior:
  - Builds JSON payloads with `JObject` / `JsonConvert`.
  - Injects `session_id` using `SessionManager` for all endpoints except `GetNewSession`.
  - Logs requests and responses with truncation for large payloads.
- Error handling maps API/HTTP failures to:
  - `SwarmSessionException` when `error_id="invalid_session_id"` (and invalidates the session).
  - `SwarmException` for other error conditions (including non-success HTTP status codes).
- Endpoint implementations (`GenerationEndpoint`, `ModelsEndpoint`, etc.) call into `ISwarmHttpClient` for all HTTP operations.

## Sessions (SessionManager)

- Manages the SwarmUI session lifecycle:
  - Lazily creates a session via the `GetNewSession` API.
  - Caches the `session_id` and marks it invalid when `invalid_session_id` errors occur.
  - Uses a `SemaphoreSlim` and double-checked logic to ensure only one session creation at a time.
- Exposes:
  - `GetOrCreateSessionAsync`
  - `RefreshSessionAsync`
  - `InvalidateSession`
  - `CurrentSessionId` (for diagnostics only).

## WebSockets (SwarmWebSocketClient)

- Provides WebSocket communication for streaming operations such as:
  - `GenerateText2ImageWS`
  - `DoModelDownloadWS`
  - `SelectModelWS`
- Behavior:
  - Derives WebSocket base URL from `SwarmClientOptions.BaseUrl` (`http`→`ws`, `https`→`wss`).
  - Sets `Authorization` header when `SwarmClientOptions.Authorization` is non-empty.
  - Injects `session_id` into the initial payload using `SessionManager`.
  - Retries connection failures up to `MaxRetryAttempts`.
  - Tracks active connections and exposes `DisconnectAllAsync` plus `GracefulCloseAsync` for cleanup.

## Generation endpoint (GenerationEndpoint)

- Maps `GenerationRequest` into the actual WebSocket payload required by SwarmUI (see `CreateGenerationPayload`):
  - Controls image count, batch size, prompt, negative prompt, dimensions, steps, CFG, sampler/scheduler, style preset, seed, LoRAs, and img2img parameters.
- Streams `GenerationUpdate` messages via `StreamGenerationAsync`, parsing:
  - Status / backend status / supported features.
  - Progress (including previews).
  - Image messages (base64 encoded image, batch index, metadata).
  - Discard and error messages.
  - `keep_alive` messages (treated as a special `"keep_alive"` update type).
- Also exposes HTTP-based helpers:
  - `GetCurrentStatusAsync`
  - `InterruptAllAsync`
  - `ListT2IParamsAsync`
  - `TriggerRefreshAsync`
  - `ServerDebugMessageAsync`

## Models endpoint (ModelsEndpoint)

- HTTP-based operations:
  - `ListModelsAsync`, `DescribeModelAsync`, `DeleteModelAsync`, `DeleteWildcardAsync`, `GetModelHashAsync`,
    `EditModelMetadataAsync`, `EditWildcardAsync`, `ForwardMetadataRequestAsync`, `ListLoadedModelsAsync`,
    `RenameModelAsync`, `SelectModelAsync`.
- WebSocket-based operations:
  - `StreamModelDownloadAsync` via `DoModelDownloadWS`.
  - `StreamModelSelectionAsync` via `SelectModelWS`.
- Uses the shared `ISwarmHttpClient`, `ISwarmWebSocketClient`, and `ISessionManager` from the client’s `Impl`.

## Authorization header behavior

- `SwarmClientOptions.Authorization` is applied directly to the HTTP `Authorization` header via
  `SwarmClient.ConfigureAuthorizationHeader`:
  - If `Authorization` is non-empty, `DefaultRequestHeaders.Authorization`
    is set accordingly on the underlying `HttpClient`.
- Both standalone usage (`new SwarmClient(options)`) and DI usage (`AddSwarmClient`) delegate to `ConfigureAuthorizationHeader` so header behavior is consistent.

## Dependency injection usage

- `SwarmUI.ApiClient.Extensions.ServiceCollectionExtensions.AddSwarmClient` configures SwarmUI.ApiClient for ASP.NET Core DI:
  - Registers `SwarmClientOptions` with the options pattern and exposes it as a singleton.
  - Registers a typed `HttpClient` for `ISwarmClient` / `SwarmClient` via `AddHttpClient`, setting:
    - `BaseAddress` and `Timeout` from `SwarmClientOptions`.
    - `Authorization` header via `SwarmClient.ConfigureAuthorizationHeader`.
  - Registers:
    - `ISessionManager` as a singleton (`SessionManager`).
    - `ISwarmHttpClient` and `ISwarmWebSocketClient` as transient services.
- This matches how HartsyWeb controllers obtain an `ISwarmClient` instance.

## Testing & real-world usage

- **Tests**
  - Unit tests live in the `SwarmTests` project (under the `Tests/` folder). Current tests focus on:
    - Payload shaping and parsing for `GenerationEndpoint` and `ModelsEndpoint`.
    - Streaming behavior with fake WebSocket clients.
    - HTTP payload shaping with recording HTTP clients.
- **HartsyWeb usage**
  - `GenerateAPIController` uses `ISwarmClient.Generation` and related endpoints to:
    - Stream image generation to web clients via SSE.
    - Work with presets, parameters, and user data.
  - `ExternalAPIController` uses `ISwarmClient.Generation` to stream generation results for external consumers (for example, Discord bots).

These guidelines should be updated whenever the public API or core implementation patterns of `SwarmUI.ApiClient` change (for example, new endpoints, new retry logic, or changes to session/authorization behavior).
