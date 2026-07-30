# SwarmUI Extension Endpoints

Everything in this folder targets a **SwarmUI server extension**, not the stock SwarmUI API.

Stock SwarmUI endpoints live in `Endpoints/` and are exposed directly on `ISwarmClient`
(`client.Models`, `client.Generation`, ...). Extension endpoints live here and are exposed under
`ISwarmClient.Extensions` (`client.Extensions.MagicPrompt`, ...), so an extension dependency is
visible in the folder tree, the namespace, and the call site.

## Supported extensions

| Extension | Folder | Accessed via | Endpoints | Repository |
| --- | --- | --- | --- | --- |
| AudioLab | `AudioLab/` | `client.Extensions.AudioLab` | 18 | https://github.com/HartsyAI/SwarmUI-AudioLab |
| LLM Assistant | `LLMAssistant/` | `client.Extensions.LLMAssistant` | 51 | https://github.com/HartsyAI/SwarmUI-LLMAssistant |
| MagicPrompt | `MagicPrompt/` | `client.Extensions.MagicPrompt` | 1 | https://github.com/HartsyAI/SwarmUI-MagicPromptExtension |

The exact endpoint names each extension adds are listed in its `ExtensionInfo`, and are reported at
runtime through `client.Extensions.All`.

## Where audio and LLM work actually happens

Two of these extensions also register things into stock SwarmUI, so not every feature is reached
through this folder:

- **AudioLab** registers audio T2I parameters and an audio backend type, so generating audio from a
  prompt runs through `client.Generation` with an audio model selected. `Extensions/AudioLab` covers
  what that pipeline does not: direct synthesis and transcription, engine and model management,
  format conversion, time stretch, and DAW project storage.
- **LLM Assistant** registers `LLM` as a SwarmUI model type, so listing LLM model *files* works
  through the stock `client.Models` endpoint with the LLM subtype. Use
  `client.Extensions.LLMAssistant.GetModelsAsync` when you need the models a live LLM provider is
  actually serving, which is a different question.

## Layout of an extension folder

```
Extensions/<ExtensionName>/
├── I<ExtensionName>Endpoint.cs     # interface, inherits ISwarmExtensionEndpoint
├── <ExtensionName>Endpoint.cs      # implementation, exposes static ExtensionInfo
└── Contracts/                      # request and response contracts owned by this extension
```

## Adding an extension

1. Create `Extensions/<ExtensionName>/` and put every request/response contract in its `Contracts/`
   subfolder. Extension contracts never go in the top level `Contracts/` folder, which is reserved
   for stock SwarmUI.
2. Declare `I<ExtensionName>Endpoint : ISwarmExtensionEndpoint`.
3. In the implementation, expose a `public static readonly SwarmExtensionInfo ExtensionInfo`
   describing the extension name, repository, and the endpoint names it adds, and return it from
   the `Extension` property.
4. Add a property to `ISwarmExtensions` / `SwarmExtensions` and append `ExtensionInfo` to `All`.
5. Add the extension to the table above.

## Runtime discovery

`client.Extensions.All` returns `SwarmExtensionInfo` for every supported extension, so a host can
log or gate features on extension availability rather than discovering a missing extension through
a failed request.
