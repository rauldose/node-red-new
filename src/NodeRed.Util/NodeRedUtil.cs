// ============================================================
// SOURCE: packages/node_modules/@node-red/util/index.js
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
// const log = require("./lib/log");
// const i18n = require("./lib/i18n");
// const util = require("./lib/util");
// const events = require("./lib/events");
// const exec = require("./lib/exec");
// const hooks = require("./lib/hooks");
// 
// module.exports = {
//     init: function(settings) {
//         log.init(settings);
//         i18n.init(settings);
//     },
//     log: log,
//     i18n: i18n,
//     util: util,
//     events: events,
//     exec: exec,
//     hooks: hooks
// }
// ------------------------------------------------------------
// TRANSLATION:
// ------------------------------------------------------------

namespace NodeRed.Util;

/// <summary>
/// Main entry point for the Node-RED utility module.
/// Provides common utilities for the Node-RED runtime and editor.
/// Translated from: @node-red/util/index.js
/// </summary>
public class NodeRedUtil
{
    /// <summary>
    /// The events emitter instance.
    /// </summary>
    public Events Events { get; }

    /// <summary>
    /// The i18n instance.
    /// </summary>
    public I18n I18n { get; }

    /// <summary>
    /// The hooks instance.
    /// </summary>
    public Hooks Hooks { get; }

    /// <summary>
    /// The exec instance.
    /// </summary>
    public Exec Exec { get; }

    /// <summary>
    /// Creates a new NodeRedUtil instance.
    /// </summary>
    public NodeRedUtil()
    {
        Events = new Events();
        I18n = new I18n();
        Hooks = new Hooks();
        Exec = new Exec(Events);
    }

    /// <summary>
    /// Initialise the module with the runtime settings.
    /// </summary>
    /// <param name="settings">The settings</param>
    public void Init(NodeRedUtilSettings? settings)
    {
        Log.Init(settings?.Log);
        I18n.Init(settings?.I18n);
    }
}

/// <summary>
/// Combined settings for NodeRedUtil initialization.
/// </summary>
public class NodeRedUtilSettings
{
    /// <summary>
    /// Log settings.
    /// </summary>
    public LogSettings? Log { get; set; }

    /// <summary>
    /// I18n settings.
    /// </summary>
    public I18nSettings? I18n { get; set; }
}
