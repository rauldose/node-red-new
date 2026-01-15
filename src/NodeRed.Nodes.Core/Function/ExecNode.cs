// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/function/90-exec.js
// ============================================================
// Exec node - runs system commands.
// ============================================================

using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Function;

/// <summary>
/// Exec node - executes system commands.
/// Translated from: @node-red/nodes/core/function/90-exec.js
/// </summary>
public class ExecNode : Node
{
    private readonly List<Process> _activeProcesses = new();
    private readonly object _lock = new();

    /// <summary>
    /// Command to execute.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    /// <summary>
    /// Whether to append msg.payload to command.
    /// </summary>
    [JsonPropertyName("addpay")]
    public string AddPay { get; set; } = "payload";

    /// <summary>
    /// Whether to append additional arguments.
    /// </summary>
    [JsonPropertyName("append")]
    public string? Append { get; set; }

    /// <summary>
    /// Use spawn mode (streaming) vs exec mode (wait for completion).
    /// </summary>
    [JsonPropertyName("useSpawn")]
    public string UseSpawn { get; set; } = "false";

    /// <summary>
    /// Timer/timeout value.
    /// </summary>
    [JsonPropertyName("timer")]
    public string? Timer { get; set; }

    /// <summary>
    /// Whether to use old return format (string) vs new (buffer).
    /// </summary>
    [JsonPropertyName("oldrc")]
    public bool OldRc { get; set; }

    /// <summary>
    /// Number of outputs (1-3).
    /// </summary>
    [JsonPropertyName("outputs")]
    public new int Outputs { get; set; } = 3;

    public ExecNode()
    {
        Type = "exec";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    public override async Task CloseAsync(bool removed)
    {
        // Kill any running processes
        lock (_lock)
        {
            foreach (var process in _activeProcesses)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch { }
            }
            _activeProcesses.Clear();
        }

        await base.CloseAsync(removed);
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            // Check for kill command
            if (msg.AdditionalProperties.TryGetValue("kill", out var killSig))
            {
                await KillProcessesAsync();
                return;
            }

            // Build command
            var cmd = new StringBuilder(Command);

            // Append payload if configured
            if (!string.IsNullOrEmpty(AddPay))
            {
                var payload = NodeRed.Util.Util.GetMessageProperty(msg, AddPay);
                if (payload is not null)
                {
                    cmd.Append(" ");
                    cmd.Append(payload.ToString());
                }
            }

            // Append additional args
            if (!string.IsNullOrEmpty(Append))
            {
                cmd.Append(" ");
                cmd.Append(Append);
            }

            var commandLine = cmd.ToString().Trim();
            
            if (UseSpawn == "true")
            {
                await ExecuteSpawnAsync(commandLine, msg);
            }
            else
            {
                await ExecuteExecAsync(commandLine, msg);
            }
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private async Task ExecuteExecAsync(string command, FlowMessage msg)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetShell(),
            Arguments = GetShellArgs(command),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        lock (_lock)
        {
            _activeProcesses.Add(process);
        }

        try
        {
            process.Start();

            // Set up timeout if configured
            var timeoutMs = GetTimeoutMs();
            
            using var cts = timeoutMs > 0 
                ? new CancellationTokenSource(timeoutMs) 
                : new CancellationTokenSource();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { }
                Error("Process timed out", msg);
                return;
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCode = process.ExitCode;

            // Send outputs
            var outputs = new FlowMessage?[3];

            // Output 1: stdout
            var stdoutMsg = NodeRed.Util.Util.CloneMessage(msg);
            stdoutMsg.Payload = OldRc ? stdout : Encoding.UTF8.GetBytes(stdout);
            outputs[0] = stdoutMsg;

            // Output 2: stderr  
            if (!string.IsNullOrEmpty(stderr))
            {
                var stderrMsg = NodeRed.Util.Util.CloneMessage(msg);
                stderrMsg.Payload = OldRc ? stderr : Encoding.UTF8.GetBytes(stderr);
                outputs[1] = stderrMsg;
            }

            // Output 3: return code
            var rcMsg = NodeRed.Util.Util.CloneMessage(msg);
            rcMsg.Payload = exitCode;
            outputs[2] = rcMsg;

            await SendAsync(outputs);
        }
        finally
        {
            lock (_lock)
            {
                _activeProcesses.Remove(process);
            }
        }
    }

    private async Task ExecuteSpawnAsync(string command, FlowMessage msg)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GetShell(),
            Arguments = GetShellArgs(command),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        lock (_lock)
        {
            _activeProcesses.Add(process);
        }

        try
        {
            process.OutputDataReceived += async (s, e) =>
            {
                if (e.Data is not null)
                {
                    var outMsg = NodeRed.Util.Util.CloneMessage(msg);
                    outMsg.Payload = OldRc ? e.Data : Encoding.UTF8.GetBytes(e.Data);
                    await SendAsync(new FlowMessage?[] { outMsg, null, null });
                }
            };

            process.ErrorDataReceived += async (s, e) =>
            {
                if (e.Data is not null)
                {
                    var errMsg = NodeRed.Util.Util.CloneMessage(msg);
                    errMsg.Payload = OldRc ? e.Data : Encoding.UTF8.GetBytes(e.Data);
                    await SendAsync(new FlowMessage?[] { null, errMsg, null });
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            // Send return code
            var rcMsg = NodeRed.Util.Util.CloneMessage(msg);
            rcMsg.Payload = process.ExitCode;
            await SendAsync(new FlowMessage?[] { null, null, rcMsg });
        }
        finally
        {
            lock (_lock)
            {
                _activeProcesses.Remove(process);
            }
        }
    }

    private async Task KillProcessesAsync()
    {
        List<Process> toKill;
        lock (_lock)
        {
            toKill = _activeProcesses.ToList();
        }

        foreach (var process in toKill)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch { }
        }

        await Task.CompletedTask;
    }

    private static string GetShell()
    {
        if (OperatingSystem.IsWindows())
        {
            return "cmd.exe";
        }
        return "/bin/sh";
    }

    private static string GetShellArgs(string command)
    {
        if (OperatingSystem.IsWindows())
        {
            return $"/c {command}";
        }
        return $"-c \"{command.Replace("\"", "\\\"")}\"";
    }

    private int GetTimeoutMs()
    {
        if (string.IsNullOrEmpty(Timer) || !int.TryParse(Timer, out var seconds))
        {
            return 0; // No timeout
        }
        return seconds * 1000;
    }
}
