using ByondLang.ChakraCore.Hosting;
using System;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsValue : IDisposable
    {
        internal JsValueRaw jsValue;
        private bool disposedValue;

        public static bool isSupported(JsValueType type) => true;

        public static JsValue FromRaw(JsValueRaw jsValue)
        {
            if (!jsValue.IsValid)
                throw new Exception("Indalid value");
            var type = jsValue.ValueType;
            switch (type)
            {
                case JsValueType.Undefined:
                    return new JsUndefined(jsValue);
                case JsValueType.Null:
                    return new JsValue(jsValue);
                case JsValueType.Number:
                    return new JsNumber(jsValue);
                case JsValueType.String:
                    break;
                case JsValueType.Boolean:
                    break;
                case JsValueType.Object:
                    return new JsObject(jsValue);
                case JsValueType.Function:
                    return new JsFunction(jsValue);
                case JsValueType.Error:
                    break;
                case JsValueType.Array:
                    return new JsArray(jsValue);
                case JsValueType.Symbol:
                    break;
                case JsValueType.ArrayBuffer:
                    break;
                case JsValueType.TypedArray:
                    break;
                case JsValueType.DataView:
                    break;
                default:
                    break;
            }
            throw new NotImplementedException("Unsupported value");
        }

        protected JsValue()
        {

        }

        public JsValue(JsValueRaw jsValue)
        {
            if (!jsValue.IsValid)
                throw new Exception("Indalid value");
            this.jsValue = jsValue;
            jsValue.AddRef(); // Mark as used
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                if(jsValue.IsValid)
                    jsValue.Release();

                // set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~JsValue()
        {
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public static implicit operator JsValueRaw(JsValue value) => value.jsValue;
    }
}
