using ByondLang.ChakraCore;
using ByondLang.ChakraCore.Hosting;
using ByondLang.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class BaseProgram : IDisposable
    {
        public const int DEFAULT_SCRIPT_TIMEOUT = 2000;
        public const int DEFAULT_PROMISE_TIMEOUT = 2000;
        public const int CALLBACK_HASH_LEN = 12;
        public const int EVENT_HASH_LEN = 12;

        private static Random random = new Random();

        protected JsRuntime runtime;
        protected JsContext context;

        protected TypeMapper typeMapper = new TypeMapper();
        protected JsPFIFOScheduler scheduler = new JsPFIFOScheduler();

        protected Task lastExecutionTask;
        protected Dictionary<string, WeakReference<JsCallback>> callbacks = new Dictionary<string, WeakReference<JsCallback>>();

        protected ILogger<BaseProgram> logger;

        protected ConcurrentDictionary<string, Event> pendingEvents = new ConcurrentDictionary<string, Event>();
        protected ConcurrentDictionary<string, Event> unresolvedEvents = new ConcurrentDictionary<string, Event>();

        private bool isDebugging = false;
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

        private void DebugEventCallback(JsDiagDebugEvent debugEvent, JsValue eventData, IntPtr callbackState)
        {
            IsInBreak = true;
            var glob = JsValue.GlobalObject;
            var JSON_Stringify = glob.GetProperty("JSON").GetProperty("stringify");

            var eventDataString = JSON_Stringify.CallFunction(glob, eventData);

            string data = eventDataString.ToString();
            Console.WriteLine(data);

            scheduler.EnterBreakState();

            IsInBreak = false;
        }

        internal IEnumerable<Event> GetEvents()
        {
            var events = new List<Event>();
            foreach (var item in pendingEvents)
            {
                events.Add(item.Value);
                if(item.Value.NeedsToBeResolved)
                    unresolvedEvents[item.Key] = item.Value;
            }
            return events;
        }

        internal void SetDebuggingState(bool enabled)
        {
            if (!isDebugging && enabled)
            {
                Function(() =>
                {
                    runtime.StartDebugging(DebugEventCallback, IntPtr.Zero);
                }, JsTaskPriority.INITIALIZATION);
            }
            else if (isDebugging && !enabled)
            {
                Function(() =>
                {
                    runtime.StopDebugging();
                }, JsTaskPriority.INITIALIZATION);
            }
        }

        internal JsCallback RegisterCallback(JsValue callback)
        {
            var hash = GenerateCallbackHash();
            var call = new JsCallback(hash, callback, this);
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

        private string GenerateEventHash()
        {
            string characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            StringBuilder result = new StringBuilder(EVENT_HASH_LEN);
            for (int i = 0; i < EVENT_HASH_LEN; i++)
            {
                result.Append(characters[random.Next(characters.Length)]);
            }
            var finalResult = result.ToString();
            if (pendingEvents.ContainsKey(finalResult) || unresolvedEvents.ContainsKey(finalResult))
                return GenerateEventHash();
            return finalResult;
        }

        public virtual void InstallInterfaces()
        {
            // Add generic global APIs accessible from everywhere
            var glob = JsValue.GlobalObject;
            glob.SetProperty("btoa", typeMapper.MTS((Func<JsValue, string>)delegate (JsValue value)
            {
                if (value.ValueType != JsValueType.String)
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
                if (IsInBreak)
                    scheduler.ExitBreakState();

                if (disposing)
                {
                    callbacks.Clear();
                    typeMapper.Dispose();
                }
                scheduler.Dispose();
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

        #region Function wrappers base
        private void FnReEx(Action action, Func<Exception, bool> exHandler)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (exHandler == null || !exHandler(ex))
                    throw;
            }
            runtime.Disabled = false;
        }
        private R FnReEx<R>(Func<R> timedAction, Func<Exception, bool> exHandler)
        {
            R result = default;
            try
            {
                result = timedAction();
            }
            catch (Exception ex)
            {
                if (exHandler == null || !exHandler(ex))
                    throw;
            }
            runtime.Disabled = false;
            return result;
        }
        #endregion

        #region Function via sheduler implemetations
        public JsTask TimedFunction(int timeout, Action function, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            scheduler.RunTimed(() => FnReEx(function, exHandler), () => runtime.Disabled = true, TimeSpan.FromMilliseconds(timeout), priority);

        public JsTask TimedFunction(Action function, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function, exHandler, priority);

        public JsTask Function(Action function, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            scheduler.Run(function, priority);

        public JsTask<TResult> TimedFunction<TResult>(int timeout, Func<TResult> function, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.LOWEST) =>
            scheduler.RunTimed(() => FnReEx(function, exHandler), () => runtime.Disabled = true, TimeSpan.FromMilliseconds(timeout), priority);

        public JsTask<TResult> TimedFunction<TResult>(Func<TResult> function, Func<Exception, bool> exHandler = null, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            TimedFunction(DEFAULT_SCRIPT_TIMEOUT, function, exHandler, priority);

        public JsTask<TResult> Function<TResult>(Func<TResult> function, JsTaskPriority priority = JsTaskPriority.EXECUTION) =>
            scheduler.Run(function, priority);


        #endregion
    }
}
