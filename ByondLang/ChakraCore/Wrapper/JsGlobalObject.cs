using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsGlobalObject : JsObject
    {

        public JsGlobalObject()
        {
            jsValue = JsValueRaw.GlobalObject;
            jsValue.AddRef();
        }
    }
}
