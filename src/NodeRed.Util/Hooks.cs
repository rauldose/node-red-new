// ============================================================
// SOURCE: packages/node_modules/@node-red/util/lib/hooks.js
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

namespace NodeRed.Util;

/// <summary>
/// Runtime hooks engine for Node-RED.
/// Translated from: @node-red/util/lib/hooks.js
/// </summary>
public class Hooks
{
    // ============================================================
    // ORIGINAL CODE (lines 3-17):
    // ------------------------------------------------------------
    // const VALID_HOOKS = [
    //     // Message Routing Path
    //    "onSend",
    //    "preRoute",
    //    "preDeliver",
    //    "postDeliver",
    //    "onReceive",
    //    "postReceive",
    //    "onComplete",
    //    // Module install hooks
    //    "preInstall",
    //    "postInstall",
    //    "preUninstall",
    //    "postUninstall"
    // ]
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Valid hook names.
    /// </summary>
    public static readonly string[] ValidHooks = new[]
    {
        // Message Routing Path
        "onSend",
        "preRoute",
        "preDeliver",
        "postDeliver",
        "onReceive",
        "postReceive",
        "onComplete",
        // Module install hooks
        "preInstall",
        "postInstall",
        "preUninstall",
        "postUninstall"
    };
    // ============================================================

    // Flags for what hooks have handlers registered
    private readonly Dictionary<string, bool> _states = new();

    // Doubly-LinkedList of hooks by id
    private readonly Dictionary<string, HookItem?> _hooks = new();

    // Hooks by label
    private readonly Dictionary<string, Dictionary<string, HookItem>> _labelledHooks = new();

    private readonly object _lock = new();

    // ============================================================
    // ORIGINAL CODE (lines 61-111):
    // ------------------------------------------------------------
    // function add(hookId, callback) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Register a handler to a named hook.
    /// </summary>
    /// <param name="hookId">The name of the hook to attach to (format: "hookName" or "hookName.label")</param>
    /// <param name="callback">The callback function for the hook</param>
    /// <exception cref="ArgumentException">If the hook name is invalid or already registered</exception>
    public void Add(string hookId, Func<object?, Task<object?>> callback)
    {
        var parts = hookId.Split('.', 2);
        var id = parts[0];
        var label = parts.Length > 1 ? parts[1] : null;

        if (!ValidHooks.Contains(id))
        {
            throw new ArgumentException($"Invalid hook '{id}'");
        }

        lock (_lock)
        {
            if (label is not null && 
                _labelledHooks.TryGetValue(label, out var labelHooks) && 
                labelHooks.ContainsKey(id))
            {
                throw new InvalidOperationException($"Hook {hookId} already registered");
            }

            // Get location of calling code
            var stack = Environment.StackTrace;
            var stackLines = stack.Split('\n').Skip(1).ToArray();
            var callModule = stackLines.Length > 1 ? stackLines[1].Trim() : "unknown:0:0";

            Log.LogDebug($"Adding hook '{hookId}' from {callModule}");

            var hookItem = new HookItem
            {
                Callback = callback,
                Location = callModule,
                PreviousHook = null,
                NextHook = null
            };

            if (!_hooks.TryGetValue(id, out var tailItem) || tailItem is null)
            {
                _hooks[id] = hookItem;
            }
            else
            {
                while (tailItem.NextHook is not null)
                {
                    tailItem = tailItem.NextHook;
                }
                tailItem.NextHook = hookItem;
                hookItem.PreviousHook = tailItem;
            }

            if (label is not null)
            {
                if (!_labelledHooks.TryGetValue(label, out var hooks))
                {
                    hooks = new Dictionary<string, HookItem>();
                    _labelledHooks[label] = hooks;
                }
                hooks[id] = hookItem;
            }

            _states[id] = true;
        }
    }

    /// <summary>
    /// Register a synchronous handler to a named hook.
    /// </summary>
    /// <param name="hookId">The name of the hook to attach to</param>
    /// <param name="callback">The synchronous callback function</param>
    public void Add(string hookId, Func<object?, object?> callback)
    {
        Add(hookId, payload => Task.FromResult(callback(payload)));
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 118-140):
    // ------------------------------------------------------------
    // function remove(hookId) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Remove a handler from a named hook.
    /// </summary>
    /// <param name="hookId">The name of the hook event to remove - must be "name.label" or "*.label"</param>
    /// <exception cref="ArgumentException">If no label is provided</exception>
    public void Remove(string hookId)
    {
        var parts = hookId.Split('.', 2);
        var id = parts[0];
        var label = parts.Length > 1 ? parts[1] : null;

        if (label is null)
        {
            throw new ArgumentException($"Cannot remove hook without label: {hookId}");
        }

        Log.LogDebug($"Removing hook '{hookId}'");

        lock (_lock)
        {
            if (_labelledHooks.TryGetValue(label, out var labelHooks))
            {
                if (id == "*")
                {
                    // Remove all hooks for this label
                    foreach (var hookKvp in labelHooks)
                    {
                        RemoveHook(hookKvp.Key, hookKvp.Value);
                    }
                    _labelledHooks.Remove(label);
                }
                else if (labelHooks.TryGetValue(id, out var hookItem))
                {
                    RemoveHook(id, hookItem);
                    labelHooks.Remove(id);
                    if (labelHooks.Count == 0)
                    {
                        _labelledHooks.Remove(label);
                    }
                }
            }
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 142-159):
    // ------------------------------------------------------------
    // function removeHook(id,hookItem) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private void RemoveHook(string id, HookItem hookItem)
    {
        var previousHook = hookItem.PreviousHook;
        var nextHook = hookItem.NextHook;

        if (previousHook is not null)
        {
            previousHook.NextHook = nextHook;
        }
        else
        {
            _hooks[id] = nextHook;
        }

        if (nextHook is not null)
        {
            nextHook.PreviousHook = previousHook;
        }

        hookItem.Removed = true;

        if (previousHook is null && nextHook is null)
        {
            _hooks.Remove(id);
            _states.Remove(id);
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 162-189):
    // ------------------------------------------------------------
    // function trigger(hookId, payload, done) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Trigger a hook with the given payload.
    /// </summary>
    /// <param name="hookId">The hook to trigger</param>
    /// <param name="payload">The payload to pass to handlers</param>
    /// <returns>A task that resolves when all handlers complete, or false if halted</returns>
    public async Task<object?> TriggerAsync(string hookId, object? payload)
    {
        HookItem? hookItem;
        lock (_lock)
        {
            _hooks.TryGetValue(hookId, out hookItem);
        }

        if (hookItem is null)
        {
            return null;
        }

        return await InvokeStackAsync(hookItem, payload);
    }

    /// <summary>
    /// Trigger a hook with the given payload and callback.
    /// </summary>
    /// <param name="hookId">The hook to trigger</param>
    /// <param name="payload">The payload to pass to handlers</param>
    /// <param name="done">Callback when complete</param>
    public void Trigger(string hookId, object? payload, Action<object?> done)
    {
        Task.Run(async () =>
        {
            try
            {
                var result = await TriggerAsync(hookId, payload);
                done(result);
            }
            catch (Exception ex)
            {
                done(ex);
            }
        });
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 190-238):
    // ------------------------------------------------------------
    // function invokeStack(hookItem,payload,done) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private async Task<object?> InvokeStackAsync(HookItem? hookItem, object? payload)
    {
        while (hookItem is not null)
        {
            if (hookItem.Removed)
            {
                hookItem = hookItem.NextHook;
                continue;
            }

            try
            {
                var result = await hookItem.Callback(payload);

                if (result is bool b && !b)
                {
                    // Halting the flow
                    return false;
                }

                if (result is not null)
                {
                    return result;
                }

                hookItem = hookItem.NextHook;
            }
            catch (Exception ex)
            {
                var error = new HookException(ex.Message, ex);
                throw error;
            }
        }

        return null;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 240-244):
    // ------------------------------------------------------------
    // function clear() {
    //     hooks = {}
    //     labelledHooks = {}
    //     states = {}
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Clear all registered hooks.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _hooks.Clear();
            _labelledHooks.Clear();
            _states.Clear();
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 246-252):
    // ------------------------------------------------------------
    // function has(hookId) {
    //     let [id, label] = hookId.split(".");
    //     if (label) {
    //         return !!(labelledHooks[label] && labelledHooks[label][id])
    //     }
    //     return !!states[id]
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Check if a hook has any handlers registered.
    /// </summary>
    /// <param name="hookId">The hook ID to check</param>
    /// <returns>True if the hook has handlers</returns>
    public bool Has(string hookId)
    {
        var parts = hookId.Split('.', 2);
        var id = parts[0];
        var label = parts.Length > 1 ? parts[1] : null;

        lock (_lock)
        {
            if (label is not null)
            {
                return _labelledHooks.TryGetValue(label, out var hooks) && hooks.ContainsKey(id);
            }
            return _states.ContainsKey(id) && _states[id];
        }
    }
    // ============================================================
}

/// <summary>
/// Represents a hook item in the doubly-linked list.
/// </summary>
internal class HookItem
{
    /// <summary>
    /// The callback function.
    /// </summary>
    public required Func<object?, Task<object?>> Callback { get; init; }

    /// <summary>
    /// The location where the hook was registered.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Reference to the previous hook in the list.
    /// </summary>
    public HookItem? PreviousHook { get; set; }

    /// <summary>
    /// Reference to the next hook in the list.
    /// </summary>
    public HookItem? NextHook { get; set; }

    /// <summary>
    /// Flag indicating if this hook has been removed.
    /// </summary>
    public bool Removed { get; set; }
}

/// <summary>
/// Exception thrown when a hook handler fails.
/// </summary>
public class HookException : Exception
{
    /// <summary>
    /// The hook that caused the exception.
    /// </summary>
    public string? Hook { get; set; }

    /// <summary>
    /// Creates a new HookException.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public HookException(string message, Exception? innerException = null) 
        : base(message, innerException)
    {
    }
}
