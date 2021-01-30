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
        public const int CALLBACK_HASH_LEN = 12;
        private static Random random = new Random();
        protected Runtime _runtime;
        protected JsContext _context;
        protected TypeMapper _typeMapper;
        protected Task lastExecutionTask;
        protected Dictionary<string, WeakReference<JsCallback>> callbacks = new Dictionary<string, WeakReference<JsCallback>>();
        private ILogger<BaseProgram> logger;

        public BaseProgram(Runtime runtime, JsContext context, TypeMapper typeMapper)
        {
            context.AddRef();
            _runtime = runtime;
            _context = context;
            _typeMapper = typeMapper;
            logger = runtime.serviceProvider?.GetService<ILogger<BaseProgram>>();
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
            callbacks.Clear();
            _context.Release();
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
            glob.SetProperty("btoa", _typeMapper.MTS((Func<JsValue, string>)delegate (JsValue value)
            {
                if(value.ValueType != JsValueType.String)
                {
                    value = value.ConvertToString();
                }
                var plainTextBytes = Encoding.UTF8.GetBytes(value.ToString());
                return Convert.ToBase64String(plainTextBytes);
            }), false);
            glob.SetProperty("atob", _typeMapper.MTS((Func<JsValue, string>)delegate (JsValue value)
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
