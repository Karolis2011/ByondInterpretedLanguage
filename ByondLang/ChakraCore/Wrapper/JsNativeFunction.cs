using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsNativeFunction : JsFunction
    {
        internal Hosting.JsNativeFunction nativeFunction;

        public JsNativeFunction(Hosting.JsNativeFunction function, IntPtr callbackState)
        {
            if (function == null)
                throw new ArgumentNullException("Expected a function, got null.");

            nativeFunction = function;
            jsValue = JsValueRaw.CreateFunction(function, callbackState);
        }
    }
}
