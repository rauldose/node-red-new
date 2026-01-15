// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/utils.js
// ============================================================
// Shared UI utility methods.
// ============================================================

namespace NodeRed.Editor.Services;

/// <summary>
/// Utility methods for the editor UI.
/// </summary>
public static class EditorUtils
{
    /// <summary>
    /// Darkens a hex color by a factor (0.0 - 1.0).
    /// Translated from Node-RED's color utility functions.
    /// </summary>
    public static string DarkenColor(string color, double factor)
    {
        if (color.StartsWith("#") && color.Length >= 7)
        {
            try
            {
                var r = Convert.ToInt32(color.Substring(1, 2), 16);
                var g = Convert.ToInt32(color.Substring(3, 2), 16);
                var b = Convert.ToInt32(color.Substring(5, 2), 16);

                r = (int)(r * (1 - factor));
                g = (int)(g * (1 - factor));
                b = (int)(b * (1 - factor));

                return $"#{r:X2}{g:X2}{b:X2}";
            }
            catch
            {
                return color;
            }
        }
        return color;
    }

    /// <summary>
    /// Gets the icon for a node type.
    /// Translated from Node-RED's node icon definitions.
    /// </summary>
    public static string GetNodeIcon(string nodeType)
    {
        return nodeType switch
        {
            "inject" => "⏰",
            "debug" => "🐛",
            "function" => "𝑓",
            "switch" => "⚡",
            "change" => "✎",
            "template" => "📝",
            "delay" => "⏱",
            "trigger" => "⚡",
            "http request" => "🌐",
            "http in" => "→",
            "http response" => "←",
            "mqtt in" or "mqtt out" => "📡",
            "file" or "file in" => "📁",
            "json" or "csv" or "xml" => "📋",
            "comment" => "💬",
            "complete" or "catch" or "status" => "⚙",
            "link in" or "link out" or "link call" => "🔗",
            "websocket in" or "websocket out" => "🔌",
            "tcp in" or "tcp out" or "udp in" or "udp out" => "📨",
            "split" or "join" => "⧉",
            "sort" or "batch" => "📊",
            "html" or "yaml" => "📄",
            "watch" => "👁",
            "exec" => "▶",
            "range" => "↔",
            "rbe" => "Δ",
            _ => "■"
        };
    }

    /// <summary>
    /// Calculates a bezier wire path between two points.
    /// Translated from Node-RED's view.js wire calculation.
    /// </summary>
    public static string CalculateWirePath(int x1, int y1, int x2, int y2)
    {
        var dx = Math.Abs(x2 - x1);
        var dy = Math.Abs(y2 - y1);
        
        var cp = Math.Max(75, dx / 2);
        
        if (x2 < x1)
        {
            cp = Math.Max(75, dy / 2);
        }
        
        return $"M {x1} {y1} C {x1 + cp} {y1}, {x2 - cp} {y2}, {x2} {y2}";
    }
}
