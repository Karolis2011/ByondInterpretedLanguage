using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using ByondLang.Interface.StateObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class ComputerProgram : BaseProgram
    {
        internal Terminal terminal;
        public string ComputerRef { get; private set; }

        public ComputerProgram(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            terminal = new Terminal(this);
        }

        public string GetTerminalBuffer()
        {
            return terminal.Stringify();
        }

        public void HandleTopic(string hash, string data)
        {
            if (!callbacks.ContainsKey(hash))
                throw new Exception("Unknown callback.");
            var weakCallback = callbacks[hash];
            JsCallback callback;
            if (weakCallback.TryGetTarget(out callback))
            {
                TimedFunction(() =>
                {
                    using (new JsContext.Scope(context))
                    {
                        JsValue callbackParam = data == null ? JsValue.Null : JsValue.FromString(data);
                        callback.CallbackFunction.CallFunction(JsValue.GlobalObject, callbackParam);
                    }
                }, HandleException, JsTaskPriority.CALLBACK);
            }
        }

        internal override bool HandleException(Exception exception)
        {
            var ex = exception as JsScriptException;
            if (ex != null)
            {
                terminal.PrintException(ex);
                return true;
            }
            else
                return base.HandleException(exception);
        }

        public override void InstallInterfaces()
        {
            base.InstallInterfaces();
            // Install APIs: term
            var glob = JsValue.GlobalObject;
            glob.SetProperty("Term", typeMapper.MTS(terminal), true); ;
        }
    }
}
