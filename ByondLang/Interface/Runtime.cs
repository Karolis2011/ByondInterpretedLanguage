﻿using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using ByondLang.Services;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class Runtime : IDisposable
    {
        public const int DEFAULT_SCRIPT_TIMEOUT = 2000;
        public const int DEFAULT_PROMISE_TIMEOUT = 2000;
        private JsRuntime runtime;
        private TypeMapper typeMapper = new TypeMapper();
        private JsPFIFOScheduler scheduler = new JsPFIFOScheduler();
        private List<BaseProgram> programs = new List<BaseProgram>();
        private Dictionary<string, JsValueRaw> callbacks = new Dictionary<string, JsValueRaw>();
        public readonly NTSL3Service Service;

        public Runtime(NTSL3Service service)
        {
            runtime = JsRuntime.Create(JsRuntimeAttributes.AllowScriptInterrupt);
            Service = service;
        }

        public async Task<T> BuildContext<T>(Func<Runtime, JsContext, TypeMapper, T> initializer) where T : BaseProgram
        {
            var buildTask = Function(() =>
            {
                var context = runtime.CreateContext();
                using (new JsContext.Scope(context))
                {
                    // TODO: Configure promise callback. Promises should be added to be executed along main queue of work.
                    JsContext.SetPromiseContinuationCallback(PromiseContinuationCallback, IntPtr.Zero);
                }
                return context;
            }, priority: JsTaskPriority.INITIALIZATION);
            var context = await buildTask;
            var program = initializer(this, context, typeMapper);
            program.InitializeState();
            programs.Add(program);
            return program;
        }

        public void RemoveContext(BaseProgram context)
        {
            programs.Remove(context);
        }

        private void PromiseContinuationCallback(JsValueRaw task, IntPtr callbackState)
        {
            task.AddRef();
            JsContext context = JsContext.Current;
            TimedFunction(DEFAULT_PROMISE_TIMEOUT, () =>
            {
                using (new JsContext.Scope(context))
                {
                    task.CallFunction(JsValueRaw.GlobalObject);
                }
                task.AddRef();
            }, priority: JsTaskPriority.PROMISE);
        }

        private void TimedFn(int timeout, Action timedAction, Func<Exception, bool> exHandler)
        {
            using (var timer = new Timer(state =>
            {
                runtime.Disabled = true;
            }, null, timeout, Timeout.Infinite))
            {
                try
                {
                    timedAction();
                }
                //catch (JsScriptException ex)
                //{
                //    if (ex.ErrorCode != JsErrorCode.ScriptTerminated)
                //        if (exHandler != null)
                //            exHandler(ex);
                //        else
                //            throw;
                //}
                catch (Exception ex)
                {
                    if (exHandler == null || !exHandler(ex))
                        throw;
                }
            }
            runtime.Disabled = false;
        }

        private R TimedFn<R>(int timeout, Func<R> timedAction, Func<Exception, bool> exHandler)
        {
            R result = default;
            using (var timer = new Timer(state =>
            {
                runtime.Disabled = true;
            }, null, timeout, Timeout.Infinite))
            {
                try
                {
                    result = timedAction();
                }
                //catch (JsScriptException ex)
                //{
                //    if (ex.ErrorCode != JsErrorCode.ScriptTerminated)
                //        if (exHandler != null)
                //            exHandler(ex);
                //        else
                //            throw;
                //}
                catch (Exception ex)
                {
                    if (exHandler == null || !exHandler(ex))
                        throw;
                }

            }
            runtime.Disabled = false;
            return result;
        }


        public JsTask TimedFunction(int timeout, Action function, BaseProgram program = null, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            scheduler.Run(() => TimedFn(timeout, function, exHandler), program, priority);

        public JsTask TimedFunction(Action function, BaseProgram program = null, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.EXECUTION) => 
            TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function, program, exHandler, priority);

        public JsTask Function(Action function, BaseProgram program = null, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            scheduler.Run(function, program, priority);

        public JsTask<TResult> TimedFunction<TResult>(int timeout, Func<TResult> function, BaseProgram program = null, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            scheduler.Run(() => TimedFn(timeout, function, exHandler), program, priority);

        public JsTask<TResult> TimedFunction<TResult>(Func<TResult> function, BaseProgram program = null, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.EXECUTION) => 
            TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function, program, exHandler, priority);

        public JsTask<TResult> Function<TResult>(Func<TResult> function, BaseProgram program = null, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            scheduler.Run(function, program, priority);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    typeMapper.Dispose();
                }
                var _programToDispose = programs.ToList();
                foreach (var program in _programToDispose)
                {
                    program.Dispose();
                }
                runtime.Dispose();

                disposedValue = true;
            }
        }

         ~Runtime()
        {
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
