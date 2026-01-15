// ============================================================
// SOURCE: packages/node_modules/@node-red/util/lib/log.js
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

using System.Text.Json;

namespace NodeRed.Util;

/// <summary>
/// Logging utilities for Node-RED.
/// Translated from: @node-red/util/lib/log.js
/// </summary>
public static class Log
{
    // ============================================================
    // ORIGINAL CODE (lines 28-38):
    // ------------------------------------------------------------
    // var levels = {
    //     off:    1,
    //     fatal:  10,
    //     error:  20,
    //     warn:   30,
    //     info:   40,
    //     debug:  50,
    //     trace:  60,
    //     audit:  98,
    //     metric: 99
    // };
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Log level: Off
    /// </summary>
    public const int OFF = 1;

    /// <summary>
    /// Log level: Fatal
    /// </summary>
    public const int FATAL = 10;

    /// <summary>
    /// Log level: Error
    /// </summary>
    public const int ERROR = 20;

    /// <summary>
    /// Log level: Warn
    /// </summary>
    public const int WARN = 30;

    /// <summary>
    /// Log level: Info
    /// </summary>
    public const int INFO = 40;

    /// <summary>
    /// Log level: Debug
    /// </summary>
    public const int DEBUG = 50;

    /// <summary>
    /// Log level: Trace
    /// </summary>
    public const int TRACE = 60;

    /// <summary>
    /// Log level: Audit
    /// </summary>
    public const int AUDIT = 98;

    /// <summary>
    /// Log level: Metric
    /// </summary>
    public const int METRIC = 99;
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 40-49):
    // ------------------------------------------------------------
    // var levelNames = {
    //     10: "fatal",
    //     20: "error",
    //     30: "warn",
    //     40: "info",
    //     50: "debug",
    //     60: "trace",
    //     98: "audit",
    //     99: "metric"
    // };
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private static readonly Dictionary<int, string> LevelNames = new()
    {
        { 10, "fatal" },
        { 20, "error" },
        { 30, "warn" },
        { 40, "info" },
        { 50, "debug" },
        { 60, "trace" },
        { 98, "audit" },
        { 99, "metric" }
    };

    private static readonly Dictionary<string, int> Levels = new()
    {
        { "off", 1 },
        { "fatal", 10 },
        { "error", 20 },
        { "warn", 30 },
        { "info", 40 },
        { "debug", 50 },
        { "trace", 60 },
        { "audit", 98 },
        { "metric", 99 }
    };
    // ============================================================

    private static readonly List<LogHandler> LogHandlers = new();
    private static bool _verbose;
    private static bool _metricsEnabled;
    private static readonly object _lock = new();

    // ============================================================
    // ORIGINAL CODE (lines 143-164):
    // ------------------------------------------------------------
    // init: function(settings) {
    //     metricsEnabled = false;
    //     logHandlers = [];
    //     var loggerSettings = {};
    //     verbose = settings.verbose;
    //     if (settings.logging) { ... }
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Initialize the logging system with settings.
    /// </summary>
    /// <param name="settings">The settings object</param>
    public static void Init(LogSettings? settings)
    {
        lock (_lock)
        {
            _metricsEnabled = false;
            LogHandlers.Clear();
            _verbose = settings?.Verbose ?? false;

            if (settings?.Logging is not null && settings.Logging.Count > 0)
            {
                foreach (var config in settings.Logging.Values)
                {
                    AddHandler(new LogHandler(config));
                }
            }
            else
            {
                AddHandler(new LogHandler(null));
            }
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 170-172):
    // ------------------------------------------------------------
    // addHandler: function(func) {
    //     logHandlers.push(func);
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Add a log handler function.
    /// </summary>
    /// <param name="handler">The handler to add</param>
    public static void AddHandler(LogHandler handler)
    {
        lock (_lock)
        {
            LogHandlers.Add(handler);
            if (handler.MetricsOn)
            {
                _metricsEnabled = true;
            }
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 178-183):
    // ------------------------------------------------------------
    // removeHandler: function(func) {
    //     var index = logHandlers.indexOf(func);
    //     if (index > -1) {
    //         logHandlers.splice(index,1);
    //     }
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Remove a log handler function.
    /// </summary>
    /// <param name="handler">The handler to remove</param>
    public static void RemoveHandler(LogHandler handler)
    {
        lock (_lock)
        {
            LogHandlers.Remove(handler);
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 189-193):
    // ------------------------------------------------------------
    // log: function(msg) {
    //     msg.timestamp = Date.now();
    //     logHandlers.forEach(function(handler) {
    //         handler.emit("log",msg);
    //     });
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Log a message object.
    /// </summary>
    /// <param name="msg">The log message</param>
    public static void LogMessage(LogMessage msg)
    {
        msg.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        List<LogHandler> handlers;
        lock (_lock)
        {
            handlers = LogHandlers.ToList();
        }
        foreach (var handler in handlers)
        {
            handler.HandleLog(msg);
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 200-202):
    // ------------------------------------------------------------
    // info: function(msg) {
    //     log.log({level:log.INFO,msg:msg});
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Log a message at INFO level.
    /// </summary>
    /// <param name="msg">The message</param>
    public static void LogInfo(object msg)
    {
        LogMessage(new LogMessage { Level = INFO, Msg = msg });
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 208-210):
    // ------------------------------------------------------------
    // warn: function(msg) {
    //     log.log({level:log.WARN,msg:msg});
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Log a message at WARN level.
    /// </summary>
    /// <param name="msg">The message</param>
    public static void LogWarn(object msg)
    {
        LogMessage(new LogMessage { Level = WARN, Msg = msg });
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 216-218):
    // ------------------------------------------------------------
    // error: function(msg) {
    //     log.log({level:log.ERROR,msg:msg});
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Log a message at ERROR level.
    /// </summary>
    /// <param name="msg">The message</param>
    public static void LogError(object msg)
    {
        LogMessage(new LogMessage { Level = ERROR, Msg = msg });
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 224-226):
    // ------------------------------------------------------------
    // trace: function(msg) {
    //     log.log({level:log.TRACE,msg:msg});
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Log a message at TRACE level.
    /// </summary>
    /// <param name="msg">The message</param>
    public static void LogTrace(object msg)
    {
        LogMessage(new LogMessage { Level = TRACE, Msg = msg });
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 232-234):
    // ------------------------------------------------------------
    // debug: function(msg) {
    //     log.log({level:log.DEBUG,msg:msg});
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Log a message at DEBUG level.
    /// </summary>
    /// <param name="msg">The message</param>
    public static void LogDebug(object msg)
    {
        LogMessage(new LogMessage { Level = DEBUG, Msg = msg });
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 240-242):
    // ------------------------------------------------------------
    // metric: function() {
    //     return metricsEnabled;
    // },
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Check if metrics are enabled.
    /// </summary>
    /// <returns>True if metrics are enabled</returns>
    public static bool IsMetricEnabled()
    {
        return _metricsEnabled;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 248-256):
    // ------------------------------------------------------------
    // audit: function(msg,req) {
    //     msg.level = log.AUDIT;
    //     if (req) {
    //         msg.user = req.user;
    //         msg.path = req.path;
    //         msg.ip = req.ip || ...;
    //     }
    //     log.log(msg);
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Log an audit event.
    /// </summary>
    /// <param name="msg">The audit message</param>
    /// <param name="request">Optional request information</param>
    public static void Audit(LogMessage msg, AuditRequest? request = null)
    {
        msg.Level = AUDIT;
        if (request is not null)
        {
            msg.User = request.User;
            msg.Path = request.Path;
            msg.Ip = request.Ip ?? request.ForwardedFor ?? request.RemoteAddress;
        }
        LogMessage(msg);
    }
    // ============================================================

    /// <summary>
    /// Gets the verbose setting.
    /// </summary>
    public static bool Verbose => _verbose;

    /// <summary>
    /// Gets the name for a log level.
    /// </summary>
    /// <param name="level">The log level</param>
    /// <returns>The level name</returns>
    public static string GetLevelName(int level)
    {
        return LevelNames.TryGetValue(level, out var name) ? name : "unknown";
    }

    /// <summary>
    /// Gets the level value for a level name.
    /// </summary>
    /// <param name="name">The level name</param>
    /// <returns>The level value</returns>
    public static int GetLevelValue(string name)
    {
        return Levels.TryGetValue(name.ToLowerInvariant(), out var level) ? level : INFO;
    }
}

/// <summary>
/// Log message structure.
/// </summary>
public class LogMessage
{
    /// <summary>
    /// The log level.
    /// </summary>
    public int Level { get; set; } = Log.INFO;

    /// <summary>
    /// The message content.
    /// </summary>
    public object? Msg { get; set; }

    /// <summary>
    /// The timestamp in milliseconds since epoch.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// The node type (if applicable).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The node name (if applicable).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The node ID (if applicable).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The user (for audit logs).
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// The request path (for audit logs).
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// The IP address (for audit logs).
    /// </summary>
    public string? Ip { get; set; }
}

/// <summary>
/// Audit request information.
/// </summary>
public class AuditRequest
{
    /// <summary>
    /// The user.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// The request path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// The IP address.
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// The X-Forwarded-For header value.
    /// </summary>
    public string? ForwardedFor { get; set; }

    /// <summary>
    /// The remote address.
    /// </summary>
    public string? RemoteAddress { get; set; }
}

/// <summary>
/// Log settings.
/// </summary>
public class LogSettings
{
    /// <summary>
    /// Verbose logging mode.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Logging configuration by handler name.
    /// </summary>
    public Dictionary<string, LogHandlerConfig>? Logging { get; set; }
}

/// <summary>
/// Log handler configuration.
/// </summary>
public class LogHandlerConfig
{
    /// <summary>
    /// The log level.
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Whether metrics are enabled.
    /// </summary>
    public bool Metrics { get; set; }

    /// <summary>
    /// Whether audit logging is enabled.
    /// </summary>
    public bool Audit { get; set; }
}

// ============================================================
// ORIGINAL CODE (lines 68-89):
// ------------------------------------------------------------
// var LogHandler = function(settings) {
//     this.logLevel  = settings ? levels[settings.level]||levels.info : levels.info;
//     this.metricsOn = settings ? settings.metrics||false : false;
//     this.auditOn = settings ? settings.audit||false : false;
//     ...
// }
// ------------------------------------------------------------
// TRANSLATION:
// ------------------------------------------------------------
/// <summary>
/// Log handler that processes log messages.
/// </summary>
public class LogHandler
{
    /// <summary>
    /// The log level for this handler.
    /// </summary>
    public int LogLevel { get; }

    /// <summary>
    /// Whether metrics are enabled.
    /// </summary>
    public bool MetricsOn { get; }

    /// <summary>
    /// Whether audit logging is enabled.
    /// </summary>
    public bool AuditOn { get; }

    private readonly Action<LogMessage>? _customHandler;

    // ============================================================
    // ORIGINAL CODE (lines 51-60):
    // ------------------------------------------------------------
    // var levelColours = {
    //     10: 'red',
    //     20: 'red',
    //     30: 'yellow',
    //     40: '',
    //     50: 'cyan',
    //     60: 'gray',
    //     98: '',
    //     99: ''
    // };
    // ------------------------------------------------------------
    private static readonly Dictionary<int, ConsoleColor?> LevelColors = new()
    {
        { 10, ConsoleColor.Red },
        { 20, ConsoleColor.Red },
        { 30, ConsoleColor.Yellow },
        { 40, null },
        { 50, ConsoleColor.Cyan },
        { 60, ConsoleColor.Gray },
        { 98, null },
        { 99, null }
    };

    private static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    /// <summary>
    /// Creates a new log handler with the specified settings.
    /// </summary>
    /// <param name="settings">The handler settings</param>
    /// <param name="customHandler">Optional custom handler function</param>
    public LogHandler(LogHandlerConfig? settings, Action<LogMessage>? customHandler = null)
    {
        LogLevel = settings?.Level is not null ? Log.GetLevelValue(settings.Level) : Log.INFO;
        MetricsOn = settings?.Metrics ?? false;
        AuditOn = settings?.Audit ?? false;
        _customHandler = customHandler;
    }

    // ============================================================
    // ORIGINAL CODE (lines 84-88):
    // ------------------------------------------------------------
    // LogHandler.prototype.shouldReportMessage = function(msglevel) {
    //     return (msglevel == log.METRIC && this.metricsOn) ||
    //            (msglevel == log.AUDIT && this.auditOn) ||
    //            msglevel <= this.logLevel;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Determines if a message should be reported based on its level.
    /// </summary>
    /// <param name="msgLevel">The message level</param>
    /// <returns>True if the message should be reported</returns>
    public bool ShouldReportMessage(int msgLevel)
    {
        return (msgLevel == Log.METRIC && MetricsOn) ||
               (msgLevel == Log.AUDIT && AuditOn) ||
               msgLevel <= LogLevel;
    }
    // ============================================================

    /// <summary>
    /// Handle a log message.
    /// </summary>
    /// <param name="msg">The log message</param>
    public void HandleLog(LogMessage msg)
    {
        if (ShouldReportMessage(msg.Level))
        {
            if (_customHandler is not null)
            {
                _customHandler(msg);
            }
            else
            {
                ConsoleLogger(msg);
            }
        }
    }

    // ============================================================
    // ORIGINAL CODE (lines 94-129):
    // ------------------------------------------------------------
    // const utilLog = function (msg, level) { ... }
    // var consoleLogger = function(msg) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private void UtilLog(string message, int level)
    {
        var d = DateTime.Now;
        var time = $"{d.Hour:D2}:{d.Minute:D2}:{d.Second:D2}";
        var logLine = $"{d.Day} {Months[d.Month - 1]} {time} - {message}";

        if (LevelColors.TryGetValue(level, out var color) && color.HasValue)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            Console.WriteLine(logLine);
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.WriteLine(logLine);
        }
    }

    private void ConsoleLogger(LogMessage msg)
    {
        var levelName = Log.GetLevelName(msg.Level);

        if (msg.Level == Log.METRIC || msg.Level == Log.AUDIT)
        {
            UtilLog($"[{levelName}] {JsonSerializer.Serialize(msg)}", msg.Level);
        }
        else
        {
            string message;
            if (Log.Verbose && msg.Msg is Exception ex)
            {
                message = ex.ToString();
            }
            else
            {
                message = GetMessageString(msg.Msg);
            }

            var typeInfo = msg.Type is not null ? $"[{msg.Type}:{msg.Name ?? msg.Id}] " : "";
            UtilLog($"[{levelName}] {typeInfo}{message}", msg.Level);
        }
    }

    private static string GetMessageString(object? msg)
    {
        if (msg is null)
        {
            return "(null)";
        }
        if (msg is string s)
        {
            return s;
        }
        if (msg is Exception ex)
        {
            return ex.Message;
        }
        try
        {
            var str = msg.ToString();
            if (str == msg.GetType().ToString())
            {
                // Try to get a message property
                var msgProp = msg.GetType().GetProperty("Message");
                if (msgProp is not null)
                {
                    return msgProp.GetValue(msg)?.ToString() ?? str;
                }
            }
            return str ?? "(null)";
        }
        catch
        {
            return $"Exception trying to log: {msg.GetType().Name}";
        }
    }
    // ============================================================
}
