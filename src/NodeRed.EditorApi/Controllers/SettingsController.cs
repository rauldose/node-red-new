// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-api/lib/admin/settings.js
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

using Microsoft.AspNetCore.Mvc;
using NodeRed.Runtime;

namespace NodeRed.EditorApi.Controllers;

/// <summary>
/// API controller for settings.
/// Translated from: @node-red/editor-api/lib/admin/settings.js
/// </summary>
[ApiController]
[Route("settings")]
public class SettingsController : ControllerBase
{
    private readonly IRuntime _runtime;

    public SettingsController(IRuntime runtime)
    {
        _runtime = runtime;
    }

    /// <summary>
    /// Get runtime settings.
    /// </summary>
    [HttpGet]
    public IActionResult GetSettings()
    {
        // Build safe settings to return to the editor
        var safeSettings = new Dictionary<string, object?>
        {
            ["httpNodeRoot"] = _runtime.Settings.Get<string>("httpNodeRoot") ?? "/",
            ["version"] = _runtime.Version,
            ["context"] = new Dictionary<string, object?>
            {
                ["default"] = "memory",
                ["stores"] = new[] { "memory" }
            },
            ["flowEncryptionType"] = "system",
            ["diagnostics"] = new Dictionary<string, object?>
            {
                ["enabled"] = true,
                ["ui"] = true
            }
        };

        // Add editor theme if configured
        var editorTheme = _runtime.Settings.Get<Dictionary<string, object?>>("editorTheme");
        if (editorTheme is not null)
        {
            safeSettings["editorTheme"] = editorTheme;
        }

        // Export node settings
        safeSettings = _runtime.Settings.ExportNodeSettings(safeSettings);

        return Ok(safeSettings);
    }
}
