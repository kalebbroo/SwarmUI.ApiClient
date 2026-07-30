# SwarmUI Extension Endpoints

Everything in this folder targets a **SwarmUI server extension**, not the stock SwarmUI API.

Stock SwarmUI endpoints live in `Endpoints/` and are exposed directly on `ISwarmClient`
(`client.Models`, `client.Generation`, ...). Extension endpoints live here and are exposed under
`ISwarmClient.Extensions` (`client.Extensions.MagicPrompt`, ...), so an extension dependency is
visible in the folder tree, the namespace, and the call site.

## Supported extensions

| Extension | Folder | Endpoints | Repository |
| --- | --- | --- | --- |
| MagicPrompt | `MagicPrompt/` | `MagicPromptPhoneHome` | https://github.com/HartsyAI/SwarmUI-MagicPromptExtension |

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
