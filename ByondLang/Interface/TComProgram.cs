using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using ByondLang.Interface.StateObjects;
using ByondLang.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class TComProgram : BaseProgram

    {
        private Communications comm;

        public TComProgram(Runtime runtime, JsContext context, ChakraCore.TypeMapper typeMapper) : base(runtime, context, typeMapper)
        {
            comm = new Communications(runtime.Service);
        }

        public JsTask ProcessSignal(TComSignal signal)
        {
            return _runtime.TimedFunction(() =>
            {
                comm.broadcast(signal);
                using (new JsContext.Scope(_context))
                {
                    if (comm.handler.IsValid && comm.handler.ValueType == JsValueType.Function && signal != null)
                        comm.handler.CallFunction(_typeMapper.MTS(signal));
                }
            }, this, HandleException, JsTaskPriority.CALLBACK);
        }

        public TComSignal[] GetSignals() => comm.GetSignals();

        public override void InstallInterfaces()
        {
            base.InstallInterfaces();
            // Install APIs: comm
            var glob = JsValue.GlobalObject;
            glob.SetProperty("Comm", _typeMapper.MTS(comm), true); ;
        }
    }
}
