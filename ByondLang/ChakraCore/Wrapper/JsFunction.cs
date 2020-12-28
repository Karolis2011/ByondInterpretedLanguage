using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsFunction : JsValue
    {
        public static new bool isSupported(JsValueType type) => type == JsValueType.Function;

        public JsFunction(JsValueRaw jsValue)
        {
            if (!jsValue.IsValid)
                throw new Exception("Indalid value");
            if (jsValue.ValueType != JsValueType.Function)
                throw new Exception("Invalid Value type");
            this.jsValue = jsValue;
            jsValue.AddRef(); // Mark as used
        }

        public JsValue Invoke(params JsValue[] arguments)
        {
            var result = jsValue.CallFunction(arguments: arguments.Cast<JsValueRaw>().ToArray());
            return JsValue.FromRaw(result);
        }
    }
}
