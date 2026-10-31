using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SwarmUI.ApiClient.Endpoints.Admin;
using SwarmUI.ApiClient.Endpoints.User;
using SwarmUI.ApiClient.Sessions;
using Xunit;

namespace SwarmUI.ApiClient.Tests.Endpoints;

/// <summary>The three settings writes, against how SwarmUI binds them: a <c>JObject</c> parameter receives the whole
/// request body (<c>src/WebAPI/APICallReflectBuilder.cs</c>), and each route reads its map from <c>settings</c> in it
/// (<c>BasicAPIFeatures.ChangeUserSettings</c>, <c>AdminAPI.AdminChangeUserSettings</c>, <c>AdminAPI.ChangeServerSettings</c>).
/// A body wrapped in <c>rawData</c> made the server throw on a null <c>settings</c>.</summary>
public class SettingsWireShapeTests
{
    private static readonly Dictionary<string, object> Change = new() { ["fileformat.reformattransientimages"] = true };

    [Fact]
    public async Task ChangeUserSettings_SendsTheMapUnderSettingsAtTheTop()
    {
        FakeSwarmHttpClient http = new();
        await new UserEndpoint(http, SwarmSessionKeys.Default).ChangeUserSettingsAsync(Change);
        (string endpoint, JObject? payload, _) = Assert.Single(http.Requests);
        Assert.Equal("ChangeUserSettings", endpoint);
        Assert.Equal(["settings"], payload!.Properties().Select(p => p.Name));
        Assert.True((bool)payload["settings"]!["fileformat.reformattransientimages"]!);
    }

    [Fact]
    public async Task AdminChangeUserSettings_SendsNameAndSettingsAtTheTop()
    {
        FakeSwarmHttpClient http = new();
        await new AdminEndpoint(http, SwarmSessionKeys.Default).ChangeUserSettingsAsync("bob", Change);
        (string endpoint, JObject? payload, _) = Assert.Single(http.Requests);
        Assert.Equal("AdminChangeUserSettings", endpoint);
        Assert.Equal(["name", "settings"], payload!.Properties().Select(p => p.Name));
        Assert.Equal("bob", (string?)payload["name"]);
        Assert.True((bool)payload["settings"]!["fileformat.reformattransientimages"]!);
    }

    [Fact]
    public async Task ChangeServerSettings_SendsTheMapUnderSettingsAtTheTop()
    {
        FakeSwarmHttpClient http = new();
        await new AdminEndpoint(http, SwarmSessionKeys.Default).ChangeServerSettingsAsync(new Dictionary<string, object> { ["defaultuserrestriction.maxt2isimultaneous"] = 4 });
        (string endpoint, JObject? payload, _) = Assert.Single(http.Requests);
        Assert.Equal("ChangeServerSettings", endpoint);
        Assert.Equal(["settings"], payload!.Properties().Select(p => p.Name));
        Assert.Equal(4, (int)payload["settings"]!["defaultuserrestriction.maxt2isimultaneous"]!);
    }
}
