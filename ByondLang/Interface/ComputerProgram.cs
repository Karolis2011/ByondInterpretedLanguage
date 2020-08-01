﻿using ByondLang.ChakraCore.Hosting;
using ByondLang.Interface.StateObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class ComputerProgram : BaseProgram
    {
        private Terminal terminal;
        public string ComputerRef { get; private set; }

        public ComputerProgram(Runtime runtime, JsContext context, ChakraCore.TypeMapper typeMapper, string computerRef) : base(runtime, context, typeMapper)
        {
            ComputerRef = computerRef;
            terminal = new Terminal(computerRef, this);
        }

        public string GetTerminalBuffer()
        {
            return terminal.Stringify();
        }

        public void HandleTopic(string hash, string data = "")
        {
            if (!callbacks.ContainsKey(hash))
                throw new Exception("Unknown callback.");
            var callback = callbacks[hash];
            _runtime.TimedFunction(() =>
            {
                using (new JsContext.Scope(_context))
                {
                    callback.CallFunction(JsValue.GlobalObject, JsValue.FromString(data));
                }
            }, HandleException);
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
            glob.SetProperty("Term", _typeMapper.MTS(terminal), true); ;
        }
    }
}