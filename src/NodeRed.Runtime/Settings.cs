// ============================================================
// SOURCE: packages/node_modules/@node-red/runtime/lib/settings.js
// ============================================================
// Copyright JS Foundation and other contributors, http://js.foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ============================================================

using System.Text.Json;
using NodeRed.Util;

namespace NodeRed.Runtime;

/// <summary>
/// Storage interface for settings persistence.
/// Translated from: @node-red/runtime/lib/storage/index.js
/// </summary>
public interface ISettingsStorage
{
    /// <summary>
    /// Get stored settings.
    /// </summary>
    Task<Dictionary<string, object?>?> GetSettingsAsync();

    /// <summary>
    /// Save settings.
    /// </summary>
    Task SaveSettingsAsync(Dictionary<string, object?> settings);
}

/// <summary>
/// Runtime settings manager.
/// Translated from: @node-red/runtime/lib/settings.js
/// </summary>
public class Settings
{
    // ============================================================
    // ORIGINAL CODE (lines 23-33):
    // ------------------------------------------------------------
    // // localSettings are those provided in the runtime settings.js file
    // var localSettings = null;
    // // globalSettings are provided by storage - .config.json on localfilesystem
    // var globalSettings = null;
    // // nodeSettings are those settings that a node module defines as being available
    // var nodeSettings = null;
    // // A subset of globalSettings that deal with per-user settings
    // var userSettings = null;
    // var disableNodeSettings = null;
    // var storage = null;
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private Dictionary<string, object?> _localSettings = new();
    private Dictionary<string, object?>? _globalSettings;
    private Dictionary<string, NodeSettingOptions> _nodeSettings = new();
    private Dictionary<string, object?>? _userSettings;
    private Dictionary<string, bool> _disableNodeSettings = new();
    private ISettingsStorage? _storage;
    private readonly object _lock = new();
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 36-52):
    // ------------------------------------------------------------
    // init: function(settings) {
    //     localSettings = settings;
    //     for (var i in settings) {
    //         if (settings.hasOwnProperty(i) && i !== 'load' && ...) {
    //             (function() {
    //                 var j = i;
    //                 persistentSettings.__defineGetter__(j, ...);
    //                 persistentSettings.__defineSetter__(j, ...);
    //             })();
    //         }
    //     }
    //     globalSettings = null;
    //     nodeSettings = {};
    //     disableNodeSettings = {};
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Initialize settings with local settings.
    /// </summary>
    /// <param name="settings">The local settings dictionary</param>
    public void Init(Dictionary<string, object?> settings)
    {
        lock (_lock)
        {
            _localSettings = new Dictionary<string, object?>(settings);
            _globalSettings = null;
            _nodeSettings = new Dictionary<string, NodeSettingOptions>();
            _disableNodeSettings = new Dictionary<string, bool>();
            _userSettings = null;
            _storage = null;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 53-63):
    // ------------------------------------------------------------
    // load: function(_storage) {
    //     storage = _storage;
    //     return storage.getSettings().then(function(_settings) {
    //         globalSettings = _settings;
    //         if (globalSettings) {
    //             userSettings = globalSettings.users || {};
    //         } else {
    //             userSettings = {};
    //         }
    //     });
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Load settings from storage.
    /// </summary>
    /// <param name="storage">The storage to load from</param>
    public async Task LoadAsync(ISettingsStorage storage)
    {
        _storage = storage;
        var settings = await storage.GetSettingsAsync();
        lock (_lock)
        {
            _globalSettings = settings ?? new Dictionary<string, object?>();
            if (_globalSettings.TryGetValue("users", out var users) && users is Dictionary<string, object?> userDict)
            {
                _userSettings = userDict;
            }
            else
            {
                _userSettings = new Dictionary<string, object?>();
            }
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 65-76):
    // ------------------------------------------------------------
    // get: function(prop) {
    //     if (prop === 'users') {
    //         throw new Error("Do not access user settings directly...");
    //     }
    //     if (localSettings.hasOwnProperty(prop)) {
    //         return clone(localSettings[prop]);
    //     }
    //     if (globalSettings === null) {
    //         throw new Error(log._("settings.not-available"));
    //     }
    //     return clone(globalSettings[prop]);
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get a setting value.
    /// </summary>
    /// <param name="prop">The property name</param>
    /// <returns>The setting value (cloned)</returns>
    public object? Get(string prop)
    {
        if (prop == "users")
        {
            throw new InvalidOperationException("Do not access user settings directly. Use GetUserSettings");
        }

        lock (_lock)
        {
            if (_localSettings.TryGetValue(prop, out var localValue))
            {
                return Clone(localValue);
            }

            if (_globalSettings is null)
            {
                throw new InvalidOperationException("Settings not available");
            }

            _globalSettings.TryGetValue(prop, out var globalValue);
            return Clone(globalValue);
        }
    }

    /// <summary>
    /// Get a setting value with a default.
    /// </summary>
    /// <typeparam name="T">The expected type</typeparam>
    /// <param name="prop">The property name</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>The setting value</returns>
    public T? Get<T>(string prop, T? defaultValue = default)
    {
        try
        {
            var value = Get(prop);
            if (value is null)
            {
                return defaultValue;
            }
            if (value is T typedValue)
            {
                return typedValue;
            }
            if (value is JsonElement je)
            {
                return JsonSerializer.Deserialize<T>(je.GetRawText());
            }
            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 78-96):
    // ------------------------------------------------------------
    // set: function(prop,value) {
    //     if (prop === 'users') {
    //         throw new Error("Do not access user settings directly...");
    //     }
    //     if (localSettings.hasOwnProperty(prop)) {
    //         throw new Error(log._("settings.property-read-only", {prop:prop}));
    //     }
    //     if (globalSettings === null) {
    //         throw new Error(log._("settings.not-available"));
    //     }
    //     var current = globalSettings[prop];
    //     globalSettings[prop] = clone(value);
    //     try {
    //         assert.deepEqual(current,value);
    //     } catch(err) {
    //         return storage.saveSettings(clone(globalSettings));
    //     }
    //     return Promise.resolve();
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Set a setting value.
    /// </summary>
    /// <param name="prop">The property name</param>
    /// <param name="value">The value to set</param>
    public async Task SetAsync(string prop, object? value)
    {
        if (prop == "users")
        {
            throw new InvalidOperationException("Do not access user settings directly. Use SetUserSettings");
        }

        Dictionary<string, object?> settingsToSave;
        bool needsSave;

        lock (_lock)
        {
            if (_localSettings.ContainsKey(prop))
            {
                throw new InvalidOperationException($"Property '{prop}' is read-only");
            }

            if (_globalSettings is null)
            {
                throw new InvalidOperationException("Settings not available");
            }

            _globalSettings.TryGetValue(prop, out var current);
            var clonedValue = Clone(value);
            _globalSettings[prop] = clonedValue;

            needsSave = !NodeRed.Util.Util.CompareObjects(current, value);
            settingsToSave = CloneDictionary(_globalSettings);
        }

        if (needsSave && _storage is not null)
        {
            await _storage.SaveSettingsAsync(settingsToSave);
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 97-109):
    // ------------------------------------------------------------
    // delete: function(prop) {
    //     if (localSettings.hasOwnProperty(prop)) {
    //         throw new Error(log._("settings.property-read-only", {prop:prop}));
    //     }
    //     if (globalSettings === null) {
    //         throw new Error(log._("settings.not-available"));
    //     }
    //     if (globalSettings.hasOwnProperty(prop)) {
    //         delete globalSettings[prop];
    //         return storage.saveSettings(clone(globalSettings));
    //     }
    //     return Promise.resolve();
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Delete a setting.
    /// </summary>
    /// <param name="prop">The property name</param>
    public async Task DeleteAsync(string prop)
    {
        Dictionary<string, object?>? settingsToSave = null;

        lock (_lock)
        {
            if (_localSettings.ContainsKey(prop))
            {
                throw new InvalidOperationException($"Property '{prop}' is read-only");
            }

            if (_globalSettings is null)
            {
                throw new InvalidOperationException("Settings not available");
            }

            if (_globalSettings.Remove(prop))
            {
                settingsToSave = CloneDictionary(_globalSettings);
            }
        }

        if (settingsToSave is not null && _storage is not null)
        {
            await _storage.SaveSettingsAsync(settingsToSave);
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 111-113):
    // ------------------------------------------------------------
    // available: function() {
    //     return (globalSettings !== null);
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Check if settings are available.
    /// </summary>
    /// <returns>True if settings are loaded</returns>
    public bool Available()
    {
        lock (_lock)
        {
            return _globalSettings is not null;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 115-126):
    // ------------------------------------------------------------
    // reset: function() {
    //     for (var i in localSettings) {
    //         if (localSettings.hasOwnProperty(i)) {
    //             delete persistentSettings[i];
    //         }
    //     }
    //     localSettings = null;
    //     globalSettings = null;
    //     userSettings = null;
    //     storage = null;
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Reset all settings.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _localSettings = new Dictionary<string, object?>();
            _globalSettings = null;
            _userSettings = null;
            _storage = null;
            _nodeSettings.Clear();
            _disableNodeSettings.Clear();
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 127-137):
    // ------------------------------------------------------------
    // registerNodeSettings: function(type, opts) {
    //     var normalisedType = util.normaliseNodeTypeName(type);
    //     for (var property in opts) {
    //         if (opts.hasOwnProperty(property)) {
    //             if (!property.startsWith(normalisedType)) {
    //                 throw new Error("Registered invalid property name...");
    //             }
    //         }
    //     }
    //     nodeSettings[type] = opts;
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Register node settings.
    /// </summary>
    /// <param name="type">The node type</param>
    /// <param name="opts">The settings options</param>
    public void RegisterNodeSettings(string type, NodeSettingOptions opts)
    {
        var normalisedType = NodeRed.Util.Util.NormaliseNodeTypeName(type);
        foreach (var property in opts.Properties.Keys)
        {
            if (!property.StartsWith(normalisedType))
            {
                throw new InvalidOperationException(
                    $"Registered invalid property name '{property}'. Properties for this node must start with '{normalisedType}'");
            }
        }

        lock (_lock)
        {
            _nodeSettings[type] = opts;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 138-160):
    // ------------------------------------------------------------
    // exportNodeSettings: function(safeSettings) { ... },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Export node settings to safe settings.
    /// </summary>
    /// <param name="safeSettings">The safe settings to export to</param>
    /// <returns>The updated safe settings</returns>
    public Dictionary<string, object?> ExportNodeSettings(Dictionary<string, object?> safeSettings)
    {
        lock (_lock)
        {
            foreach (var kvp in _nodeSettings)
            {
                var type = kvp.Key;
                var nodeTypeSettings = kvp.Value;

                if (_disableNodeSettings.TryGetValue(type, out var disabled) && disabled)
                {
                    continue;
                }

                foreach (var propKvp in nodeTypeSettings.Properties)
                {
                    var property = propKvp.Key;
                    var setting = propKvp.Value;

                    if (setting.Exportable)
                    {
                        if (safeSettings.ContainsKey(property))
                        {
                            // Cannot overwrite existing setting
                            continue;
                        }
                        
                        if (_localSettings.TryGetValue(property, out var localValue))
                        {
                            safeSettings[property] = localValue;
                        }
                        else if (setting.Value is not null)
                        {
                            safeSettings[property] = setting.Value;
                        }
                    }
                }
            }
        }

        return safeSettings;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 161-165):
    // ------------------------------------------------------------
    // enableNodeSettings: function(types) {
    //     types.forEach(function(type) {
    //         disableNodeSettings[type] = false;
    //     });
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Enable node settings for specified types.
    /// </summary>
    /// <param name="types">The types to enable</param>
    public void EnableNodeSettings(IEnumerable<string> types)
    {
        lock (_lock)
        {
            foreach (var type in types)
            {
                _disableNodeSettings[type] = false;
            }
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 166-170):
    // ------------------------------------------------------------
    // disableNodeSettings: function(types) {
    //     types.forEach(function(type) {
    //         disableNodeSettings[type] = true;
    //     });
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Disable node settings for specified types.
    /// </summary>
    /// <param name="types">The types to disable</param>
    public void DisableNodeSettings(IEnumerable<string> types)
    {
        lock (_lock)
        {
            foreach (var type in types)
            {
                _disableNodeSettings[type] = true;
            }
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 171-173):
    // ------------------------------------------------------------
    // getUserSettings: function(username) {
    //     return clone(userSettings[username]);
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get user settings.
    /// </summary>
    /// <param name="username">The username</param>
    /// <returns>The user settings (cloned)</returns>
    public Dictionary<string, object?>? GetUserSettings(string username)
    {
        lock (_lock)
        {
            if (_userSettings?.TryGetValue(username, out var settings) == true &&
                settings is Dictionary<string, object?> userDict)
            {
                return CloneDictionary(userDict);
            }
            return null;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 174-187):
    // ------------------------------------------------------------
    // setUserSettings: function(username,settings) {
    //     if (globalSettings === null) {
    //         throw new Error(log._("settings.not-available"));
    //     }
    //     var current = userSettings[username];
    //     userSettings[username] = settings;
    //     try {
    //         assert.deepEqual(current,settings);
    //         return Promise.resolve();
    //     } catch(err) {
    //         globalSettings.users = userSettings;
    //         return storage.saveSettings(clone(globalSettings));
    //     }
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Set user settings.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="settings">The settings to set</param>
    public async Task SetUserSettingsAsync(string username, Dictionary<string, object?> settings)
    {
        Dictionary<string, object?>? settingsToSave = null;

        lock (_lock)
        {
            if (_globalSettings is null)
            {
                throw new InvalidOperationException("Settings not available");
            }

            _userSettings ??= new Dictionary<string, object?>();
            
            _userSettings.TryGetValue(username, out var current);
            var needsSave = !NodeRed.Util.Util.CompareObjects(current, settings);

            _userSettings[username] = settings;
            _globalSettings["users"] = _userSettings;

            if (needsSave)
            {
                settingsToSave = CloneDictionary(_globalSettings);
            }
        }

        if (settingsToSave is not null && _storage is not null)
        {
            await _storage.SaveSettingsAsync(settingsToSave);
        }
    }
    // ============================================================

    /// <summary>
    /// Check if a local setting exists.
    /// </summary>
    /// <param name="prop">The property name</param>
    /// <returns>True if the setting exists locally</returns>
    public bool HasProperty(string prop)
    {
        lock (_lock)
        {
            return _localSettings.ContainsKey(prop);
        }
    }

    /// <summary>
    /// Indexer for accessing settings.
    /// </summary>
    public object? this[string prop]
    {
        get => Get(prop);
    }

    private static object? Clone(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<object>(json);
    }

    private static Dictionary<string, object?> CloneDictionary(Dictionary<string, object?> dict)
    {
        var json = JsonSerializer.Serialize(dict);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
    }
}

/// <summary>
/// Node setting options.
/// </summary>
public class NodeSettingOptions
{
    /// <summary>
    /// Properties and their settings.
    /// </summary>
    public Dictionary<string, NodeSettingProperty> Properties { get; set; } = new();
}

/// <summary>
/// Node setting property configuration.
/// </summary>
public class NodeSettingProperty
{
    /// <summary>
    /// Whether this setting is exportable to the client.
    /// </summary>
    public bool Exportable { get; set; }

    /// <summary>
    /// The default value.
    /// </summary>
    public object? Value { get; set; }
}
