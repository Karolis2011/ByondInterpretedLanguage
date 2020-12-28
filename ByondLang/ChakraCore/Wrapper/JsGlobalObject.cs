using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsGlobalObject : JsObject
    {
        public static new bool isSupported(JsValueType type) => false;
        public JsGlobalObject() : base()
        {
            jsValue = JsValueRaw.GlobalObject;
            jsValue.AddRef();
        }
    }
}
