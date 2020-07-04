using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
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
        private ChakraCore.TypeMapper typeMapper = new ChakraCore.TypeMapper();
        private RuntimeTaskScheduler scheduler = new RuntimeTaskScheduler();
        private TaskFactory factory;
        private List<BaseProgram> programs = new List<BaseProgram>();

        public Runtime()
        {
            factory = new TaskFactory(scheduler);
            runtime = JsRuntime.Create(JsRuntimeAttributes.AllowScriptInterrupt);
        }

        public T BuildContext<T>(Func<Runtime, JsContext, ChakraCore.TypeMapper, T> initializer) where T : BaseProgram
        {
            var context = runtime.CreateContext();
            using (new JsContext.Scope(context))
            {
                // TODO: Configure promise callback. Promises should be added to be executed along main queue of work.
                // JsContext.SetPromiseContinuationCallback();
            }
            var program = initializer(this, context, typeMapper);
            programs.Add(program);
            return program;
        }

        public void RemoveContext(BaseProgram context)
        {
            programs.Remove(context);
        }

        /*
        private void PromiseContinuationCallback(JsValue task, IntPtr callbackState)
        {
            task.AddRef();
            JsContext context = JsContext.Current;
            TimedFunction(DEFAULT_PROMISE_TIMEOUT, () =>
            {
                using (new JsContext.Scope(context))
                {

                }
            });
        }*/

        private void TimedFn(int timeout, Action timedAction)
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
                catch (JsScriptException ex)
                {
                    if (ex.ErrorCode != JsErrorCode.ScriptTerminated)
                        throw;
                }
            }
            runtime.Disabled = false;
        }

        private R TimedFn<R>(int timeout, Func<R> timedAction)
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
                catch (JsScriptException ex)
                {
                    if (ex.ErrorCode != JsErrorCode.ScriptTerminated)
                        throw;
                }
            }
            runtime.Disabled = false;
            return result;
        }

        public Task TimedFunction(int timeout, Action function) => factory.StartNew(() => TimedFn(timeout, function));
        public Task TimedFunction(Action function) => TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function);
        public Task Function(Action function) => factory.StartNew(function);
        public Task<TResult> TimedFunction<TResult>(int timeout, Func<TResult> function) => factory.StartNew(() => TimedFn(timeout, function));
        public Task<TResult> TimedFunction<TResult>(Func<TResult> function) => TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function);
        public Task<TResult> Function<TResult>(Func<TResult> function) => factory.StartNew(function);

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
