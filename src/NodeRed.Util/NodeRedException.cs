// ============================================================
// SOURCE: Error handling from @node-red/util/lib/util.js
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
// ORIGINAL CODE (lines 194-198):
// ------------------------------------------------------------
// function createError(code, message) {
//     var e = new Error(message);
//     e.code = code;
//     return e;
// }
// ------------------------------------------------------------

namespace NodeRed.Util;

/// <summary>
/// Represents an error with an associated code.
/// Equivalent to the JavaScript Error with a code property.
/// </summary>
public class NodeRedException : Exception
{
    /// <summary>
    /// The error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Creates a new NodeRedException with the specified code and message.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    public NodeRedException(string code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Creates a new NodeRedException with the specified code, message, and inner exception.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public NodeRedException(string code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}
