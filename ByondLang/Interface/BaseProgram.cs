﻿using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    /// <summary>
    /// Direct replacemnt for original program
    /// </summary>
    public class BaseProgram : IDisposable
    {
        public const int CALLBACK_HASH_LEN = 12;
        private static Random random = new Random();
        protected Runtime _runtime;
        protected JsContext _context;
        protected TypeMapper _typeMapper;
        protected Task lastExecutionTask;
        protected Dictionary<string, JsValue> callbacks = new Dictionary<string, JsValue>();
        private ILogger<BaseProgram> logger;

        public BaseProgram(Runtime runtime, JsContext context, TypeMapper typeMapper)
        {
            context.AddRef();
            _runtime = runtime;
            _context = context;
            _typeMapper = typeMapper;
            logger = runtime.serviceProvider.GetService<ILogger<BaseProgram>>();
        }

        public void InitializeState()
        {
            _runtime.Function(() =>
            {
                using (new JsContext.Scope(_context))
                {
                    InstallInterfaces();
                }
            }, this, priority: JsTaskPriority.INITIALIZATION);
        }

        public void Dispose()
        {
            _runtime.RemoveContext(this);
            foreach (var callback in callbacks)
            {
                callback.Value.Release();
            }
            callbacks.Clear();
            _context.Release();
        }

        internal string RegisterCallback(JsValue callback)
        {
            callback.AddRef();
            var hash = GenerateCallbackHash();
            callbacks[hash] = callback;
            return hash;
        }

        private string GenerateCallbackHash()
        {
            string characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            StringBuilder result = new StringBuilder(CALLBACK_HASH_LEN);
            for (int i = 0; i < CALLBACK_HASH_LEN; i++)
            {
                result.Append(characters[random.Next(characters.Length)]);
            }
            var finalResult = result.ToString();
            if (callbacks.ContainsKey(finalResult))
                return GenerateCallbackHash();
            return finalResult;
        }

        public virtual void InstallInterfaces()
        {
            // Add generic global APIs accessible from everywhere
        }

        internal virtual bool HandleException(Exception exception)
        {
            logger.LogError(exception, "Unhandled runtime exception.");
            return false;
        }

        public JsTask<JsValue> ExecuteScript(string script)
        {
            return _runtime.TimedFunction(() =>
            {
                using (new JsContext.Scope(_context))
                {
                    return JsContext.RunScript(script);
                }
            }, this, HandleException, JsTaskPriority.EXECUTION);
        }
    }
}
