using System;
using System.Runtime.Serialization;

namespace ByondLang.ChakraCore.Hosting
{
    /// <summary>
    ///     A script exception.
    /// </summary>
    public sealed class JsScriptException : JsException
    {

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsScriptException"/> class. 
        /// </summary>
        /// <param name="code">The error code returned.</param>
        /// <param name="error">The JavaScript error object.</param>
        public JsScriptException(JsErrorCode code, JsValue error) :
            this(code, error, "JavaScript Exception")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsScriptException"/> class. 
        /// </summary>
        /// <param name="code">The error code returned.</param>
        /// <param name="error">The JavaScript error object.</param>
        /// <param name="message">The error message.</param>
        public JsScriptException(JsErrorCode code, JsValue error, string message) :
            base(code, message)
        {
            Error = error;
        }

        /// <summary>
        ///     Gets a JavaScript object representing the script error.
        /// </summary>
        public JsValue Error { get; }
    }
}