// ============================================================
// SOURCE: packages/node_modules/@node-red/runtime/lib/storage/index.js
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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NodeRed.Util;

namespace NodeRed.Runtime;

/// <summary>
/// Flow configuration result.
/// </summary>
public class FlowsConfig
{
    /// <summary>
    /// The flows array.
    /// </summary>
    public List<Dictionary<string, object?>> Flows { get; set; } = new();

    /// <summary>
    /// The credentials.
    /// </summary>
    public Dictionary<string, object?> Credentials { get; set; } = new();

    /// <summary>
    /// The revision hash.
    /// </summary>
    public string Rev { get; set; } = string.Empty;

    /// <summary>
    /// Whether credentials have changed.
    /// </summary>
    public bool CredentialsDirty { get; set; }
}

/// <summary>
/// Storage module interface.
/// Translated from: @node-red/runtime/lib/storage/index.js
/// </summary>
public interface IStorage : ISettingsStorage
{
    /// <summary>
    /// Initialize storage.
    /// </summary>
    Task InitAsync(IRuntime runtime);

    /// <summary>
    /// Get flows.
    /// </summary>
    Task<List<Dictionary<string, object?>>> GetFlowsAsync();

    /// <summary>
    /// Save flows.
    /// </summary>
    Task SaveFlowsAsync(List<Dictionary<string, object?>> flows, string? user = null);

    /// <summary>
    /// Get credentials.
    /// </summary>
    Task<Dictionary<string, object?>> GetCredentialsAsync();

    /// <summary>
    /// Save credentials.
    /// </summary>
    Task SaveCredentialsAsync(Dictionary<string, object?> credentials);

    /// <summary>
    /// Get sessions.
    /// </summary>
    Task<Dictionary<string, object?>?> GetSessionsAsync();

    /// <summary>
    /// Save sessions.
    /// </summary>
    Task SaveSessionsAsync(Dictionary<string, object?> sessions);

    /// <summary>
    /// Get library entry.
    /// </summary>
    Task<object> GetLibraryEntryAsync(string type, string path);

    /// <summary>
    /// Save library entry.
    /// </summary>
    Task SaveLibraryEntryAsync(string type, string path, Dictionary<string, object?> meta, string body);
}

/// <summary>
/// Storage module wrapper with flow/credential handling.
/// Translated from: @node-red/runtime/lib/storage/index.js
/// </summary>
public class Storage : IStorage
{
    private IStorage? _storageModule;
    private IRuntime? _runtime;
    private readonly SemaphoreSlim _settingsSaveMutex = new(1, 1);

    /// <summary>
    /// Check for malicious path.
    /// </summary>
    private static bool IsMalicious(string path)
    {
        return path.Contains("../") || path.Contains("..\\");
    }

    // ============================================================
    // ORIGINAL CODE (lines 52-72):
    // ------------------------------------------------------------
    // init: async function(_runtime) {
    //     runtime = _runtime;
    //     storageModule = moduleSelector(runtime.settings);
    //     ...
    //     return storageModule.init(runtime.settings, runtime);
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Initialize storage with the runtime.
    /// </summary>
    /// <param name="runtime">The runtime</param>
    public async Task InitAsync(IRuntime runtime)
    {
        _runtime = runtime;
        // For now, use the provided storage module from settings or default
        // In a full implementation, this would select the appropriate module
        _storageModule = runtime.Settings.Get<IStorage>("storageModule") 
            ?? new LocalFileSystemStorage();
        
        await _storageModule.InitAsync(runtime);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 73-84):
    // ------------------------------------------------------------
    // getFlows: async function() {
    //     return storageModule.getFlows().then(function(flows) {
    //         return storageModule.getCredentials().then(function(creds) {
    //             var result = {
    //                 flows: flows,
    //                 credentials: creds
    //             };
    //             result.rev = crypto.createHash('sha256').update(JSON.stringify(result.flows)).digest("hex");
    //             return result;
    //         })
    //     });
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get flows with credentials and revision hash.
    /// </summary>
    /// <returns>The flows configuration</returns>
    public async Task<FlowsConfig> GetFlowsConfigAsync()
    {
        if (_storageModule is null)
        {
            throw new InvalidOperationException("Storage not initialized");
        }

        var flows = await _storageModule.GetFlowsAsync();
        var credentials = await _storageModule.GetCredentialsAsync();

        var result = new FlowsConfig
        {
            Flows = flows,
            Credentials = credentials
        };

        var flowsJson = JsonSerializer.Serialize(result.Flows);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(flowsJson));
        result.Rev = Convert.ToHexString(hash).ToLowerInvariant();

        return result;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 85-101):
    // ------------------------------------------------------------
    // saveFlows: async function(config, user) {
    //     var flows = config.flows;
    //     var credentials = config.credentials;
    //     var credentialSavePromise;
    //     if (config.credentialsDirty) {
    //         credentialSavePromise = storageModule.saveCredentials(credentials);
    //     } else {
    //         credentialSavePromise = Promise.resolve();
    //     }
    //     delete config.credentialsDirty;
    //     return credentialSavePromise.then(function() {
    //         return storageModule.saveFlows(flows, user).then(function() {
    //             return crypto.createHash('sha256').update(JSON.stringify(config.flows)).digest("hex");
    //         })
    //     });
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Save flows configuration.
    /// </summary>
    /// <param name="config">The flows configuration</param>
    /// <param name="user">The user saving the flows</param>
    /// <returns>The new revision hash</returns>
    public async Task<string> SaveFlowsConfigAsync(FlowsConfig config, string? user = null)
    {
        if (_storageModule is null)
        {
            throw new InvalidOperationException("Storage not initialized");
        }

        if (config.CredentialsDirty)
        {
            await _storageModule.SaveCredentialsAsync(config.Credentials);
        }

        await _storageModule.SaveFlowsAsync(config.Flows, user);

        var flowsJson = JsonSerializer.Serialize(config.Flows);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(flowsJson));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    // ============================================================

    /// <summary>
    /// Get flows (delegated to storage module).
    /// </summary>
    public Task<List<Dictionary<string, object?>>> GetFlowsAsync()
    {
        if (_storageModule is null)
        {
            throw new InvalidOperationException("Storage not initialized");
        }
        return _storageModule.GetFlowsAsync();
    }

    /// <summary>
    /// Save flows (delegated to storage module).
    /// </summary>
    public Task SaveFlowsAsync(List<Dictionary<string, object?>> flows, string? user = null)
    {
        if (_storageModule is null)
        {
            throw new InvalidOperationException("Storage not initialized");
        }
        return _storageModule.SaveFlowsAsync(flows, user);
    }

    /// <summary>
    /// Get credentials (delegated to storage module).
    /// </summary>
    public Task<Dictionary<string, object?>> GetCredentialsAsync()
    {
        if (_storageModule is null)
        {
            throw new InvalidOperationException("Storage not initialized");
        }
        return _storageModule.GetCredentialsAsync();
    }

    /// <summary>
    /// Save credentials (delegated to storage module).
    /// </summary>
    public Task SaveCredentialsAsync(Dictionary<string, object?> credentials)
    {
        if (_storageModule is null)
        {
            throw new InvalidOperationException("Storage not initialized");
        }
        return _storageModule.SaveCredentialsAsync(credentials);
    }

    // ============================================================
    // ORIGINAL CODE (lines 108-114):
    // ------------------------------------------------------------
    // getSettings: async function() {
    //     if (settingsAvailable) {
    //         return storageModule.getSettings();
    //     } else {
    //         return null
    //     }
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get settings.
    /// </summary>
    public Task<Dictionary<string, object?>?> GetSettingsAsync()
    {
        return _storageModule?.GetSettingsAsync() ?? Task.FromResult<Dictionary<string, object?>?>(null);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 115-119):
    // ------------------------------------------------------------
    // saveSettings: async function(settings) {
    //     if (settingsAvailable) {
    //         return settingsSaveMutex.runExclusive(() => storageModule.saveSettings(settings))
    //     }
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Save settings with mutex protection.
    /// </summary>
    public async Task SaveSettingsAsync(Dictionary<string, object?> settings)
    {
        if (_storageModule is null)
        {
            return;
        }

        await _settingsSaveMutex.WaitAsync();
        try
        {
            await _storageModule.SaveSettingsAsync(settings);
        }
        finally
        {
            _settingsSaveMutex.Release();
        }
    }
    // ============================================================

    /// <summary>
    /// Get sessions.
    /// </summary>
    public Task<Dictionary<string, object?>?> GetSessionsAsync()
    {
        return _storageModule?.GetSessionsAsync() ?? Task.FromResult<Dictionary<string, object?>?>(null);
    }

    /// <summary>
    /// Save sessions.
    /// </summary>
    public Task SaveSessionsAsync(Dictionary<string, object?> sessions)
    {
        if (_storageModule is null)
        {
            return Task.CompletedTask;
        }
        return _storageModule.SaveSessionsAsync(sessions);
    }

    // ============================================================
    // ORIGINAL CODE (lines 135-142):
    // ------------------------------------------------------------
    // getLibraryEntry: async function(type, path) {
    //     if (is_malicious(path)) {
    //         var err = new Error();
    //         err.code = "forbidden";
    //         throw err;
    //     }
    //     return storageModule.getLibraryEntry(type, path);
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get a library entry.
    /// </summary>
    public Task<object> GetLibraryEntryAsync(string type, string path)
    {
        if (IsMalicious(path))
        {
            throw NodeRed.Util.Util.CreateError("forbidden", "Access forbidden");
        }

        if (_storageModule is null)
        {
            throw new InvalidOperationException("Storage not initialized");
        }

        return _storageModule.GetLibraryEntryAsync(type, path);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 143-150):
    // ------------------------------------------------------------
    // saveLibraryEntry: async function(type, path, meta, body) {
    //     if (is_malicious(path)) {
    //         var err = new Error();
    //         err.code = "forbidden";
    //         throw err;
    //     }
    //     return storageModule.saveLibraryEntry(type, path, meta, body);
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Save a library entry.
    /// </summary>
    public Task SaveLibraryEntryAsync(string type, string path, Dictionary<string, object?> meta, string body)
    {
        if (IsMalicious(path))
        {
            throw NodeRed.Util.Util.CreateError("forbidden", "Access forbidden");
        }

        if (_storageModule is null)
        {
            throw new InvalidOperationException("Storage not initialized");
        }

        return _storageModule.SaveLibraryEntryAsync(type, path, meta, body);
    }
    // ============================================================
}

/// <summary>
/// Placeholder local filesystem storage implementation.
/// This will be fully translated from localfilesystem/index.js
/// </summary>
public class LocalFileSystemStorage : IStorage
{
    private string _userDir = string.Empty;
    private string _flowsFile = "flows.json";
    private string _credentialsFile = "flows_cred.json";
    private string _settingsFile = ".config.json";
    private string _sessionsFile = ".sessions.json";

    public Task InitAsync(IRuntime runtime)
    {
        _userDir = runtime.Settings.Get<string>("userDir") ?? 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".node-red");
        
        if (!Directory.Exists(_userDir))
        {
            Directory.CreateDirectory(_userDir);
        }

        var flowsFileName = runtime.Settings.Get<string>("flowFile");
        if (!string.IsNullOrEmpty(flowsFileName))
        {
            _flowsFile = flowsFileName;
            _credentialsFile = Path.GetFileNameWithoutExtension(flowsFileName) + "_cred.json";
        }

        return Task.CompletedTask;
    }

    public async Task<List<Dictionary<string, object?>>> GetFlowsAsync()
    {
        var filePath = Path.Combine(_userDir, _flowsFile);
        if (!File.Exists(filePath))
        {
            return new List<Dictionary<string, object?>>();
        }

        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(content) 
            ?? new List<Dictionary<string, object?>>();
    }

    public async Task SaveFlowsAsync(List<Dictionary<string, object?>> flows, string? user = null)
    {
        var filePath = Path.Combine(_userDir, _flowsFile);
        var content = JsonSerializer.Serialize(flows, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, content);
    }

    public async Task<Dictionary<string, object?>> GetCredentialsAsync()
    {
        var filePath = Path.Combine(_userDir, _credentialsFile);
        if (!File.Exists(filePath))
        {
            return new Dictionary<string, object?>();
        }

        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(content) 
            ?? new Dictionary<string, object?>();
    }

    public async Task SaveCredentialsAsync(Dictionary<string, object?> credentials)
    {
        var filePath = Path.Combine(_userDir, _credentialsFile);
        var content = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, content);
    }

    public async Task<Dictionary<string, object?>?> GetSettingsAsync()
    {
        var filePath = Path.Combine(_userDir, _settingsFile);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(content);
    }

    public async Task SaveSettingsAsync(Dictionary<string, object?> settings)
    {
        var filePath = Path.Combine(_userDir, _settingsFile);
        var content = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, content);
    }

    public async Task<Dictionary<string, object?>?> GetSessionsAsync()
    {
        var filePath = Path.Combine(_userDir, _sessionsFile);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(content);
    }

    public async Task SaveSessionsAsync(Dictionary<string, object?> sessions)
    {
        var filePath = Path.Combine(_userDir, _sessionsFile);
        var content = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, content);
    }

    public Task<object> GetLibraryEntryAsync(string type, string path)
    {
        var libDir = Path.Combine(_userDir, "lib", type);
        var fullPath = Path.Combine(libDir, path);

        if (File.Exists(fullPath))
        {
            return Task.FromResult<object>(File.ReadAllText(fullPath));
        }

        if (Directory.Exists(fullPath))
        {
            var entries = new List<object>();
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                entries.Add(Path.GetFileName(dir));
            }
            foreach (var file in Directory.GetFiles(fullPath))
            {
                entries.Add(new Dictionary<string, object?> { { "fn", Path.GetFileName(file) } });
            }
            return Task.FromResult<object>(entries);
        }

        throw NodeRed.Util.Util.CreateError("not_found", $"Library entry not found: {path}");
    }

    public async Task SaveLibraryEntryAsync(string type, string path, Dictionary<string, object?> meta, string body)
    {
        var libDir = Path.Combine(_userDir, "lib", type);
        var fullPath = Path.Combine(libDir, path);
        var dir = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(fullPath, body);
    }
}
