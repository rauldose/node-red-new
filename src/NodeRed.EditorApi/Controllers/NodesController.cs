// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-api/lib/admin/nodes.js
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
/// Interface for node registry access.
/// </summary>
public interface INodeRegistryService
{
    List<object> GetNodeList();
    string GetNodeConfigs(string lang);
    object? GetModuleInfo(string module);
    object? GetNodeInfo(string id);
}

/// <summary>
/// API controller for nodes.
/// Translated from: @node-red/editor-api/lib/admin/nodes.js
/// </summary>
[ApiController]
[Route("nodes")]
public class NodesController : ControllerBase
{
    private readonly IRuntime _runtime;
    private readonly INodeRegistryService? _registryService;

    public NodesController(IRuntime runtime, INodeRegistryService? registryService = null)
    {
        _runtime = runtime;
        _registryService = registryService;
    }

    // ============================================================
    // ORIGINAL CODE (lines 25-43):
    // ------------------------------------------------------------
    // getAll: function(req,res) {
    //     var opts = { user: req.user, req: apiUtils.getRequestLogObject(req) }
    //     if (req.get("accept") == "application/json") {
    //         runtimeAPI.nodes.getNodeList(opts).then(function(list) {
    //             res.json(list);
    //         })
    //     } else {
    //         opts.lang = apiUtils.determineLangFromHeaders(req.acceptsLanguages());
    //         ...
    //         runtimeAPI.nodes.getNodeConfigs(opts).then(function(configs) {
    //             res.send(configs);
    //         })
    //     }
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get all nodes.
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var accept = Request.Headers.Accept.FirstOrDefault() ?? "";

        if (_registryService is null)
        {
            return Ok(new List<object>());
        }

        if (accept.Contains("application/json"))
        {
            var list = _registryService.GetNodeList();
            return Ok(list);
        }
        else
        {
            var lang = DetermineLang();
            var configs = _registryService.GetNodeConfigs(lang);
            return Content(configs, "text/html");
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 71-82):
    // ------------------------------------------------------------
    // getModule: function(req,res) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get module info.
    /// </summary>
    [HttpGet("{module}")]
    public IActionResult GetModule(string module)
    {
        if (_registryService is null)
        {
            return NotFound(new { code = "not_found", message = $"Module not found: {module}" });
        }

        var info = _registryService.GetModuleInfo(module);
        if (info is null)
        {
            return NotFound(new { code = "not_found", message = $"Module not found: {module}" });
        }

        return Ok(info);
    }
    // ============================================================

    /// <summary>
    /// Get node set info.
    /// </summary>
    [HttpGet("{module}/{name}")]
    public IActionResult GetSet(string module, string name)
    {
        if (_registryService is null)
        {
            return NotFound(new { code = "not_found", message = $"Node set not found: {module}/{name}" });
        }

        var accept = Request.Headers.Accept.FirstOrDefault() ?? "";

        if (accept.Contains("application/json"))
        {
            var info = _registryService.GetNodeInfo($"{module}/{name}");
            if (info is null)
            {
                return NotFound(new { code = "not_found", message = $"Node set not found: {module}/{name}" });
            }
            return Ok(info);
        }
        else
        {
            var lang = DetermineLang();
            var configs = _registryService.GetNodeConfigs(lang);
            return Content(configs, "text/html");
        }
    }

    /// <summary>
    /// Get message catalogs for all modules.
    /// </summary>
    [HttpGet("messages")]
    public IActionResult GetModuleCatalogs()
    {
        // Return empty catalog for now - will be populated when i18n is fully implemented
        return Ok(new Dictionary<string, object>());
    }

    /// <summary>
    /// Get message catalog for a specific module.
    /// </summary>
    [HttpGet("{module}/{name}/messages")]
    public IActionResult GetModuleCatalog(string module, string name)
    {
        // Return empty catalog for now
        return Ok(new Dictionary<string, object>());
    }

    private string DetermineLang()
    {
        var acceptLanguage = Request.Headers.AcceptLanguage.FirstOrDefault();
        if (string.IsNullOrEmpty(acceptLanguage))
        {
            return "en-US";
        }

        // Parse Accept-Language header
        var parts = acceptLanguage.Split(',');
        if (parts.Length > 0)
        {
            var lang = parts[0].Split(';')[0].Trim();
            // Validate language format
            if (System.Text.RegularExpressions.Regex.IsMatch(lang, @"^[0-9a-zA-Z\-]+$"))
            {
                return lang;
            }
        }

        return "en-US";
    }
}
