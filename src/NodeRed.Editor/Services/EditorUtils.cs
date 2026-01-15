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
    /// Returns FontAwesome unicode characters for use in SVG text elements.
    /// Translated from Node-RED's node icon definitions.
    /// </summary>
    public static string GetNodeIcon(string nodeType)
    {
        // FontAwesome 4.7 unicode values
        return nodeType switch
        {
            "inject" => "\uf017",       // fa-clock-o
            "debug" => "\uf188",        // fa-bug
            "function" => "\uf121",     // fa-code
            "switch" => "\uf0e7",       // fa-bolt
            "change" => "\uf040",       // fa-pencil
            "template" => "\uf15c",     // fa-file-text
            "delay" => "\uf017",        // fa-clock-o
            "trigger" => "\uf0e7",      // fa-bolt
            "http request" => "\uf0ac", // fa-globe
            "http in" => "\uf061",      // fa-arrow-right
            "http response" => "\uf060", // fa-arrow-left
            "mqtt in" or "mqtt out" => "\uf1eb", // fa-wifi
            "file" or "file in" => "\uf15b",     // fa-file
            "json" or "csv" or "xml" => "\uf0ea", // fa-clipboard
            "comment" => "\uf075",      // fa-comment
            "complete" or "catch" or "status" => "\uf013", // fa-cog
            "link in" or "link out" or "link call" => "\uf0c1", // fa-link
            "websocket in" or "websocket out" => "\uf1e6", // fa-plug
            "tcp in" or "tcp out" or "udp in" or "udp out" => "\uf0e0", // fa-envelope
            "split" or "join" => "\uf0db",  // fa-columns
            "sort" or "batch" => "\uf0dc",  // fa-sort
            "html" or "yaml" => "\uf016",   // fa-file-o
            "watch" => "\uf06e",        // fa-eye
            "exec" => "\uf120",         // fa-terminal
            "range" => "\uf07e",        // fa-arrows-h
            "rbe" => "\uf0b0",          // fa-filter
            _ => "\uf1b2"               // fa-cube (default)
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
