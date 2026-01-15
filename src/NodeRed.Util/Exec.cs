// ============================================================
// SOURCE: packages/node_modules/@node-red/util/lib/exec.js
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

using System.Diagnostics;

namespace NodeRed.Util;

/// <summary>
/// Run system commands with event-log integration.
/// Translated from: @node-red/util/lib/exec.js
/// </summary>
public class Exec
{
    private readonly Events _events;

    /// <summary>
    /// Creates a new Exec instance with the specified events emitter.
    /// </summary>
    /// <param name="events">The events emitter to use</param>
    public Exec(Events events)
    {
        _events = events;
    }

    // ============================================================
    // ORIGINAL CODE (lines 26-28):
    // ------------------------------------------------------------
    // function logLines(id,type,data) {
    //     events.emit("event-log", {id:id,payload:{ts: Date.now(),data:data,type:type}});
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private void LogLines(string id, string type, string data)
    {
        _events.Emit("event-log", new NodeRedEventArgs(new EventLogPayload
        {
            Id = id,
            Payload = new EventLogData
            {
                Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Data = data,
                Type = type
            }
        }));
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 52-98):
    // ------------------------------------------------------------
    // run: function(command,args,options,emit) {
    //     var invocationId = util.generateId();
    //     emit && events.emit("event-log", {...});
    //     return new Promise((resolve, reject) => {
    //         let stdout = "";
    //         let stderr = "";
    //         let child = child_process.spawn(command, args, options);
    //         ...
    //     });
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Run a system command with stdout/err being emitted as 'event-log' events.
    /// </summary>
    /// <param name="command">The command to run</param>
    /// <param name="args">Arguments for the command</param>
    /// <param name="options">Options for the process</param>
    /// <param name="emit">Whether to emit events to the event-log</param>
    /// <returns>A task that resolves with the command result</returns>
    public async Task<ExecResult> RunAsync(
        string command, 
        string[]? args = null, 
        ExecOptions? options = null, 
        bool emit = false)
    {
        var invocationId = Util.GenerateId();
        args ??= Array.Empty<string>();
        options ??= new ExecOptions();

        if (emit)
        {
            var commandLine = args.Length > 0 ? $"{command} {string.Join(" ", args)}" : command;
            _events.Emit("event-log", new NodeRedEventArgs(new EventLogPayload
            {
                Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Id = invocationId,
                Payload = new EventLogData
                {
                    Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Data = commandLine
                }
            }));
        }

        var stdout = new System.Text.StringBuilder();
        var stderr = new System.Text.StringBuilder();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command,
                UseShellExecute = options.Shell,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // If using shell, combine command and args
            if (options.Shell && args.Length > 0)
            {
                startInfo.FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";
                startInfo.Arguments = OperatingSystem.IsWindows() 
                    ? $"/c {command} {string.Join(" ", args)}"
                    : $"-c \"{command} {string.Join(" ", args)}\"";
            }
            else
            {
                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }

            if (!string.IsNullOrEmpty(options.WorkingDirectory))
            {
                startInfo.WorkingDirectory = options.WorkingDirectory;
            }

            if (options.Environment is not null)
            {
                foreach (var kvp in options.Environment)
                {
                    startInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data is not null)
                {
                    stdout.AppendLine(e.Data);
                    if (emit)
                    {
                        LogLines(invocationId, "out", e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data is not null)
                {
                    stderr.AppendLine(e.Data);
                    if (emit)
                    {
                        LogLines(invocationId, "err", e.Data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var result = new ExecResult
            {
                Code = process.ExitCode,
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString()
            };

            if (emit)
            {
                _events.Emit("event-log", new NodeRedEventArgs(new EventLogPayload
                {
                    Id = invocationId,
                    Payload = new EventLogData
                    {
                        Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Data = $"rc={result.Code}",
                        End = true
                    }
                }));
            }

            if (result.Code != 0)
            {
                throw new ExecException(result);
            }

            return result;
        }
        catch (ExecException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = ex.ToString();
            if (emit)
            {
                LogLines(invocationId, "err", errorMessage);
            }

            var result = new ExecResult
            {
                Code = -1,
                Stdout = stdout.ToString(),
                Stderr = errorMessage
            };

            throw new ExecException(result);
        }
    }
    // ============================================================
}

/// <summary>
/// Result of an exec operation.
/// </summary>
public class ExecResult
{
    /// <summary>
    /// The exit code.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Standard output from the command.
    /// </summary>
    public string Stdout { get; set; } = string.Empty;

    /// <summary>
    /// Standard error from the command.
    /// </summary>
    public string Stderr { get; set; } = string.Empty;
}

/// <summary>
/// Options for exec operations.
/// </summary>
public class ExecOptions
{
    /// <summary>
    /// Whether to use shell execution.
    /// </summary>
    public bool Shell { get; set; }

    /// <summary>
    /// The working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Environment variables.
    /// </summary>
    public Dictionary<string, string>? Environment { get; set; }
}

/// <summary>
/// Exception thrown when an exec operation fails.
/// </summary>
public class ExecException : Exception
{
    /// <summary>
    /// The exec result.
    /// </summary>
    public ExecResult Result { get; }

    /// <summary>
    /// Creates a new ExecException with the specified result.
    /// </summary>
    /// <param name="result">The exec result</param>
    public ExecException(ExecResult result) : base($"Command failed with exit code {result.Code}")
    {
        Result = result;
    }
}

/// <summary>
/// Event log payload structure.
/// </summary>
public class EventLogPayload
{
    /// <summary>
    /// Timestamp.
    /// </summary>
    public long Ts { get; set; }

    /// <summary>
    /// Invocation ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Payload data.
    /// </summary>
    public EventLogData? Payload { get; set; }
}

/// <summary>
/// Event log data structure.
/// </summary>
public class EventLogData
{
    /// <summary>
    /// Timestamp.
    /// </summary>
    public long Ts { get; set; }

    /// <summary>
    /// Data content.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Type of log entry (out/err).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the end marker.
    /// </summary>
    public bool End { get; set; }
}
