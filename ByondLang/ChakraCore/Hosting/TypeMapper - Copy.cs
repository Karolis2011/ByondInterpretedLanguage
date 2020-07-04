using ByondLang.ChakraCore.Hosting.Embedding;
using ByondLang.ChakraCore.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ByondLang.ChakraCore.Hosting
{
	/// <summary>
	/// Type mapper
	/// </summary>
	internal sealed class TypeMapper
	{

		/// <summary>
		/// Flag indicating whether this object is disposed
		/// </summary>
		private readonly InterlockedStatedFlag _disposedFlag = new InterlockedStatedFlag();


		/// <summary>
		/// Constructs an instance of type mapper
		/// </summary>
		public TypeMapper()
		{ }

		/// <summary>
		/// Makes a mapping of value from the host type to a script type
		/// </summary>
		/// <param name="value">The source value</param>
		/// <returns>The mapped value</returns>
		public JsValue MapToScriptType(object value)
		{

			if (value == null)
			{
				return JsValue.Null;
			}

			TypeCode typeCode = Type.GetTypeCode(value.GetType());

			switch (typeCode)
			{
				case TypeCode.Boolean:
					return (bool)value ? JsValue.True : JsValue.False;

				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return JsValue.FromInt32(Convert.ToInt32(value));

				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return JsValue.FromDouble(Convert.ToDouble(value));

				case TypeCode.Char:
				case TypeCode.String:
					return JsValue.FromString((string)value);

				default:
					if (!(value is JsValue))
						throw new ArgumentException("Unsupported type.");
					return (JsValue)value;
			}
		}

		/// <summary>
		/// Makes a mapping of value from the script type to a host type
		/// </summary>
		/// <param name="value">The source value</param>
		/// <returns>The mapped value</returns>
		public object MapToHostType(JsValue value)
		{
			JsValueType valueType = value.ValueType;
			object result = null;

			switch (valueType)
			{
				case JsValueType.Null:
				case JsValueType.Undefined:
					result = null;
					break;
				case JsValueType.Boolean:
					result = value.ToBoolean();
					break;
				case JsValueType.Number:
					result = CastDoubleValueToCorrectType(value.ToDouble());
					break;
				case JsValueType.String:
					result = value.ToString();
					break;
				case JsValueType.Function:
					//JsPropertyId externalObjectPropertyId = JsPropertyId.FromString(ExternalObjectPropertyName);
					//if (value.HasProperty(externalObjectPropertyId))
					//{
					//	JsValue externalObjectValue = value.GetProperty(externalObjectPropertyId);
					//	result = externalObjectValue.HasExternalData ?
					//		GCHandle.FromIntPtr(externalObjectValue.ExternalData).Target : null;
					//}

					result = result ?? value.ConvertToObject();
					break;
				case JsValueType.Object:
				case JsValueType.Error:
				case JsValueType.Array:
				case JsValueType.Symbol:
				case JsValueType.ArrayBuffer:
				case JsValueType.TypedArray:
				case JsValueType.DataView:
					result = value.HasExternalData ?
						GCHandle.FromIntPtr(value.ExternalData).Target : value.ConvertToObject();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return result;
		}

		[MethodImpl((MethodImplOptions)256 /* AggressiveInlining */)]
		private static void FreezeObject(JsValue objValue)
		{
			JsValue freezeMethodValue = JsValue.GlobalObject
				.GetProperty("Object")
				.GetProperty("freeze")
				;
			freezeMethodValue.CallFunction(objValue);
		}

		[MethodImpl((MethodImplOptions)256 /* AggressiveInlining */)]
		private static void SetNonEnumerableProperty(JsValue objValue, string name, JsValue value)
		{
			JsValue descriptorValue = JsValue.CreateObject();
			descriptorValue.SetProperty("enumerable", JsValue.False, true);
			descriptorValue.SetProperty("writable", JsValue.True, true);

			JsPropertyId id = JsPropertyId.FromString(name);
			objValue.DefineProperty(id, descriptorValue);
			objValue.SetProperty(id, value, true);
		}

		[MethodImpl((MethodImplOptions)256 /* AggressiveInlining */)]
		private static void CreateAndSetError(string message)
		{
			JsValue errorValue = JsValue.CreateError(JsValue.FromString(message));
			JsContext.SetException(errorValue);
		}

		[MethodImpl((MethodImplOptions)256 /* AggressiveInlining */)]
		private static void CreateAndSetReferenceError(string message)
		{
			JsValue errorValue = JsValue.CreateReferenceError(JsValue.FromString(message));
			JsContext.SetException(errorValue);
		}

		[MethodImpl((MethodImplOptions)256 /* AggressiveInlining */)]
		private static void CreateAndSetTypeError(string message)
		{
			JsValue errorValue = JsValue.CreateTypeError(JsValue.FromString(message));
			JsContext.SetException(errorValue);
		}

		private const double MAX_INTEGER_IN_DOUBLE = (1L << 53) - 1;

		public static object CastDoubleValueToCorrectType(double value)
		{
			if (Math.Round(value) == value)
			{
				if (Math.Abs(value) <= MAX_INTEGER_IN_DOUBLE)
				{
					long longValue = Convert.ToInt64(value);
					if (longValue >= int.MinValue && longValue <= int.MaxValue)
					{
						return (int)longValue;
					}

					return longValue;
				}
			}
			else
			{
				float floatValue = Convert.ToSingle(value);
				if (value == floatValue)
				{
					return floatValue;
				}
			}

			return value;
		}

		[MethodImpl((MethodImplOptions)256 /* AggressiveInlining */)]
		private EmbeddedObject CreateEmbeddedFunction(Delegate del)
		{
			JsNativeFunction nativeFunction = (callee, isConstructCall, args, argCount, callbackData) =>
			{
				MethodInfo method = del.GetMethodInfo();

				ParameterInfo[] parameters = method.GetParameters();

				object[] processedArgs = new object[parameters.Length];

				int procI = 1;

				for (int i = 0; i < parameters.Length; i++)
				{
					var paramInfo = parameters[i];
					var targetType = paramInfo.ParameterType;
					if (targetType == typeof(JsValue))
					{
						if (paramInfo.GetCustomAttribute<ThisAttribute>() != null)
						{
							processedArgs[i] = args[0];
						}
						else if (paramInfo.GetCustomAttribute<GlobalStateAttribute>() != null)
						{
							processedArgs[i] = JsValue.GlobalObject;
						}
						else
						{
							if (procI >= argCount)
								break;
							processedArgs[i] = args[procI];
							procI++;
						}
					}
					else {
						processedArgs[i] = (targetType)MapToHostType(args[procI]);
					}
				}

				object result;

				try
				{
					result = del.DynamicInvoke(processedArgs);
				}
				catch (Exception e)
				{
					JsValue undefinedValue = JsValue.Undefined;
					Exception exception = UnwrapException(e);
					var wrapperException = exception as WrapperException;
					JsValue errorValue = wrapperException != null ?
						CreateErrorFromWrapperException(wrapperException)
						:
						JsErrorHelpers.CreateError(string.Format(
							Strings.Runtime_HostDelegateInvocationFailed, exception.Message))
						;
					JsContext.SetException(errorValue);

					return undefinedValue;
				}

				JsValue resultValue = MapToScriptType(result);

				return resultValue;
			};

			GCHandle delHandle = GCHandle.Alloc(del);
			IntPtr delPtr = GCHandle.ToIntPtr(delHandle);
			JsValue objValue = JsValue.CreateExternalObject(delPtr, _embeddedObjectFinalizeCallback);

			JsValue functionValue = JsValue.CreateFunction(nativeFunction);
			SetNonEnumerableProperty(functionValue, ExternalObjectPropertyName, objValue);

			var embeddedObject = new EmbeddedObject(del, functionValue,
				new List<JsNativeFunction> { nativeFunction });

			return embeddedObject;
		}
	}
}