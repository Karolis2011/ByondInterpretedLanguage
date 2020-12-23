using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsUndefined : JsValue
    {
        public JsUndefined()
        {
            jsValue = JsValueRaw.Undefined;
            jsValue.AddRef();
        }

        public JsUndefined(JsValueRaw value) : base(value) { }
    }
}
