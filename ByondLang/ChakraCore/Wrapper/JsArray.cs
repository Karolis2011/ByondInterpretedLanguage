using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsArray : JsValue
    {
        public static new bool isSupported(JsValueType type) => type == JsValueType.Array;

        public JsArray(uint length)
        {
            jsValue = JsValueRaw.CreateArray(length);
            jsValue.AddRef(); // Remeber that we use this.
        }

        public JsArray(JsValueRaw jsValue)
        {
            if (jsValue.ValueType != JsValueType.Array)
                throw new Exception("Invalid JsValue type");

            this.jsValue = jsValue;
            jsValue.AddRef(); // Remeber that we use this.
        }

        private JsValue this[JsValueRaw index]
        {
            get => FromRaw(jsValue.GetIndexedProperty(index));
            set => jsValue.SetIndexedProperty(index, value.jsValue);
        }

        public JsValue this[int index]
        {
            get => this[JsValueRaw.FromInt32(index)];
            set => this[JsValueRaw.FromInt32(index)] = value;
        }

        public JsValue this[JsValue index]
        {
            get => this[index.jsValue];
            set => this[index.jsValue] = value;
        }
    }
}
