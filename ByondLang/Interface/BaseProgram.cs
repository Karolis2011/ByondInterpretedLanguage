using ByondLang.ChakraCore;
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
        public const int DEFAULT_SCRIPT_TIMEOUT = 2000;
        public const int DEFAULT_PROMISE_TIMEOUT = 2000;
        public const int CALLBACK_HASH_LEN = 12;

        private static Random random = new Random();

        protected JsRuntime runtime;
        protected JsContext context;

        protected TypeMapper typeMapper = new TypeMapper();
        protected JsPFIFOScheduler scheduler = new JsPFIFOScheduler();

        protected Task lastExecutionTask;
        protected Dictionary<string, WeakReference<JsCallback>> callbacks = new Dictionary<string, WeakReference<JsCallback>>();
        private ILogger<BaseProgram> logger;

        public bool IsInBreak { get; protected set; }

        public BaseProgram(IServiceProvider serviceProvider)
        {
            runtime = JsRuntime.Create(JsRuntimeAttributes.AllowScriptInterrupt);
            
            logger = serviceProvider?.GetService<ILogger<BaseProgram>>();
        }

        public async Task InitializeState()
        {
            await Function(() =>
            {
                context = runtime.CreateContext();
                using (new JsContext.Scope(context))
                {
                    JsContext.SetPromiseContinuationCallback(PromiseContinuationCallback, IntPtr.Zero);
                    InstallInterfaces();
                    context.AddRef();
                }
            }, JsTaskPriority.INITIALIZATION);
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
                    task.Release();
                }
            }, priority: JsTaskPriority.PROMISE);
        }

        internal JsCallback RegisterCallback(JsValue callback)
        {
            var hash = GenerateCallbackHash();
            var call = new JsCallback(hash, callback);
            callbacks[hash] = new WeakReference<JsCallback>(call);
            return call;
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
            var glob = JsValue.GlobalObject;
            glob.SetProperty("btoa", typeMapper.MTS((Func<JsValue, string>)delegate (JsValue value)
            {
                if(value.ValueType != JsValueType.String)
                {
                    value = value.ConvertToString();
                }
                var plainTextBytes = Encoding.UTF8.GetBytes(value.ToString());
                return Convert.ToBase64String(plainTextBytes);
            }), false);
            glob.SetProperty("atob", typeMapper.MTS((Func<JsValue, string>)delegate (JsValue value)
            {
                if (value.ValueType != JsValueType.String)
                {
                    return "";
                }
                var plainTextBytes = Convert.FromBase64String(value.ToString());
                return Encoding.UTF8.GetString(plainTextBytes);
            }), false);
        }

        internal virtual bool HandleException(Exception exception)
        {
            logger?.LogError(exception, "Unhandled runtime exception.");
            return false;
        }

        public JsTask<JsValue> ExecuteScript(string script)
        {
            return TimedFunction(() =>
            {
                using (new JsContext.Scope(context))
                {
                    return JsContext.RunScript(script);
                }
            }, HandleException, JsTaskPriority.EXECUTION);
        }

        

        #region IDisposable support
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    callbacks.Clear();
                    typeMapper.Dispose();
                }
                context.Release();
                runtime.Dispose();

                disposedValue = true;
            }
        }

        ~BaseProgram()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Timed Function implemetation base
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
        #endregion
        #region Timed Function via sheduler implemetations
        public JsTask TimedFunction(int timeout, Action function, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            scheduler.Run(() => TimedFn(timeout, function, exHandler), priority);

        public JsTask TimedFunction(Action function, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function, exHandler, priority);

        public JsTask Function(Action function, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            scheduler.Run(function, priority);

        public JsTask<TResult> TimedFunction<TResult>(int timeout, Func<TResult> function, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            scheduler.Run(() => TimedFn(timeout, function, exHandler), priority);

        public JsTask<TResult> TimedFunction<TResult>(Func<TResult> function, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function, exHandler, priority);

        public JsTask<TResult> Function<TResult>(Func<TResult> function, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            scheduler.Run(function, priority);
        #endregion
    }
}
