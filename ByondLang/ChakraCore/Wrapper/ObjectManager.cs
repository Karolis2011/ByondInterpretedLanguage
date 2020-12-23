using ByondLang.ChakraCore.Hosting;
using ByondLang.ChakraCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Wrapper
{
    public class ObjectManager
    {

		public object MapToHostType(JsValueRaw value, Type target)
		{
			JsValueType valueType = value.ValueType;
			object result = null;

			switch (valueType)
			{
				case JsValueType.Null:
					result = null;
					break;
				case JsValueType.Undefined: // Undefined is not mapped
					result = value;
					break;
				case JsValueType.Boolean:
					result = value.ToBoolean();
					break;
				case JsValueType.Number:
					TypeCode typeCode = Type.GetTypeCode(target);
					switch (typeCode)
                    {
						case TypeCode.Boolean:
							result = value.ToInt32() == 0 ? false : true;
							break;
						case TypeCode.SByte:
						case TypeCode.Byte:
						case TypeCode.Int16:
						case TypeCode.UInt16:
						case TypeCode.Int32:
						case TypeCode.UInt32:
						case TypeCode.Int64:
						case TypeCode.UInt64:
							result = value.ToInt32();
							break;
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							result = value.ToDouble();
							break;
					}
					break;
				case JsValueType.String:
					result = value.ToString();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return result;
		}

		private bool ProcessFunction(ParameterInfo[] parameterInfos, ParameterType[] parameterTypes, JsValueRaw[] args, out object[] processedArgs)
		{
			processedArgs = new object[parameterInfos.Length];
			int aPos = 1;
			for (int i = 0; i < processedArgs.Length; i++)
			{
				var pt = parameterTypes[i];
				switch (pt)
				{
					case ParameterType.Direct:
						processedArgs[i] = args[aPos];
						aPos++;
						break;
					case ParameterType.Warp:
						processedArgs[i] = JsValue.FromRaw(args[aPos]);
						aPos++;
						break;
					case ParameterType.InjectMeta:
						var pi = parameterInfos[i];
						if (pi.ParameterType == typeof(JsRuntime))
							processedArgs[i] = JsContext.Current.Runtime;
						else if (pi.ParameterType == typeof(JsContext))
							processedArgs[i] = JsContext.Current;
						else if (pi.ParameterType == typeof(ObjectManager))
							processedArgs[i] = this;
						else if (pi.ParameterType == typeof(JsGlobalObject))
							processedArgs[i] = new JsGlobalObject();
						break;
					case ParameterType.Convert:
						Type targetType = parameterInfos[i].ParameterType;
						processedArgs[i] = MapToHostType(args[aPos], targetType);
						aPos++;
						break;
					case ParameterType.This:
						processedArgs[i] = args[0];
						break;
					default:
						break;
				}
			}
			return true;
		}

		/// <summary>
		/// Determines if passed signature is compatible and how is it compatible
		/// </summary>
		/// <param name="args"></param>
		/// <param name="parameters"></param>
		/// <param name="parameterTypes"></param>
		/// <returns></returns>
		private static bool IsCompatibleSignature(JsValueType[] args, ParameterInfo[] parameters, out ParameterType[] parameterTypes)
		{
			parameterTypes = new ParameterType[parameters.Length];
			int argPos = 1;
			for (int i = 0; i < parameters.Length; i++)
			{
				var param = parameters[i];
				if (param.ParameterType == typeof(JsContext) || param.ParameterType == typeof(JsRuntime) || param.ParameterType == typeof(ObjectManager) || param.ParameterType == typeof(JsGlobalObject))
				{
					parameterTypes[i] = ParameterType.InjectMeta;
					continue;
				}
				if (param.ParameterType == typeof(JsValueRaw))
				{
					if (param.GetCustomAttribute<ThisAttribute>() != null)
					{
						if (!param.ParameterType.IsAssignableFrom(typeof(JsObject)))
							return false;
						parameterTypes[i] = ParameterType.This;
						continue;
					}
					else
					{
						// Did we ran out of arguments
						if (args.Length <= argPos && !param.IsOptional)
							return false;
						parameterTypes[i] = ParameterType.Direct;
						argPos++;
						continue;
					}
				}
				if (param.ParameterType.IsAssignableFrom(typeof(JsValue)))
                {
					if (param.GetCustomAttribute<ThisAttribute>() != null)
					{
						parameterTypes[i] = ParameterType.This;
						continue;
					}
					else
					{
						// Did we ran out of arguments
						if (args.Length <= argPos && !param.IsOptional)
							return false;
						parameterTypes[i] = ParameterType.Warp;
						argPos++;
						continue;
					}
				}
				// We ran out of arguments
				if (args.Length <= argPos && !param.IsOptional)
					return false;
				if (AreTypesConvertable(args[argPos], param.ParameterType))
				{
					parameterTypes[i] = ParameterType.Convert;
					argPos++;
				}
				else
				{
					return false;
				}
			}
			return true;
		}

		public static bool AreTypesConvertable(JsValueType jsType, Type type)
		{
			TypeCode typeCode = Type.GetTypeCode(type);
			switch (typeCode)
			{
				case TypeCode.Boolean:
					return jsType == JsValueType.Boolean || jsType == JsValueType.Number;
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return jsType == JsValueType.Number;
				case TypeCode.Char:
				case TypeCode.String:
					return jsType == JsValueType.String;
				default:
					switch (jsType)
					{
						case JsValueType.Undefined:
							return false; // We can't convert
						case JsValueType.Null:
							return type.IsAssignableFrom(typeof(object)) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
						default:
							return false;
					}
			}
		}

		enum ParameterType
		{
			Direct, // Taken from args
			Warp, // Taken from args
			InjectMeta,
			Convert, // Taken from args
			This // First arg
		}
	}
}
