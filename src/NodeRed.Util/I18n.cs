// ============================================================
// SOURCE: packages/node_modules/@node-red/util/lib/i18n.js
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
using System.Text.RegularExpressions;

namespace NodeRed.Util;

/// <summary>
/// Internationalization utilities for Node-RED.
/// Translated from: @node-red/util/lib/i18n.js
/// </summary>
public class I18n
{
    // ============================================================
    // ORIGINAL CODE (line 28):
    // ------------------------------------------------------------
    // var defaultLang = "en-US";
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// The default language of the runtime.
    /// </summary>
    public const string DefaultLang = "en-US";
    // ============================================================

    private readonly Dictionary<string, ResourceMapEntry> _resourceMap = new();
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, object>>> _resourceCache = new();
    private string _currentLang = DefaultLang;
    private bool _initialized;
    private readonly object _lock = new();

    private class ResourceMapEntry
    {
        public string BaseDir { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = new();
    }

    // ============================================================
    // ORIGINAL CODE (lines 158-185):
    // ------------------------------------------------------------
    // function init(settings) {
    //     if (!initPromise) {
    //         initPromise = new Promise((resolve,reject) => {
    //             i18n.use(MessageFileLoader);
    //             var opt = { ... };
    //             var lang = settings.lang || getCurrentLocale();
    //             if (lang) { opt.lng = lang; }
    //             i18n.init(opt, function() { resolve(); });
    //         });
    //     }
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Initialize the i18n system with settings.
    /// </summary>
    /// <param name="settings">The settings object</param>
    public void Init(I18nSettings? settings)
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            _currentLang = settings?.Lang ?? GetCurrentLocale() ?? DefaultLang;
            _initialized = true;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 147-156):
    // ------------------------------------------------------------
    // function getCurrentLocale() {
    //     var env = process.env;
    //     for (var name of ['LC_ALL', 'LC_MESSAGES', 'LANG']) {
    //         if (name in env) {
    //             var val = env[name];
    //             return val.substring(0, 2);
    //         }
    //     }
    //     return undefined;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private static string? GetCurrentLocale()
    {
        foreach (var name in new[] { "LC_ALL", "LC_MESSAGES", "LANG" })
        {
            var val = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(val))
            {
                return val.Length >= 2 ? val.Substring(0, 2) : val;
            }
        }
        return null;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 38-43):
    // ------------------------------------------------------------
    // function registerMessageCatalogs(catalogs) {
    //     var promises = catalogs.map(function(catalog) {
    //         return registerMessageCatalog(catalog.namespace,catalog.dir,catalog.file).catch(err => {});
    //     });
    //     return Promise.all(promises);
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Register multiple message catalogs.
    /// </summary>
    /// <param name="catalogs">The catalogs to register</param>
    public async Task RegisterMessageCatalogsAsync(IEnumerable<MessageCatalog> catalogs)
    {
        var tasks = catalogs.Select(c => RegisterMessageCatalogAsync(c.Namespace, c.Dir, c.File));
        await Task.WhenAll(tasks);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 49-69):
    // ------------------------------------------------------------
    // async function registerMessageCatalog(namespace,dir,file) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Register a message catalog.
    /// </summary>
    /// <param name="ns">The namespace</param>
    /// <param name="dir">The directory containing language folders</param>
    /// <param name="file">The file name</param>
    public async Task RegisterMessageCatalogAsync(string ns, string dir, string file)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                _resourceMap[ns] = new ResourceMapEntry
                {
                    BaseDir = dir,
                    File = file,
                    Languages = new List<string>()
                };

                if (Directory.Exists(dir))
                {
                    foreach (var langDir in Directory.GetDirectories(dir))
                    {
                        var langName = Path.GetFileName(langDir);
                        var filePath = Path.Combine(langDir, file);
                        if (File.Exists(filePath))
                        {
                            _resourceMap[ns].Languages.Add(langName);
                        }
                    }
                }
            }
        });

        // Pre-load the default language
        await LoadNamespaceAsync(ns, DefaultLang);
    }

    private async Task LoadNamespaceAsync(string ns, string lang)
    {
        try
        {
            await ReadFileAsync(lang, ns);
        }
        catch
        {
            // Ignore errors during pre-loading
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 71-81):
    // ------------------------------------------------------------
    // function mergeCatalog(fallback,catalog) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private static void MergeCatalog(Dictionary<string, object> fallback, Dictionary<string, object> catalog)
    {
        foreach (var kvp in fallback)
        {
            if (!catalog.ContainsKey(kvp.Key))
            {
                catalog[kvp.Key] = kvp.Value;
            }
            else if (kvp.Value is Dictionary<string, object> fallbackDict && 
                     catalog[kvp.Key] is Dictionary<string, object> catalogDict)
            {
                MergeCatalog(fallbackDict, catalogDict);
            }
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 83-97):
    // ------------------------------------------------------------
    // function migrateMessageCatalogV3toV4(catalog) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private static Dictionary<string, object> MigrateMessageCatalogV3ToV4(Dictionary<string, object> catalog)
    {
        var keys = catalog.Keys.ToList();
        foreach (var key in keys)
        {
            if (catalog[key] is Dictionary<string, object> nestedDict)
            {
                catalog[key] = MigrateMessageCatalogV3ToV4(nestedDict);
            }
            else if (key.EndsWith("_plural"))
            {
                var otherKey = key.Replace("_plural", "_other");
                if (!catalog.ContainsKey(otherKey))
                {
                    catalog[otherKey] = catalog[key];
                }
                catalog.Remove(key);
            }
        }
        return catalog;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 99-127):
    // ------------------------------------------------------------
    // async function readFile(lng, ns) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private async Task<Dictionary<string, object>> ReadFileAsync(string lng, string ns)
    {
        // Validate language to prevent path traversal
        if (Regex.IsMatch(lng, @"[^a-z\-]", RegexOptions.IgnoreCase))
        {
            throw new ArgumentException($"Invalid language: {lng}");
        }

        lock (_lock)
        {
            if (_resourceCache.TryGetValue(ns, out var nsCache) && 
                nsCache.TryGetValue(lng, out var cached))
            {
                return cached;
            }
        }

        if (!_resourceMap.TryGetValue(ns, out var resource))
        {
            throw new ArgumentException("Unrecognised namespace");
        }

        var filePath = Path.Combine(resource.BaseDir, lng, resource.File);
        var content = await File.ReadAllTextAsync(filePath);

        // Remove BOM if present
        if (content.StartsWith("\uFEFF"))
        {
            content = content.Substring(1);
        }

        var catalog = JsonSerializer.Deserialize<Dictionary<string, object>>(content) 
                      ?? new Dictionary<string, object>();

        // Migrate v3 to v4 format
        catalog = MigrateMessageCatalogV3ToV4(catalog);

        lock (_lock)
        {
            if (!_resourceCache.ContainsKey(ns))
            {
                _resourceCache[ns] = new Dictionary<string, Dictionary<string, object>>();
            }
            _resourceCache[ns][lng] = catalog;

            // Merge with base language if applicable
            var baseLng = lng.Split('-')[0];
            if (baseLng != lng && _resourceCache[ns].TryGetValue(baseLng, out var baseCatalog))
            {
                MergeCatalog(baseCatalog, catalog);
            }
            if (lng != DefaultLang && _resourceCache[ns].TryGetValue(DefaultLang, out var defaultCatalog))
            {
                MergeCatalog(defaultCatalog, catalog);
            }
        }

        return catalog;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 194-211):
    // ------------------------------------------------------------
    // function getCatalog(namespace,lang) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Gets a message catalog.
    /// </summary>
    /// <param name="ns">The namespace</param>
    /// <param name="lang">The language (optional, defaults to defaultLang)</param>
    /// <returns>The catalog dictionary or null if not found</returns>
    public Dictionary<string, object>? GetCatalog(string ns, string? lang = null)
    {
        lang ??= DefaultLang;

        // Validate language
        if (Regex.IsMatch(lang, @"[^a-z\-]", RegexOptions.IgnoreCase))
        {
            throw new ArgumentException($"Invalid language: {lang}");
        }

        lock (_lock)
        {
            if (_resourceCache.TryGetValue(ns, out var nsCache))
            {
                if (nsCache.TryGetValue(lang, out var result))
                {
                    return result;
                }

                // Try base language
                var langParts = lang.Split('-');
                if (langParts.Length == 2 && nsCache.TryGetValue(langParts[0], out result))
                {
                    return result;
                }
            }
        }

        return null;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 219-223):
    // ------------------------------------------------------------
    // function availableLanguages(namespace) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Gets a list of languages a given catalog is available in.
    /// </summary>
    /// <param name="ns">The namespace</param>
    /// <returns>List of language codes or null</returns>
    public List<string>? AvailableLanguages(string ns)
    {
        lock (_lock)
        {
            if (_resourceMap.TryGetValue(ns, out var resource))
            {
                return resource.Languages.ToList();
            }
        }
        return null;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 247-255):
    // ------------------------------------------------------------
    // obj['_'] = function() {
    //     var res = i18n.t.apply(i18n,arguments);
    //     return res;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Perform a message catalog lookup.
    /// </summary>
    /// <param name="key">The message key (format: "namespace:key.path")</param>
    /// <param name="options">Optional interpolation options</param>
    /// <returns>The translated string</returns>
    public string Translate(string key, Dictionary<string, object>? options = null)
    {
        // Parse namespace:key format
        var parts = key.Split(':', 2);
        string ns;
        string keyPath;
        
        if (parts.Length == 2)
        {
            ns = parts[0];
            keyPath = parts[1];
        }
        else
        {
            ns = "runtime";
            keyPath = key;
        }

        var catalog = GetCatalog(ns, _currentLang);
        if (catalog is null)
        {
            return key;
        }

        var result = GetNestedValue(catalog, keyPath);
        if (result is null)
        {
            return key;
        }

        var text = result.ToString() ?? key;

        // Handle interpolation (format: __varname__)
        if (options is not null)
        {
            foreach (var kvp in options)
            {
                text = text.Replace($"__{kvp.Key}__", kvp.Value?.ToString() ?? "");
            }
        }

        return text;
    }

    /// <summary>
    /// Alias for Translate method - matches the JavaScript _() function.
    /// </summary>
    public string _(string key, Dictionary<string, object>? options = null)
    {
        return Translate(key, options);
    }

    private static object? GetNestedValue(Dictionary<string, object> dict, string keyPath)
    {
        var keys = keyPath.Split('.');
        object? current = dict;

        foreach (var k in keys)
        {
            if (current is Dictionary<string, object> d)
            {
                if (!d.TryGetValue(k, out current))
                {
                    return null;
                }
            }
            else if (current is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty(k, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        if (current is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => current
            };
        }

        return current;
    }
    // ============================================================

    /// <summary>
    /// Gets the current language.
    /// </summary>
    public string CurrentLanguage => _currentLang;
}

/// <summary>
/// I18n settings.
/// </summary>
public class I18nSettings
{
    /// <summary>
    /// The language to use.
    /// </summary>
    public string? Lang { get; set; }
}

/// <summary>
/// Message catalog registration info.
/// </summary>
public class MessageCatalog
{
    /// <summary>
    /// The namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// The directory containing language folders.
    /// </summary>
    public string Dir { get; set; } = string.Empty;

    /// <summary>
    /// The file name.
    /// </summary>
    public string File { get; set; } = string.Empty;
}
