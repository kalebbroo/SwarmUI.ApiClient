# SwarmUI.ApiClient Changelog

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
