using ByondLang.ChakraCore.Hosting;
using ByondLang.ChakraCore.Reflection;
using ByondLang.Models;
using ByondLang.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Interface.StateObjects
{
    [JsObject]
    public class Communications
    {
        internal JsValueRaw handler = JsValueRaw.Invalid;
        private NTSL3Service service;
        private List<TComSignal> pendingSignals = new List<TComSignal>();


        public Communications(NTSL3Service service)
        {
            this.service = service;
        }

        [JsCallable]
        public void setSignalHandler(JsValueRaw handler)
        {
            var type = handler.ValueType;
            if (type != JsValueType.Function && type != JsValueType.Null)
                throw new Exception("Invalid paramter type.");
            if(this.handler.IsValid)
                this.handler.Release();
            if(type == JsValueType.Null)
            {
                this.handler = JsValueRaw.Invalid;
                return;
            }
            handler.AddRef();
            this.handler = handler;
        }

        [JsCallable]
        public TComSignal createSignal()
        {
            return new TComSignal();
        }

        [JsCallable]
        public TComSignal broadcast()
        {
            var n = createSignal();
            AddSignalIfCan(n);
            return n;
        }

        [JsCallable]
        public TComSignal broadcast(TComSignal signal)
        {
            AddSignalIfCan(signal);
            return signal;
        }

        private void AddSignalIfCan(TComSignal signal)
        {
            if (!pendingSignals.Contains(signal))
                pendingSignals.Add(signal);
        }

        public TComSignal[] GetSignals()
        {
            var signals = pendingSignals.ToArray();
            pendingSignals.Clear();
            return signals;
        }
    }
}
