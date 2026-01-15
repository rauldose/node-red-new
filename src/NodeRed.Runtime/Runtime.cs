// ============================================================
// SOURCE: packages/node_modules/@node-red/runtime/lib/index.js
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

using System.Reflection;
using NodeRed.Util;

namespace NodeRed.Runtime;

/// <summary>
/// Runtime interface for Node-RED components.
/// </summary>
public interface IRuntime
{
    /// <summary>
    /// The runtime settings.
    /// </summary>
    Settings Settings { get; }

    /// <summary>
    /// The storage module.
    /// </summary>
    Storage Storage { get; }

    /// <summary>
    /// The events emitter.
    /// </summary>
    Events Events { get; }

    /// <summary>
    /// The hooks system.
    /// </summary>
    Hooks Hooks { get; }

    /// <summary>
    /// The I18n system.
    /// </summary>
    I18n I18n { get; }

    /// <summary>
    /// Check if the runtime is started.
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    /// Get the runtime version.
    /// </summary>
    string Version { get; }
}

/// <summary>
/// The core runtime component of Node-RED.
/// Translated from: @node-red/runtime/lib/index.js
/// </summary>
public class Runtime : IRuntime
{
    // ============================================================
    // ORIGINAL CODE (lines 36-38):
    // ------------------------------------------------------------
    // var runtimeMetricInterval = null;
    // var started = false;
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private Timer? _runtimeMetricInterval;
    private bool _started;
    private string? _version;
    // ============================================================

    /// <summary>
    /// The runtime settings.
    /// </summary>
    public Settings Settings { get; } = new();

    /// <summary>
    /// The storage module.
    /// </summary>
    public Storage Storage { get; } = new();

    /// <summary>
    /// The events emitter.
    /// </summary>
    public Events Events { get; } = new();

    /// <summary>
    /// The hooks system.
    /// </summary>
    public Hooks Hooks { get; } = new();

    /// <summary>
    /// The I18n system.
    /// </summary>
    public I18n I18n { get; } = new();

    /// <summary>
    /// Check if the runtime is started.
    /// </summary>
    public bool IsStarted => _started;

    /// <summary>
    /// Get the runtime version.
    /// </summary>
    public string Version => GetVersion();

    // ============================================================
    // ORIGINAL CODE (lines 67-111):
    // ------------------------------------------------------------
    // function init(_userSettings,httpServer,_adminApi) {
    //     server = httpServer;
    //     userSettings = _userSettings;
    //     ...
    //     settings.init(userSettings);
    //     ...
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Initialize the runtime module.
    /// </summary>
    /// <param name="userSettings">The runtime settings</param>
    public void Init(Dictionary<string, object?> userSettings)
    {
        userSettings["version"] = GetVersion();
        Settings.Init(userSettings);
        Log.Init(null);
        I18n.Init(null);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 115-127):
    // ------------------------------------------------------------
    // function getVersion() {
    //     if (!version) {
    //         version = require(path.join(__dirname,"..","package.json")).version;
    //         try {
    //             fs.statSync(path.join(__dirname,"..","..","..","..",".git"));
    //             version += "-git";
    //         } catch(err) {}
    //     }
    //     return version;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private string GetVersion()
    {
        if (_version is null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            _version = version?.ToString() ?? "0.0.0";

            // Check for git directory
            var assemblyLocation = assembly.Location;
            var dir = Path.GetDirectoryName(assemblyLocation);
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(Path.Combine(dir, ".git")))
                {
                    _version += "-git";
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }
        }
        return _version;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 135-249):
    // ------------------------------------------------------------
    // function start() {
    //     return i18n.registerMessageCatalog(...)
    //         .then(function() { return storage.init(runtime)})
    //         .then(function() { return settings.load(storage)})
    //         ...
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Start the runtime.
    /// </summary>
    /// <returns>Task that resolves when the runtime is started</returns>
    public async Task StartAsync()
    {
        // Initialize storage
        await Storage.InitAsync(this);

        // Load settings from storage
        await Settings.LoadAsync(Storage);

        // Generate instance ID if not present
        if (Settings.Available())
        {
            var instanceId = Settings.Get<string>("instanceId");
            if (string.IsNullOrEmpty(instanceId))
            {
                instanceId = NodeRed.Util.Util.GenerateId();
                await Settings.SetAsync("instanceId", instanceId);
            }
        }

        // Setup metrics interval if enabled
        if (Log.IsMetricEnabled())
        {
            var interval = Settings.Get<int>("runtimeMetricInterval", 15000);
            _runtimeMetricInterval = new Timer(
                _ => ReportMetrics(),
                null,
                TimeSpan.FromMilliseconds(interval),
                TimeSpan.FromMilliseconds(interval));
        }

        // Log startup info
        Log.LogInfo("\n\nWelcome to Node-RED.NET\n===================\n");
        Log.LogInfo($"Node-RED.NET version: v{Version}");
        Log.LogInfo($".NET Runtime: {Environment.Version}");
        Log.LogInfo($"{Environment.OSVersion}");

        _started = true;

        // Emit runtime started event
        Events.Emit("runtime-state", new NodeRedEventArgs(new { state = "started" }));
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 287-305):
    // ------------------------------------------------------------
    // function reportMetrics() {
    //     var memUsage = process.memoryUsage();
    //     log.log({ level: log.METRIC, event: "runtime.memory.rss", value: memUsage.rss });
    //     ...
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private void ReportMetrics()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();

        Log.LogMessage(new LogMessage
        {
            Level = Log.METRIC,
            Msg = new { @event = "runtime.memory.workingSet", value = process.WorkingSet64 }
        });

        Log.LogMessage(new LogMessage
        {
            Level = Log.METRIC,
            Msg = new { @event = "runtime.memory.privateMemory", value = process.PrivateMemorySize64 }
        });

        Log.LogMessage(new LogMessage
        {
            Level = Log.METRIC,
            Msg = new { @event = "runtime.memory.gcTotalMemory", value = GC.GetTotalMemory(false) }
        });
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 316-328):
    // ------------------------------------------------------------
    // function stop() {
    //     if (runtimeMetricInterval) {
    //         clearInterval(runtimeMetricInterval);
    //         runtimeMetricInterval = null;
    //     }
    //     ...
    //     started = false;
    //     ...
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Stop the runtime.
    /// </summary>
    /// <returns>Task that resolves when the runtime is stopped</returns>
    public async Task StopAsync()
    {
        if (_runtimeMetricInterval is not null)
        {
            await _runtimeMetricInterval.DisposeAsync();
            _runtimeMetricInterval = null;
        }

        _started = false;

        // Emit runtime stopped event
        Events.Emit("runtime-state", new NodeRedEventArgs(new { state = "stopped" }));
    }
    // ============================================================
}
