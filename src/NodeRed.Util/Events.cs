// ============================================================
// SOURCE: packages/node_modules/@node-red/util/lib/events.js
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
// ORIGINAL CODE:
// ------------------------------------------------------------
// const events = new (require("events")).EventEmitter();
// 
// const deprecatedEvents = {
//     "nodes-stopped": "flows:stopped",
//     "nodes-started": "flows:started"
// }
// 
// function wrapEventFunction(obj,func) { ... }
// 
// events.on = wrapEventFunction(events,"on");
// events.once = wrapEventFunction(events,"once");
// events.addListener = events.on;
// ------------------------------------------------------------
// TRANSLATION:
// ------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace NodeRed.Util;

/// <summary>
/// Runtime events emitter.
/// Translated from: @node-red/util/lib/events.js
/// </summary>
public class Events
{
    private readonly Dictionary<string, List<EventHandler<EventArgs>>> _handlers = new();
    private readonly Dictionary<string, List<EventHandler<EventArgs>>> _onceHandlers = new();
    private readonly object _lock = new();
    private ILogger? _logger;

    /// <summary>
    /// Deprecated event mappings (old name -> new name).
    /// </summary>
    private static readonly Dictionary<string, string> DeprecatedEvents = new()
    {
        { "nodes-stopped", "flows:stopped" },
        { "nodes-started", "flows:started" }
    };

    /// <summary>
    /// Sets the logger for deprecation warnings.
    /// </summary>
    /// <param name="logger">The logger to use</param>
    public void SetLogger(ILogger? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register an event listener for a runtime event.
    /// </summary>
    /// <param name="eventName">The name of the event to listen to</param>
    /// <param name="listener">The callback function for the event</param>
    public void On(string eventName, EventHandler<EventArgs> listener)
    {
        WarnIfDeprecated(eventName);
        lock (_lock)
        {
            if (!_handlers.ContainsKey(eventName))
            {
                _handlers[eventName] = new List<EventHandler<EventArgs>>();
            }
            _handlers[eventName].Add(listener);
        }
    }

    /// <summary>
    /// Register an event listener that will only be called once.
    /// </summary>
    /// <param name="eventName">The name of the event to listen to</param>
    /// <param name="listener">The callback function for the event</param>
    public void Once(string eventName, EventHandler<EventArgs> listener)
    {
        WarnIfDeprecated(eventName);
        lock (_lock)
        {
            if (!_onceHandlers.ContainsKey(eventName))
            {
                _onceHandlers[eventName] = new List<EventHandler<EventArgs>>();
            }
            _onceHandlers[eventName].Add(listener);
        }
    }

    /// <summary>
    /// Register an event listener for a runtime event.
    /// Alias for <see cref="On"/>.
    /// </summary>
    /// <param name="eventName">The name of the event to listen to</param>
    /// <param name="listener">The callback function for the event</param>
    public void AddListener(string eventName, EventHandler<EventArgs> listener)
    {
        On(eventName, listener);
    }

    /// <summary>
    /// Remove an event listener.
    /// </summary>
    /// <param name="eventName">The name of the event</param>
    /// <param name="listener">The listener to remove</param>
    public void RemoveListener(string eventName, EventHandler<EventArgs> listener)
    {
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventName, out var handlers))
            {
                handlers.Remove(listener);
            }
            if (_onceHandlers.TryGetValue(eventName, out var onceHandlers))
            {
                onceHandlers.Remove(listener);
            }
        }
    }

    /// <summary>
    /// Remove all listeners for an event.
    /// </summary>
    /// <param name="eventName">The name of the event</param>
    public void RemoveAllListeners(string eventName)
    {
        lock (_lock)
        {
            _handlers.Remove(eventName);
            _onceHandlers.Remove(eventName);
        }
    }

    /// <summary>
    /// Emit an event to all of its registered listeners.
    /// </summary>
    /// <param name="eventName">The name of the event to emit</param>
    /// <param name="args">The event arguments</param>
    /// <returns>Whether the event had listeners or not</returns>
    public bool Emit(string eventName, EventArgs? args = null)
    {
        args ??= EventArgs.Empty;
        List<EventHandler<EventArgs>> handlers;
        List<EventHandler<EventArgs>> onceHandlers;

        lock (_lock)
        {
            _handlers.TryGetValue(eventName, out var h);
            _onceHandlers.TryGetValue(eventName, out var oh);
            handlers = h?.ToList() ?? new List<EventHandler<EventArgs>>();
            onceHandlers = oh?.ToList() ?? new List<EventHandler<EventArgs>>();
            
            // Clear once handlers after copying
            if (oh is not null)
            {
                _onceHandlers.Remove(eventName);
            }
        }

        bool hadListeners = handlers.Count > 0 || onceHandlers.Count > 0;

        foreach (var handler in handlers)
        {
            handler(this, args);
        }

        foreach (var handler in onceHandlers)
        {
            handler(this, args);
        }

        return hadListeners;
    }

    /// <summary>
    /// Get the number of listeners for an event.
    /// </summary>
    /// <param name="eventName">The event name</param>
    /// <returns>The number of listeners</returns>
    public int ListenerCount(string eventName)
    {
        lock (_lock)
        {
            int count = 0;
            if (_handlers.TryGetValue(eventName, out var handlers))
            {
                count += handlers.Count;
            }
            if (_onceHandlers.TryGetValue(eventName, out var onceHandlers))
            {
                count += onceHandlers.Count;
            }
            return count;
        }
    }

    private void WarnIfDeprecated(string eventName)
    {
        if (DeprecatedEvents.TryGetValue(eventName, out var newEventName))
        {
            var stackTrace = Environment.StackTrace;
            var lines = stackTrace.Split('\n');
            var location = lines.Length > 3 ? lines[3].Trim() : "(unknown)";
            
            _logger?.LogWarning(
                "[RED.events] Deprecated use of \"{OldEvent}\" event from \"{Location}\". Use \"{NewEvent}\" instead.",
                eventName, location, newEventName);
        }
    }
}

/// <summary>
/// Event arguments for Node-RED events.
/// </summary>
public class NodeRedEventArgs : EventArgs
{
    /// <summary>
    /// The event data.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Creates new event arguments with the specified data.
    /// </summary>
    /// <param name="data">The event data</param>
    public NodeRedEventArgs(object? data = null)
    {
        Data = data;
    }
}
