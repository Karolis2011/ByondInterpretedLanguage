using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.UnitTests.ChakraCore
{
    public class CCFixture : IDisposable
    {
        public JsRuntime runtime;
        public JsContext context;
        public ByondLang.ChakraCore.TypeMapper typeMapper;
        public CCFixture()
        {
            runtime = JsRuntime.Create(JsRuntimeAttributes.AllowScriptInterrupt);
            context = runtime.CreateContext();
            context.AddRef();
            typeMapper = new ByondLang.ChakraCore.TypeMapper();
        }

        public void Dispose()
        {
            typeMapper.Dispose();
            context.Release();
            runtime.Dispose();
        }
    }
}
