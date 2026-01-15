// ============================================================
// Tests for NodeRed.Runtime.Settings
// ============================================================

using Xunit;
using NodeRed.Runtime;

namespace NodeRed.Runtime.Tests;

public class SettingsTests
{
    [Fact]
    public void Init_SetsLocalSettings()
    {
        // Arrange
        var settings = new Settings();
        var localSettings = new Dictionary<string, object?>
        {
            { "testKey", "testValue" }
        };

        // Act
        settings.Init(localSettings);

        // Assert
        Assert.Equal("testValue", settings.Get("testKey")?.ToString());
    }

    [Fact]
    public void Get_ThrowsForUsersProperty()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => settings.Get("users"));
        Assert.Contains("user settings", ex.Message.ToLower());
    }

    [Fact]
    public void Get_ThrowsWhenNotAvailable()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => settings.Get("nonExistentKey"));
        Assert.Contains("not available", ex.Message.ToLower());
    }

    [Fact]
    public void Available_ReturnsFalseBeforeLoad()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>());

        // Act & Assert
        Assert.False(settings.Available());
    }

    [Fact]
    public async Task LoadAsync_SetsGlobalSettings()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>());
        var storage = new MockSettingsStorage(new Dictionary<string, object?>
        {
            { "globalKey", "globalValue" }
        });

        // Act
        await settings.LoadAsync(storage);

        // Assert
        Assert.True(settings.Available());
        Assert.Equal("globalValue", settings.Get("globalKey")?.ToString());
    }

    [Fact]
    public async Task SetAsync_ThrowsForReadOnlyProperty()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>
        {
            { "readOnlyKey", "value" }
        });
        await settings.LoadAsync(new MockSettingsStorage());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => settings.SetAsync("readOnlyKey", "newValue"));
        Assert.Contains("read-only", ex.Message.ToLower());
    }

    [Fact]
    public void HasProperty_ReturnsTrueForLocalSetting()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>
        {
            { "localKey", "value" }
        });

        // Act & Assert
        Assert.True(settings.HasProperty("localKey"));
        Assert.False(settings.HasProperty("nonExistentKey"));
    }

    [Fact]
    public void Reset_ClearsAllSettings()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>
        {
            { "key", "value" }
        });

        // Act
        settings.Reset();

        // Assert
        Assert.False(settings.HasProperty("key"));
    }

    [Fact]
    public void RegisterNodeSettings_ValidatesPropertyNames()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>());

        // Act & Assert - should throw for invalid prefix
        Assert.Throws<InvalidOperationException>(() =>
            settings.RegisterNodeSettings("my-node", new NodeSettingOptions
            {
                Properties = new Dictionary<string, NodeSettingProperty>
                {
                    { "wrongPrefix", new NodeSettingProperty { Exportable = true } }
                }
            }));
    }

    [Fact]
    public void RegisterNodeSettings_AcceptsValidPropertyNames()
    {
        // Arrange
        var settings = new Settings();
        settings.Init(new Dictionary<string, object?>());

        // Act - should not throw
        settings.RegisterNodeSettings("my-node", new NodeSettingOptions
        {
            Properties = new Dictionary<string, NodeSettingProperty>
            {
                { "myNodeProperty", new NodeSettingProperty { Exportable = true, Value = "default" } }
            }
        });

        // Assert - export the settings
        var safeSettings = new Dictionary<string, object?>();
        settings.ExportNodeSettings(safeSettings);
        Assert.True(safeSettings.ContainsKey("myNodeProperty"));
    }

    private class MockSettingsStorage : ISettingsStorage
    {
        private Dictionary<string, object?>? _settings;

        public MockSettingsStorage(Dictionary<string, object?>? settings = null)
        {
            _settings = settings ?? new Dictionary<string, object?>();
        }

        public Task<Dictionary<string, object?>?> GetSettingsAsync()
        {
            return Task.FromResult(_settings);
        }

        public Task SaveSettingsAsync(Dictionary<string, object?> settings)
        {
            _settings = settings;
            return Task.CompletedTask;
        }
    }
}
