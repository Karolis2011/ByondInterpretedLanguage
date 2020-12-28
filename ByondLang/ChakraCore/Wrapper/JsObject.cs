using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsObject : JsValue
    {
        public static new bool isSupported(JsValueType type) => type == JsValueType.Object;

        protected JsObject() { }

        public JsObject(JsValueRaw jsValue)
        {
            if (!jsValue.IsValid)
                throw new Exception("Indalid value");
            if (jsValue.ValueType != JsValueType.Object)
                throw new Exception("Invalid Value type");
            this.jsValue = jsValue;
            jsValue.AddRef(); // Mark as used
        }
    }
}
