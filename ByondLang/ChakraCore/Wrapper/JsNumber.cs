using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class JsNumber : JsValue
    {
		public static JsNumber FromNumber(object value)
		{
			TypeCode typeCode = Type.GetTypeCode(value.GetType());
			switch (typeCode)
			{
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return new JsNumber(JsValueRaw.FromInt32(Convert.ToInt32(value)));
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return new JsNumber(JsValueRaw.FromDouble(Convert.ToDouble(value)));
			}
			throw new Exception("Invalid value type, exptected a number.");
		}

        public JsNumber(JsValueRaw jsValue)
        {
            if (jsValue.ValueType != JsValueType.Number)
                throw new Exception("Invalid JsValue type.");

            this.jsValue = jsValue;
            jsValue.AddRef(); // Remeber that we use this.
        }

		public static implicit operator int(JsNumber number) => number.jsValue.ToInt32();
		public static implicit operator long(JsNumber number) => Convert.ToInt64(number.jsValue.ToInt32());
		public static implicit operator short(JsNumber number) => Convert.ToInt16(number.jsValue.ToInt32());
		public static implicit operator byte(JsNumber number) => Convert.ToByte(number.jsValue.ToInt32());
		public static implicit operator double(JsNumber number) => number.jsValue.ToDouble();
		public static implicit operator decimal(JsNumber number) => Convert.ToDecimal(number.jsValue.ToDouble());
		public static implicit operator float(JsNumber number) => Convert.ToSingle(number.jsValue.ToDouble());

	}
}
