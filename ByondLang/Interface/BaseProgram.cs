using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    /// <summary>
    /// Direct replacemnt for original program
    /// </summary>
    public class BaseProgram : IDisposable
    {
        protected Runtime _runtime;
        protected JsContext _context;
        protected ChakraCore.TypeMapper _typeMapper;
        protected Task lastExecutionTask;

        public BaseProgram(Runtime runtime, JsContext context, ChakraCore.TypeMapper typeMapper)
        {
            context.AddRef();
            _runtime = runtime;
            _context = context;
            _typeMapper = typeMapper;
            _runtime.Function(() =>
            {
                using (new JsContext.Scope(_context))
                {
                    InstallInterfaces();
                }
            });
        }


        public void Dispose()
        {
            _runtime.RemoveContext(this);
            _context.Release();
        }

        public virtual void InstallInterfaces()
        {
            // Add generic global APIs accessible from everywhere
        }


        public Task<JsValue> ExecuteScript(string script)
        {
            return _runtime.TimedFunction(() =>
            {
                using (new JsContext.Scope(_context))
                {
                    return JsContext.RunScript(script);
                }
            });
        }
    }
}
