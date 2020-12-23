using System;

namespace ByondLang.ChakraCore.Hosting
{
    /// <summary>
    ///     A callback called before collecting an object.
    /// </summary>
    /// <remarks>
    ///     Use <c>JsSetObjectBeforeCollectCallback</c> to register this callback.
    /// </remarks>
    /// <param name="ref">The object to be collected.</param>
    /// <param name="callbackState">The state passed to <c>JsSetObjectBeforeCollectCallback</c>.</param>
    public delegate void JsObjectBeforeCollectCallback(JsValueRaw reference, IntPtr callbackState);
}