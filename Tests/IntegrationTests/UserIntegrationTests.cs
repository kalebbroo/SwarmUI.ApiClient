using System;
using System.Threading;
using System.Threading.Tasks;
using SwarmUI.ApiClient.Models.Responses;
using Xunit;

namespace SwarmUI.ApiClient.Tests.IntegrationTests
{
    /// <summary>Integration tests for the User endpoint against a real SwarmUI instance.</summary>
    /// <remarks>Verify user-related operations and require a running SwarmUI instance at the configured test URL.</remarks>
    public class UserIntegrationTests : SwarmIntegrationTestBase
    {
        [Fact]
        public async Task GetUserSettingsAsync_ReturnsUserSettings()
        {
            UserSettingsResponse response = await Client.User.GetUserSettingsAsync(CancellationToken.None);
            Assert.NotNull(response);
            if (response.Settings != null)
            {
                Assert.IsAssignableFrom<System.Collections.Generic.Dictionary<string, object>>(response.Settings);
            }
        }

        [Fact]
        public async Task GetUserSettingsAsync_SettingsHaveExpectedStructure()
        {
            UserSettingsResponse response = await Client.User.GetUserSettingsAsync(CancellationToken.None);
            Assert.NotNull(response);
            if (response.Settings != null && response.Settings.Count > 0)
            {
                foreach (System.Collections.Generic.KeyValuePair<string, object> setting in response.Settings)
                {
                    Assert.False(string.IsNullOrEmpty(setting.Key), "Setting key should not be empty");
                }
            }
        }

        [Fact]
        public async Task GetUserSettingsAsync_CanUseHelperMethod()
        {
            UserSettingsResponse response = await Client.User.GetUserSettingsAsync(CancellationToken.None);
            Assert.NotNull(response);
            string theme = response.GetSetting("theme", "light");
            int steps = response.GetSetting("default_steps", 20);
            bool showAdvanced = response.GetSetting("show_advanced", false);
            Assert.NotNull(theme);
            Assert.True(steps >= 0);
        }

        [Fact]
        public async Task GetUserSettingsAsync_CalledMultipleTimes_ReturnsConsistentData()
        {
            UserSettingsResponse response1 = await Client.User.GetUserSettingsAsync(CancellationToken.None);
            UserSettingsResponse response2 = await Client.User.GetUserSettingsAsync(CancellationToken.None);

            Assert.Equal(response1.Settings?.Count ?? 0, response2.Settings?.Count ?? 0);
        }

        [Fact]
        public async Task GetUserSettingsAsync_CanBeCancelled()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await Client.User.GetUserSettingsAsync(cts.Token);
            });
        }

        [Fact]
        public async Task GetUserSettingsAsync_ResponseDeserializesCorrectly()
        {
            UserSettingsResponse response = await Client.User.GetUserSettingsAsync(CancellationToken.None);

            Assert.NotNull(response);

            if (response.Settings != null)
            {
                Assert.IsAssignableFrom<System.Collections.Generic.Dictionary<string, object>>(response.Settings);

                foreach (System.Collections.Generic.KeyValuePair<string, object> setting in response.Settings)
                {
                    Assert.NotNull(setting.Key);
                }
            }
        }

        [Fact]
        public async Task GetMyUserDataAsync_ReturnsUserData()
        {
            UserDataResponse response = await Client.User.GetMyUserDataAsync(CancellationToken.None);

            Assert.NotNull(response);
        }
    }
}
