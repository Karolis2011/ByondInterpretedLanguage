using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class JsCallback : IDisposable
    {
        public JsValue CallbackFunction;
        public BaseProgram program;
        public string Id;
        protected bool disposedValue;

        public JsCallback(string id, JsValue callback, BaseProgram program)
        {
            CallbackFunction = callback;
            Id = id;
            callback.AddRef();
            this.program = program;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                program.Function(() =>
                {
                    CallbackFunction.Release();
                }, ChakraCore.JsTaskPriority.INITIALIZATION);

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~JsCallback()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
