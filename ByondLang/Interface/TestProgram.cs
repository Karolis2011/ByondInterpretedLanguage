using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class TestProgram : BaseProgram
    {
        public bool HasErrored { get; private set; } = false;

        public TestProgram(Runtime runtime, JsContext context, ChakraCore.TypeMapper typeMapper) : base(runtime, context, typeMapper)
        {
        }

        internal override bool HandleException(Exception exception)
        {
            if(exception is JsScriptException scriptException)
                if (scriptException.ErrorCode == JsErrorCode.ScriptTerminated)
                    return true;
            HasErrored = true;
            return base.HandleException(exception);
        }

    }
}
