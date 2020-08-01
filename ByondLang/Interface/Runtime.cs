using ByondLang.ChakraCore;
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
        private RuntimeTaskScheduler scheduler = new RuntimeTaskScheduler();
        private TaskFactory factory;
        private List<BaseProgram> programs = new List<BaseProgram>();
        private Dictionary<string, JsValue> callbacks = new Dictionary<string, JsValue>();
        public readonly NTSL3Service Service;

        public Runtime(NTSL3Service service)
        {
            factory = new TaskFactory(scheduler);
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
            });
            scheduler.PrioritizeTask(buildTask);
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

        private void PromiseContinuationCallback(JsValue task, IntPtr callbackState)
        {
            task.AddRef();
            JsContext context = JsContext.Current;
            TimedFunction(DEFAULT_PROMISE_TIMEOUT, () =>
            {
                using (new JsContext.Scope(context))
                {
                    task.CallFunction(JsValue.GlobalObject);
                }
                task.AddRef();
            });
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
                    if (exHandler != null)
                        exHandler(ex);
                    else
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
                    if (exHandler?.Invoke(ex) != true)
                        throw;
                }

            }
            runtime.Disabled = false;
            return result;
        }

        public Task TimedFunction(int timeout, Action function, Func<Exception, bool> exHandler = null) => factory.StartNew(() => TimedFn(timeout, function, exHandler));
        public Task TimedFunction(Action function, Func<Exception, bool> exHandler = null) => TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function, exHandler);
        public Task Function(Action function) => factory.StartNew(function);
        public Task<TResult> TimedFunction<TResult>(int timeout, Func<TResult> function, Func<Exception, bool> exHandler = null) => factory.StartNew(() => TimedFn(timeout, function, exHandler));
        public Task<TResult> TimedFunction<TResult>(Func<TResult> function, Func<Exception, bool> exHandler = null) => TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function, exHandler);
        public Task<TResult> Function<TResult>(Func<TResult> function, Func<Exception, bool> exHandler = null) => factory.StartNew(function);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    factory = null;
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
