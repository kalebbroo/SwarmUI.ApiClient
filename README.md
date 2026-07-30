# SwarmUI API Client Library

**Professional C# client library for SwarmUI API**

🚧 **v0.7.0-beta** 🚧

SwarmUI.ApiClient is a strongly-typed C# wrapper around the SwarmUI API, providing first-class support for text-to-image generation, model management, presets, user data, backends, and admin operations. The core implementation is in place and covered by unit tests; the API surface may still evolve before a 1.0.0 stable release.

## Project Structure

Stock SwarmUI and SwarmUI *extensions* are deliberately kept in separate trees. Anything under
`Endpoints/` works against a vanilla SwarmUI server; anything under `Extensions/` requires a
specific extension to be installed on that server.

```
SwarmUI.ApiClient/
├── SwarmClient.cs                 # Main client class
├── ISwarmClient.cs                # Main client interface
├── SwarmClientOptions.cs          # Configuration options
├── SwarmClientServiceCollectionExtensions.cs   # AddSwarmClient DI registration
│
├── Sessions/                      # Session management
│   ├── ISessionManager.cs
│   └── SessionManager.cs
│
├── Http/                          # HTTP communication
│   ├── ISwarmHttpClient.cs
│   └── SwarmHttpClient.cs
│
├── WebSockets/                    # WebSocket streaming
│   ├── ISwarmWebSocketClient.cs
│   └── SwarmWebSocketClient.cs
│
├── Endpoints/                     # Stock SwarmUI API endpoint groups
│   ├── Generation/                # Text-to-image generation
│   ├── Models/                    # Model management
│   ├── Backends/                  # Backend servers
│   ├── Presets/                   # Parameter presets
│   ├── User/                      # User settings
│   └── Admin/                     # Admin operations
│
├── Contracts/                     # Wire contracts for stock SwarmUI endpoints
│   ├── Requests/                  # Request contracts
│   ├── Responses/                 # Response contracts
│   ├── Common/                    # Shared contracts
│   └── Enums/                     # Contract enums
│
├── Exceptions/                    # Custom exceptions
│   ├── SwarmException.cs
│   ├── SwarmSessionException.cs
│   ├── SwarmAuthenticationException.cs
│   └── SwarmWebSocketException.cs
│
└── Extensions/                    # SwarmUI server extensions, one folder per extension
    ├── README.md                  # Supported extension registry
    ├── ISwarmExtensions.cs        # Exposed as ISwarmClient.Extensions
    ├── ISwarmExtensionEndpoint.cs # Implemented by every extension endpoint group
    ├── SwarmExtensionInfo.cs      # Extension metadata for runtime discovery
    └── MagicPrompt/               # MagicPrompt extension
        ├── IMagicPromptEndpoint.cs
        ├── MagicPromptEndpoint.cs
        └── Contracts/             # Contracts owned by this extension
```

### Extension endpoints

Extension-backed endpoints are namespaced and accessed separately from the stock API, so a dependency
on a server-side extension is visible in the folder tree, the namespace, and the call site:

```csharp
// Stock SwarmUI - works against any server
ModelListResponse models = await client.Models.ListModelsAsync("Stable-Diffusion");

// Extension - requires SwarmUI-MagicPromptExtension installed on the server
MagicPromptResponse enhanced = await client.Extensions.MagicPrompt.EnhancePromptAsync(request);

// Which extensions this client supports
foreach (SwarmExtensionInfo info in client.Extensions.All)
{
    Console.WriteLine($"{info.DisplayName} -> {info.RepositoryUrl}");
}
```

See [`Extensions/README.md`](./Extensions/README.md) for the supported extension registry and the
steps for adding a new one.

## Changelog

This README gives a high level snapshot. For detailed release notes, see:

- [`Docs/CHANGELOG.md`](./Docs/CHANGELOG.md)

Highlights for the current beta:

- First beta of `SwarmUI.ApiClient`: typed wrapper around SwarmUI HTTP + WebSocket APIs.
- Core infrastructure implemented: `SwarmClientOptions`, `SessionManager`, `SwarmHttpClient`, `SwarmWebSocketClient`, and the `SwarmClient` facade.
- Endpoint coverage for generation, models, backends, presets, user, and admin operations.
- Unit tests in the `SwarmTests` project cover HTTP behavior, sessions, streaming generation, model management, presets, and client wiring.

## Upcoming Features

Planned improvements for future releases include:

- Retry and resilience policies using Polly (configurable via `SwarmClientOptions`).
- Integration tests against a real SwarmUI instance.
- Optional examples project / samples that mirror the docs.
- CI/CD pipeline for automated build, test, pack, and publish to NuGet.
- Potential multi targeting support for additional .NET versions.

## Usage

### Standalone Usage
```csharp
SwarmClientOptions options = new SwarmClientOptions
{
    BaseUrl = "https://hartsy.ai",
    Authorization = "your-api-key"
};
using SwarmClient client = new SwarmClient(options);
GenerationRequest request = new GenerationRequest
{
    Prompt = "A beautiful sunset over mountains",
    Model = "flux-dev",
    Width = 1024,
    Height = 768
};
await foreach (GenerationUpdate update in client.Generation.StreamGenerationAsync(request))
{
    if (update.Type == "progress")
        Console.WriteLine($"Progress: {update.Progress.CurrentPercent}%");
    else if (update.Type == "image")
        SaveImage(update.Image.Image);
}
```

### Dependency Injection Usage
```csharp
// Program.cs - AddSwarmClient lives in the Microsoft.Extensions.DependencyInjection
// namespace, so no extra using directive is needed in a typical host.
builder.Services.AddSwarmClient(options =>
{
    options.BaseUrl = "https://hartsy.ai";
    options.Authorization = builder.Configuration["SwarmAuth"];
});

// YourService.cs
public class ImageService(ISwarmClient swarm)
{
    public async Task GenerateAsync()
    {
        // Use swarm...
    }
}
```

## Contributing

This library follows strict coding guidelines:
- No `var` keyword - always use explicit types
- No `private` fields - use public `Impl` struct pattern
- All public members must have XML documentation
- Follow .NET naming conventions
- Use `ConfigureAwait(false)` in library code

See detailed guidelines in [`Docs/CodingGuidelines.md`](./Docs/CodingGuidelines.md).

## Real-World Usage Examples

The HartsyWeb application uses `SwarmUI.ApiClient` in production for both internal and external APIs. For example, an ASP.NET Core controller can stream generation updates to the client using Server-Sent Events (SSE):

```csharp
[ApiController]
[Route("api/swarm")]
public class GenerateController(ISwarmClient swarmClient) : ControllerBase
{
    [HttpPost("generate")]
    public async Task Generate([FromBody] GenerationRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");

        await foreach (GenerationUpdate update in swarmClient.Generation.StreamGenerationAsync(request, cancellationToken))
        {
            // Write SSE event data and flush the response stream here.
        }
    }
}
```

See the HartsyWeb repository for full controller implementations and additional end-to-end examples.

## License

MIT License. See the `LICENSE` file in this folder.

## Links

- SwarmUI: https://github.com/mcmonkeyprojects/SwarmUI
- SwarmUI API Docs: https://github.com/mcmonkeyprojects/SwarmUI/blob/master/docs/API.md
