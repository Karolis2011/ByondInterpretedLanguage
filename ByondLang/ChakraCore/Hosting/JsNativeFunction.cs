﻿using System;
using System.Runtime.InteropServices;

namespace ByondLang.ChakraCore.Hosting
{
    /// <summary>
    ///     A function callback.
    /// </summary>
    /// <param name="callee">
    ///     A <c>Function</c> object that represents the function being invoked.
    /// </param>
    /// <param name="isConstructCall">Indicates whether this is a regular call or a 'new' call.</param>
    /// <param name="arguments">The arguments to the call.</param>
    /// <param name="argumentCount">The number of arguments.</param>
    /// <param name="callbackState">The state passed to <c>JsCreateFunction</c>.</param>
    /// <returns>The result of the call, if any.</returns>
    public delegate JsValue JsNativeFunction(
        JsValue callee,
        [MarshalAs(UnmanagedType.U1)] bool isConstructCall,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] JsValue[] arguments,
        ushort argumentCount,
        IntPtr callbackState);

}