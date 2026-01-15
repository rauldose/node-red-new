// ============================================================
// SOURCE: packages/node_modules/@node-red/util/lib/util.js
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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NodeRed.Util;

/// <summary>
/// General utilities for Node-RED runtime and editor.
/// Translated from: @node-red/util/lib/util.js
/// </summary>
public static class Util
{
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

    // ============================================================
    // ORIGINAL CODE (lines 44-50):
    // ------------------------------------------------------------
    // function generateId() {
    //     var bytes = [];
    //     for (var i=0;i<8;i++) {
    //         bytes.push(Math.round(0xff*Math.random()).toString(16).padStart(2,'0'));
    //     }
    //     return bytes.join("");
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Generates a pseudo-unique-random id.
    /// </summary>
    /// <returns>A random-ish id as a 16-character hex string</returns>
    public static string GenerateId()
    {
        var bytes = new byte[8];
        _rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 60-69):
    // ------------------------------------------------------------
    // function ensureString(o) {
    //     if (Buffer.isBuffer(o)) {
    //         return o.toString();
    //     } else if (typeof o === "object") {
    //         return JSON.stringify(o);
    //     } else if (typeof o === "string") {
    //         return o;
    //     }
    //     return ""+o;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Converts the provided argument to a String, using type-dependent methods.
    /// </summary>
    /// <param name="o">The property to convert to a String</param>
    /// <returns>The stringified version</returns>
    public static string EnsureString(object? o)
    {
        if (o is byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer);
        }
        else if (o is string s)
        {
            return s;
        }
        else if (o is not null && o.GetType().IsClass && o.GetType() != typeof(string))
        {
            return JsonSerializer.Serialize(o);
        }
        return o?.ToString() ?? string.Empty;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 79-88):
    // ------------------------------------------------------------
    // function ensureBuffer(o) {
    //     if (Buffer.isBuffer(o)) {
    //         return o;
    //     } else if (typeof o === "object") {
    //         o = JSON.stringify(o);
    //     } else if (typeof o !== "string") {
    //         o = ""+o;
    //     }
    //     return Buffer.from(o);
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Converts the provided argument to a byte array (Buffer), using type-dependent methods.
    /// </summary>
    /// <param name="o">The property to convert to a Buffer</param>
    /// <returns>The byte array version</returns>
    public static byte[] EnsureBuffer(object? o)
    {
        if (o is byte[] buffer)
        {
            return buffer;
        }
        
        string str;
        if (o is string s)
        {
            str = s;
        }
        else if (o is not null && o.GetType().IsClass && o.GetType() != typeof(string))
        {
            str = JsonSerializer.Serialize(o);
        }
        else
        {
            str = o?.ToString() ?? string.Empty;
        }
        return Encoding.UTF8.GetBytes(str);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 98-118):
    // ------------------------------------------------------------
    // function cloneMessage(msg) {
    //     if (typeof msg !== "undefined" && msg !== null) {
    //         // Temporary fix for #97
    //         // TODO: remove this http-node-specific fix somehow
    //         var req = msg.req;
    //         var res = msg.res;
    //         delete msg.req;
    //         delete msg.res;
    //         var m = clonedeep(msg);
    //         if (req) {
    //             m.req = req;
    //             msg.req = req;
    //         }
    //         if (res) {
    //             m.res = res;
    //             msg.res = res;
    //         }
    //         return m;
    //     }
    //     return msg;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Safely clones a message object. This handles msg.req/msg.res objects that must not be cloned.
    /// </summary>
    /// <param name="msg">The message object to clone</param>
    /// <returns>The cloned message</returns>
    public static FlowMessage? CloneMessage(FlowMessage? msg)
    {
        if (msg is null)
        {
            return null;
        }

        // Temporary fix for #97 - preserve req/res references
        var req = msg.Req;
        var res = msg.Res;
        msg.Req = null;
        msg.Res = null;

        // Deep clone via JSON serialization (equivalent to lodash.clonedeep)
        var json = JsonSerializer.Serialize(msg);
        var m = JsonSerializer.Deserialize<FlowMessage>(json);

        // Restore req/res on both original and clone
        if (req is not null)
        {
            m!.Req = req;
            msg.Req = req;
        }
        if (res is not null)
        {
            m!.Res = res;
            msg.Res = res;
        }
        return m;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 128-192):
    // ------------------------------------------------------------
    // function compareObjects(obj1,obj2) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Compares two objects, handling various types.
    /// </summary>
    /// <param name="obj1">First object</param>
    /// <param name="obj2">Second object</param>
    /// <returns>Whether the two objects are the same</returns>
    public static bool CompareObjects(object? obj1, object? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }
        if (obj1 is null || obj2 is null)
        {
            return false;
        }

        // Handle arrays
        var isArray1 = obj1 is Array || obj1 is System.Collections.IList;
        var isArray2 = obj2 is Array || obj2 is System.Collections.IList;
        if (isArray1 != isArray2)
        {
            return false;
        }
        if (isArray1 && isArray2)
        {
            var list1 = (System.Collections.IList)obj1;
            var list2 = (System.Collections.IList)obj2;
            if (list1.Count != list2.Count)
            {
                return false;
            }
            for (int i = 0; i < list1.Count; i++)
            {
                if (!CompareObjects(list1[i], list2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        // Handle byte arrays (Buffer equivalent)
        var isBuffer1 = obj1 is byte[];
        var isBuffer2 = obj2 is byte[];
        if (isBuffer1 != isBuffer2)
        {
            return false;
        }
        if (isBuffer1 && isBuffer2)
        {
            var buf1 = (byte[])obj1;
            var buf2 = (byte[])obj2;
            return buf1.SequenceEqual(buf2);
        }

        // Handle primitive types
        if (obj1.GetType() != obj2.GetType())
        {
            return false;
        }

        // Handle dictionaries/objects
        if (obj1 is IDictionary<string, object?> dict1 && obj2 is IDictionary<string, object?> dict2)
        {
            if (dict1.Count != dict2.Count)
            {
                return false;
            }
            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var value2))
                {
                    return false;
                }
                if (!CompareObjects(kvp.Value, value2))
                {
                    return false;
                }
            }
            return true;
        }

        // Fallback to Equals
        return obj1.Equals(obj2);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 194-198):
    // ------------------------------------------------------------
    // function createError(code, message) {
    //     var e = new Error(message);
    //     e.code = code;
    //     return e;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Creates an error with a code.
    /// </summary>
    /// <param name="code">Error code</param>
    /// <param name="message">Error message</param>
    /// <returns>A NodeRedException with the specified code and message</returns>
    public static NodeRedException CreateError(string code, string message)
    {
        return new NodeRedException(code, message);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 217-383):
    // ------------------------------------------------------------
    // function normalisePropertyExpression(str, msg, toString) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Parses a property expression, such as `msg.foo.bar[3]` to validate it
    /// and convert it to a canonical version expressed as an Array of property names.
    /// </summary>
    /// <param name="str">The property expression</param>
    /// <param name="msg">Optional message object for cross-reference evaluation</param>
    /// <param name="toString">If true, returns a normalized string representation</param>
    /// <returns>The normalised expression as a list of property parts</returns>
    public static List<object> NormalisePropertyExpression(string str, FlowMessage? msg = null, bool toString = false)
    {
        int length = str.Length;
        if (length == 0)
        {
            throw CreateError("INVALID_EXPR", "Invalid property expression: zero-length");
        }

        var parts = new List<object>();
        int start = 0;
        bool inString = false;
        bool inBox = false;
        char quoteChar = '\0';

        for (int i = 0; i < length; i++)
        {
            char c = str[i];
            if (!inString)
            {
                if (c == '\'' || c == '"')
                {
                    if (i != start)
                    {
                        throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected {c} at position {i}");
                    }
                    inString = true;
                    quoteChar = c;
                    start = i + 1;
                }
                else if (c == '.')
                {
                    if (i == 0)
                    {
                        throw CreateError("INVALID_EXPR", "Invalid property expression: unexpected . at position 0");
                    }
                    if (start != i)
                    {
                        var v = str.Substring(start, i - start);
                        if (int.TryParse(v, out int num))
                        {
                            parts.Add(num);
                        }
                        else
                        {
                            parts.Add(v);
                        }
                    }
                    if (i == length - 1)
                    {
                        throw CreateError("INVALID_EXPR", "Invalid property expression: unterminated expression");
                    }
                    // Next char is first char of an identifier: a-z 0-9 $ _
                    if (!Regex.IsMatch(str[i + 1].ToString(), @"[a-z0-9\$_]", RegexOptions.IgnoreCase))
                    {
                        throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected {str[i + 1]} at position {i + 1}");
                    }
                    start = i + 1;
                }
                else if (c == '[')
                {
                    if (i == 0)
                    {
                        throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected {c} at position {i}");
                    }
                    if (start != i)
                    {
                        parts.Add(str.Substring(start, i - start));
                    }
                    if (i == length - 1)
                    {
                        throw CreateError("INVALID_EXPR", "Invalid property expression: unterminated expression");
                    }
                    // Check for nested msg reference
                    if (i + 1 < length && Regex.IsMatch(str.Substring(i + 1), @"^msg[.\[]"))
                    {
                        int depth = 1;
                        bool inLocalString = false;
                        char localStringQuote = '\0';
                        for (int j = i + 1; j < length; j++)
                        {
                            if (str[j] == '"' || str[j] == '\'')
                            {
                                if (inLocalString)
                                {
                                    if (str[j] == localStringQuote)
                                    {
                                        inLocalString = false;
                                    }
                                }
                                else
                                {
                                    inLocalString = true;
                                    localStringQuote = str[j];
                                }
                            }
                            if (str[j] == '[')
                            {
                                depth++;
                            }
                            else if (str[j] == ']')
                            {
                                depth--;
                            }
                            if (depth == 0)
                            {
                                try
                                {
                                    if (msg is not null)
                                    {
                                        var crossRefProp = GetMessageProperty(msg, str.Substring(i + 1, j - i - 1));
                                        if (crossRefProp is null)
                                        {
                                            throw CreateError("INVALID_EXPR", $"Invalid expression: undefined reference at position {i + 1} : {str.Substring(i + 1, j - i - 1)}");
                                        }
                                        parts.Add(crossRefProp);
                                    }
                                    else
                                    {
                                        parts.Add(NormalisePropertyExpression(str.Substring(i + 1, j - i - 1), msg));
                                    }
                                    inBox = false;
                                    i = j;
                                    start = j + 1;
                                    break;
                                }
                                catch
                                {
                                    throw CreateError("INVALID_EXPR", $"Invalid expression started at position {i + 1}");
                                }
                            }
                        }
                        if (depth > 0)
                        {
                            throw CreateError("INVALID_EXPR", $"Invalid property expression: unmatched '[' at position {i}");
                        }
                        continue;
                    }
                    else if (!Regex.IsMatch(str[i + 1].ToString(), @"[""'\d]"))
                    {
                        throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected {str[i + 1]} at position {i + 1}");
                    }
                    start = i + 1;
                    inBox = true;
                }
                else if (c == ']')
                {
                    if (!inBox)
                    {
                        throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected {c} at position {i}");
                    }
                    if (start != i)
                    {
                        var v = str.Substring(start, i - start);
                        if (Regex.IsMatch(v, @"^\d+$"))
                        {
                            parts.Add(int.Parse(v));
                        }
                        else
                        {
                            throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected array expression at position {start}");
                        }
                    }
                    start = i + 1;
                    inBox = false;
                }
                else if (c == ' ')
                {
                    throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected ' ' at position {i}");
                }
            }
            else
            {
                if (c == quoteChar)
                {
                    if (i - start == 0)
                    {
                        throw CreateError("INVALID_EXPR", $"Invalid property expression: zero-length string at position {start}");
                    }
                    parts.Add(str.Substring(start, i - start));
                    // If inBox, next char must be a ]. Otherwise it may be [ or .
                    if (inBox && (i + 1 >= length || str[i + 1] != ']'))
                    {
                        throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected array expression at position {start}");
                    }
                    else if (!inBox && i + 1 != length && !Regex.IsMatch(str[i + 1].ToString(), @"[\[.]"))
                    {
                        throw CreateError("INVALID_EXPR", $"Invalid property expression: unexpected {str[i + 1]} expression at position {i + 1}");
                    }
                    start = i + 1;
                    inString = false;
                }
            }
        }

        if (inBox || inString)
        {
            throw CreateError("INVALID_EXPR", "Invalid property expression: unterminated expression");
        }
        if (start < length)
        {
            parts.Add(str.Substring(start));
        }

        if (toString)
        {
            var result = new StringBuilder();
            if (parts.Count > 0)
            {
                result.Append(parts[0]?.ToString() ?? string.Empty);
            }
            for (int i = 1; i < parts.Count; i++)
            {
                var p = parts[i];
                string pStr;
                if (p is string ps)
                {
                    if (ps.Contains('"'))
                    {
                        pStr = $"'{ps}'";
                    }
                    else
                    {
                        pStr = $"\"{ps}\"";
                    }
                }
                else
                {
                    pStr = p?.ToString() ?? string.Empty;
                }
                result.Append($"[{pStr}]");
            }
            return new List<object> { result.ToString() };
        }

        return parts;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 397-402):
    // ------------------------------------------------------------
    // function getMessageProperty(msg,expr) {
    //     if (expr.indexOf('msg.')===0) {
    //         expr = expr.substring(4);
    //     }
    //     return getObjectProperty(msg,expr);
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Gets a property of a message object.
    /// Unlike GetObjectProperty, this function will strip `msg.` from the front of the property expression if present.
    /// </summary>
    /// <param name="msg">The message object</param>
    /// <param name="expr">The property expression</param>
    /// <returns>The message property, or null if it does not exist</returns>
    public static object? GetMessageProperty(FlowMessage msg, string expr)
    {
        if (expr.StartsWith("msg."))
        {
            expr = expr.Substring(4);
        }
        return GetObjectProperty(msg, expr);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 426-434):
    // ------------------------------------------------------------
    // function getObjectProperty(msg,expr) {
    //     var result = null;
    //     var msgPropParts = normalisePropertyExpression(expr,msg);
    //     msgPropParts.reduce(function(obj, key) {
    //         result = (typeof obj[key] !== "undefined" ? obj[key] : undefined);
    //         return result;
    //     }, msg);
    //     return result;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Gets a property of an object.
    /// </summary>
    /// <param name="obj">The object</param>
    /// <param name="expr">The property expression</param>
    /// <returns>The object property, or null if it does not exist</returns>
    public static object? GetObjectProperty(object obj, string expr)
    {
        var msgPropParts = NormalisePropertyExpression(expr, obj as FlowMessage);
        object? result = obj;

        foreach (var key in msgPropParts)
        {
            if (result is null)
            {
                return null;
            }

            if (key is int index)
            {
                if (result is System.Collections.IList list && index < list.Count)
                {
                    result = list[index];
                }
                else
                {
                    return null;
                }
            }
            else if (key is string propName)
            {
                result = GetPropertyValue(result, propName);
            }
        }

        return result;
    }

    private static object? GetPropertyValue(object obj, string propertyName)
    {
        if (obj is IDictionary<string, object?> dict)
        {
            return dict.TryGetValue(propertyName, out var value) ? value : null;
        }
        if (obj is JsonElement jsonElement)
        {
            if (jsonElement.TryGetProperty(propertyName, out var prop))
            {
                return JsonElementToObject(prop);
            }
            return null;
        }

        var prop2 = obj.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        return prop2?.GetValue(obj);
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
            _ => null
        };
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 448-453):
    // ------------------------------------------------------------
    // function setMessageProperty(msg,prop,value,createMissing) {
    //     if (prop.indexOf('msg.')===0) {
    //         prop = prop.substring(4);
    //     }
    //     return setObjectProperty(msg,prop,value,createMissing);
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Sets a property of a message object.
    /// Unlike SetObjectProperty, this function will strip `msg.` from the front of the property expression if present.
    /// </summary>
    /// <param name="msg">The message object</param>
    /// <param name="prop">The property expression</param>
    /// <param name="value">The value to set</param>
    /// <param name="createMissing">Whether to create missing parent properties</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool SetMessageProperty(FlowMessage msg, string prop, object? value, bool? createMissing = null)
    {
        if (prop.StartsWith("msg."))
        {
            prop = prop.Substring(4);
        }
        return SetObjectProperty(msg, prop, value, createMissing);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 464-527):
    // ------------------------------------------------------------
    // function setObjectProperty(msg,prop,value,createMissing) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Sets a property of an object.
    /// </summary>
    /// <param name="obj">The object</param>
    /// <param name="prop">The property expression</param>
    /// <param name="value">The value to set</param>
    /// <param name="createMissing">Whether to create missing parent properties</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool SetObjectProperty(object obj, string prop, object? value, bool? createMissing = null)
    {
        createMissing ??= value is not null;

        var msgPropParts = NormalisePropertyExpression(prop, obj as FlowMessage);
        int length = msgPropParts.Count;
        object? current = obj;

        for (int i = 0; i < length - 1; i++)
        {
            var key = msgPropParts[i];
            
            if (key is string propName)
            {
                var nextValue = GetPropertyValue(current!, propName);
                if (nextValue is null)
                {
                    if (createMissing.Value)
                    {
                        var nextKey = msgPropParts[i + 1];
                        var newValue = nextKey is string ? new Dictionary<string, object?>() : (object)new List<object?>();
                        SetPropertyValue(current!, propName, newValue);
                        current = newValue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    current = nextValue;
                }
            }
            else if (key is int index)
            {
                if (current is System.Collections.IList list)
                {
                    if (index < list.Count)
                    {
                        current = list[index];
                    }
                    else if (createMissing.Value)
                    {
                        while (list.Count <= index)
                        {
                            list.Add(null);
                        }
                        var nextKey = msgPropParts[i + 1];
                        var newValue = nextKey is string ? new Dictionary<string, object?>() : (object)new List<object?>();
                        list[index] = newValue;
                        current = newValue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        var finalKey = msgPropParts[length - 1];
        
        if (value is null)
        {
            if (finalKey is int index && current is System.Collections.IList list)
            {
                if (index < list.Count)
                {
                    list.RemoveAt(index);
                }
            }
            else if (finalKey is string propName && current is IDictionary<string, object?> dict)
            {
                dict.Remove(propName);
            }
        }
        else
        {
            if (finalKey is int index && current is System.Collections.IList list)
            {
                while (list.Count <= index)
                {
                    list.Add(null);
                }
                list[index] = value;
            }
            else if (finalKey is string propName)
            {
                SetPropertyValue(current!, propName, value);
            }
        }

        return true;
    }

    private static void SetPropertyValue(object obj, string propertyName, object? value)
    {
        if (obj is IDictionary<string, object?> dict)
        {
            dict[propertyName] = value;
            return;
        }

        var prop = obj.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        prop?.SetValue(obj, value);
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 800-811):
    // ------------------------------------------------------------
    // function normaliseNodeTypeName(name) {
    //     var result = name.replace(/[^a-zA-Z0-9]/g, " ");
    //     result = result.trim();
    //     result = result.replace(/ +/g, " ");
    //     result = result.replace(/ ./g,
    //         function(s) {
    //             return s.charAt(1).toUpperCase();
    //         }
    //     );
    //     result = result.charAt(0).toLowerCase() + result.slice(1);
    //     return result;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Normalise a node type name to camel case.
    /// For example: `a-random node type` will normalise to `aRandomNodeType`
    /// </summary>
    /// <param name="name">The node type</param>
    /// <returns>The normalised name</returns>
    public static string NormaliseNodeTypeName(string name)
    {
        var result = Regex.Replace(name, @"[^a-zA-Z0-9]", " ");
        result = result.Trim();
        result = Regex.Replace(result, @" +", " ");
        result = Regex.Replace(result, @" .", m => m.Value[1].ToString().ToUpperInvariant());
        if (result.Length > 0)
        {
            result = char.ToLowerInvariant(result[0]) + result.Substring(1);
        }
        return result;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 604-614):
    // ------------------------------------------------------------
    // function parseContextStore(key) {
    //     var parts = {};
    //     var m = /^#:\((\S+?)\)::(.*)$/.exec(key);
    //     if (m) {
    //         parts.store = m[1];
    //         parts.key = m[2];
    //     } else {
    //         parts.key = key;
    //     }
    //     return parts;
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Parses a context property string, as generated by the TypedInput, to extract the store name if present.
    /// For example, `#:(file)::foo` results in `{ Store: "file", Key: "foo" }`.
    /// </summary>
    /// <param name="key">The context property string to parse</param>
    /// <returns>The parsed property</returns>
    public static ContextStoreParts ParseContextStore(string key)
    {
        var match = Regex.Match(key, @"^#:\((\S+?)\)::(.*)$");
        if (match.Success)
        {
            return new ContextStoreParts
            {
                Store = match.Groups[1].Value,
                Key = match.Groups[2].Value
            };
        }
        return new ContextStoreParts { Key = key };
    }
    // ============================================================
}

/// <summary>
/// Represents parsed context store parts.
/// </summary>
public class ContextStoreParts
{
    /// <summary>
    /// The store name (if present).
    /// </summary>
    public string? Store { get; set; }

    /// <summary>
    /// The key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
