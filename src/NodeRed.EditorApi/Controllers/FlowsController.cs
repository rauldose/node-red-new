// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-api/lib/admin/flows.js
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
/// API controller for flows.
/// Translated from: @node-red/editor-api/lib/admin/flows.js
/// </summary>
[ApiController]
[Route("flows")]
public class FlowsController : ControllerBase
{
    private readonly IRuntime _runtime;

    public FlowsController(IRuntime runtime)
    {
        _runtime = runtime;
    }

    // ============================================================
    // ORIGINAL CODE (lines 24-41):
    // ------------------------------------------------------------
    // get: function(req,res) {
    //     var version = req.get("Node-RED-API-Version")||"v1";
    //     if (!/^v[12]$/.test(version)) {
    //         return res.status(400).json({code:"invalid_api_version", message:"Invalid API Version requested"});
    //     }
    //     var opts = {
    //         user: req.user,
    //         req: apiUtils.getRequestLogObject(req)
    //     }
    //     runtimeAPI.flows.getFlows(opts).then(function(result) {
    //         if (version === "v1") {
    //             res.json(result.flows);
    //         } else if (version === "v2") {
    //             res.json(result);
    //         }
    //     }).catch(function(err) {
    //         apiUtils.rejectHandler(req,res,err);
    //     })
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get all flows.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFlows()
    {
        var version = Request.Headers["Node-RED-API-Version"].FirstOrDefault() ?? "v1";
        if (version != "v1" && version != "v2")
        {
            return BadRequest(new { code = "invalid_api_version", message = "Invalid API Version requested" });
        }

        try
        {
            var result = await _runtime.Storage.GetFlowsConfigAsync();

            if (version == "v1")
            {
                return Ok(result.Flows);
            }
            else
            {
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "unexpected_error", message = ex.Message });
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 43-71):
    // ------------------------------------------------------------
    // post: function(req,res) {
    //     var version = req.get("Node-RED-API-Version")||"v1";
    //     if (!/^v[12]$/.test(version)) {
    //         return res.status(400).json({code:"invalid_api_version", message:"Invalid API Version requested"});
    //     }
    //     var opts = {
    //         user: req.user,
    //         deploymentType: req.get("Node-RED-Deployment-Type")||"full",
    //         req: apiUtils.getRequestLogObject(req)
    //     }
    //     if (opts.deploymentType !== 'reload') {
    //         if (version === "v1") {
    //             opts.flows = {flows: req.body}
    //         } else {
    //             opts.flows = req.body;
    //         }
    //     }
    //     runtimeAPI.flows.setFlows(opts).then(function(result) {
    //         if (version === "v1") {
    //             res.status(204).end();
    //         } else {
    //             res.json(result);
    //         }
    //     }).catch(function(err) {
    //         apiUtils.rejectHandler(req,res,err);
    //     })
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Deploy flows.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PostFlows([FromBody] FlowsRequest? request)
    {
        var version = Request.Headers["Node-RED-API-Version"].FirstOrDefault() ?? "v1";
        if (version != "v1" && version != "v2")
        {
            return BadRequest(new { code = "invalid_api_version", message = "Invalid API Version requested" });
        }

        var deploymentType = Request.Headers["Node-RED-Deployment-Type"].FirstOrDefault() ?? "full";

        try
        {
            if (deploymentType != "reload" && request is not null)
            {
                FlowsConfig config;
                if (version == "v1")
                {
                    config = new FlowsConfig
                    {
                        Flows = request.Flows ?? new List<Dictionary<string, object?>>(),
                        Credentials = request.Credentials ?? new Dictionary<string, object?>()
                    };
                }
                else
                {
                    config = new FlowsConfig
                    {
                        Flows = request.Flows ?? new List<Dictionary<string, object?>>(),
                        Credentials = request.Credentials ?? new Dictionary<string, object?>(),
                        CredentialsDirty = request.CredentialsDirty
                    };
                }

                var rev = await _runtime.Storage.SaveFlowsConfigAsync(config);

                if (version == "v1")
                {
                    return NoContent();
                }
                else
                {
                    return Ok(new { rev });
                }
            }

            if (version == "v1")
            {
                return NoContent();
            }
            else
            {
                return Ok(new { rev = "" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "unexpected_error", message = ex.Message });
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 72-82):
    // ------------------------------------------------------------
    // getState: function(req,res) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get flows state.
    /// </summary>
    [HttpGet("state")]
    public IActionResult GetState()
    {
        return Ok(new
        {
            state = _runtime.IsStarted ? "started" : "stopped"
        });
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 83-94):
    // ------------------------------------------------------------
    // postState: function(req,res) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Set flows state.
    /// </summary>
    [HttpPost("state")]
    public async Task<IActionResult> PostState([FromBody] StateRequest request)
    {
        var state = request.State ?? "";

        try
        {
            if (state == "start" && !_runtime.IsStarted)
            {
                await ((Runtime.Runtime)_runtime).StartAsync();
            }
            else if (state == "stop" && _runtime.IsStarted)
            {
                await ((Runtime.Runtime)_runtime).StopAsync();
            }

            return Ok(new
            {
                state = _runtime.IsStarted ? "started" : "stopped"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "unexpected_error", message = ex.Message });
        }
    }
    // ============================================================
}

/// <summary>
/// Flows request model.
/// </summary>
public class FlowsRequest
{
    public List<Dictionary<string, object?>>? Flows { get; set; }
    public Dictionary<string, object?>? Credentials { get; set; }
    public bool CredentialsDirty { get; set; }
    public string? Rev { get; set; }
}

/// <summary>
/// State request model.
/// </summary>
public class StateRequest
{
    public string? State { get; set; }
}
