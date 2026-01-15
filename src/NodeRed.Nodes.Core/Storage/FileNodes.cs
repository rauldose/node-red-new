// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/storage/10-file.js
// ============================================================
// File nodes - read and write files.
// ============================================================

using System.Text;
using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Storage;

/// <summary>
/// File node - writes data to a file.
/// Translated from: @node-red/nodes/core/storage/10-file.js
/// </summary>
public class FileNode : Node
{
    /// <summary>
    /// File path.
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = "";

    /// <summary>
    /// File path type.
    /// </summary>
    [JsonPropertyName("filenameType")]
    public string FilenameType { get; set; } = "str";

    /// <summary>
    /// Append newline after data.
    /// </summary>
    [JsonPropertyName("appendNewline")]
    public bool AppendNewline { get; set; }

    /// <summary>
    /// Overwrite mode: "true" (overwrite), "false" (append), "delete".
    /// </summary>
    [JsonPropertyName("overwriteFile")]
    public string OverwriteFile { get; set; } = "false";

    /// <summary>
    /// Create directory if it doesn't exist.
    /// </summary>
    [JsonPropertyName("createDir")]
    public bool CreateDir { get; set; }

    /// <summary>
    /// Encoding to use.
    /// </summary>
    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "none";

    public FileNode()
    {
        Type = "file";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            var filename = GetFilename(msg);
            
            if (string.IsNullOrEmpty(filename))
            {
                Error("No filename specified", msg);
                return;
            }

            // Create directory if needed
            if (CreateDir)
            {
                var dir = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            // Handle delete mode
            if (OverwriteFile == "delete")
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                await SendAsync(msg);
                return;
            }

            // Get content to write
            byte[] content;
            if (msg.Payload is byte[] bytes)
            {
                content = bytes;
            }
            else
            {
                var str = msg.Payload?.ToString() ?? "";
                if (AppendNewline)
                {
                    str += "\n";
                }
                content = GetEncoding().GetBytes(str);
            }

            // Write to file
            if (OverwriteFile == "true")
            {
                await File.WriteAllBytesAsync(filename, content);
            }
            else
            {
                await File.AppendAllTextAsync(filename, GetEncoding().GetString(content));
            }

            await SendAsync(msg);
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private string GetFilename(FlowMessage msg)
    {
        return FilenameType switch
        {
            "msg" => NodeRed.Util.Util.GetMessageProperty(msg, Filename)?.ToString() ?? "",
            "flow" => "", // Would get from flow context
            "global" => "", // Would get from global context
            "env" => Environment.GetEnvironmentVariable(Filename) ?? "",
            _ => Filename
        };
    }

    private Encoding GetEncoding()
    {
        return Encoding switch
        {
            "utf8" or "utf-8" => System.Text.Encoding.UTF8,
            "utf16" or "utf-16" => System.Text.Encoding.Unicode,
            "latin1" or "binary" => System.Text.Encoding.Latin1,
            "ascii" => System.Text.Encoding.ASCII,
            _ => System.Text.Encoding.UTF8
        };
    }
}

/// <summary>
/// File In node - reads data from a file.
/// Translated from: @node-red/nodes/core/storage/10-file.js
/// </summary>
public class FileInNode : Node
{
    /// <summary>
    /// File path.
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = "";

    /// <summary>
    /// File path type.
    /// </summary>
    [JsonPropertyName("filenameType")]
    public string FilenameType { get; set; } = "str";

    /// <summary>
    /// Output format: "utf8", "lines", "stream", "".
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "utf8";

    /// <summary>
    /// Encoding to use.
    /// </summary>
    [JsonPropertyName("encoding")]
    public string Encoding { get; set; } = "utf8";

    /// <summary>
    /// Whether to emit message per line.
    /// </summary>
    [JsonPropertyName("allProps")]
    public bool AllProps { get; set; }

    public FileInNode()
    {
        Type = "file in";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            var filename = GetFilename(msg);
            
            if (string.IsNullOrEmpty(filename))
            {
                Error("No filename specified", msg);
                return;
            }

            if (!File.Exists(filename))
            {
                Error($"File not found: {filename}", msg);
                return;
            }

            if (Format == "lines")
            {
                // Read and send one message per line
                var lines = await File.ReadAllLinesAsync(filename, GetEncoding());
                foreach (var line in lines)
                {
                    var lineMsg = NodeRed.Util.Util.CloneMessage(msg);
                    lineMsg.Payload = line;
                    await SendAsync(lineMsg);
                }
            }
            else if (Format == "" || Format == "binary")
            {
                // Read as binary
                msg.Payload = await File.ReadAllBytesAsync(filename);
                await SendAsync(msg);
            }
            else
            {
                // Read as text
                msg.Payload = await File.ReadAllTextAsync(filename, GetEncoding());
                await SendAsync(msg);
            }
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private string GetFilename(FlowMessage msg)
    {
        return FilenameType switch
        {
            "msg" => NodeRed.Util.Util.GetMessageProperty(msg, Filename)?.ToString() ?? "",
            "flow" => "", // Would get from flow context
            "global" => "", // Would get from global context
            "env" => Environment.GetEnvironmentVariable(Filename) ?? "",
            _ => Filename
        };
    }

    private Encoding GetEncoding()
    {
        return Encoding switch
        {
            "utf8" or "utf-8" => System.Text.Encoding.UTF8,
            "utf16" or "utf-16" => System.Text.Encoding.Unicode,
            "latin1" or "binary" => System.Text.Encoding.Latin1,
            "ascii" => System.Text.Encoding.ASCII,
            _ => System.Text.Encoding.UTF8
        };
    }
}

/// <summary>
/// Watch node - watches for file changes.
/// Translated from: @node-red/nodes/core/storage/23-watch.js
/// </summary>
public class WatchNode : Node
{
    private FileSystemWatcher? _watcher;

    /// <summary>
    /// File or directory to watch.
    /// </summary>
    [JsonPropertyName("files")]
    public string Files { get; set; } = "";

    /// <summary>
    /// Whether to watch recursively.
    /// </summary>
    [JsonPropertyName("recursive")]
    public bool Recursive { get; set; }

    public WatchNode()
    {
        Type = "watch";
    }

    public override Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(Files))
        {
            return Task.CompletedTask;
        }

        var path = Path.GetDirectoryName(Files) ?? Files;
        var filter = Path.GetFileName(Files);

        if (string.IsNullOrEmpty(filter))
        {
            filter = "*.*";
        }

        _watcher = new FileSystemWatcher(path, filter)
        {
            IncludeSubdirectories = Recursive,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | 
                           NotifyFilters.DirectoryName | NotifyFilters.Size
        };

        _watcher.Changed += OnFileEvent;
        _watcher.Created += OnFileEvent;
        _watcher.Deleted += OnFileEvent;
        _watcher.Renamed += OnRenamedEvent;

        _watcher.EnableRaisingEvents = true;

        return Task.CompletedTask;
    }

    public override Task CloseAsync(bool removed)
    {
        _watcher?.Dispose();
        _watcher = null;
        return base.CloseAsync(removed);
    }

    private async void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        var msg = new FlowMessage
        {
            MsgId = NodeRed.Util.Util.GenerateId(),
            Payload = e.FullPath,
            Topic = Files
        };

        msg.AdditionalProperties["filename"] = e.FullPath;
        msg.AdditionalProperties["type"] = e.ChangeType.ToString().ToLower();

        await SendAsync(msg);
    }

    private async void OnRenamedEvent(object sender, RenamedEventArgs e)
    {
        var msg = new FlowMessage
        {
            MsgId = NodeRed.Util.Util.GenerateId(),
            Payload = e.FullPath,
            Topic = Files
        };

        msg.AdditionalProperties["filename"] = e.FullPath;
        msg.AdditionalProperties["oldFilename"] = e.OldFullPath;
        msg.AdditionalProperties["type"] = "renamed";

        await SendAsync(msg);
    }
}
