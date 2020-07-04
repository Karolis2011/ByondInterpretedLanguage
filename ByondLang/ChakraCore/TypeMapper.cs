using ByondLang.ChakraCore.Embedding;
using ByondLang.ChakraCore.Hosting;
using ByondLang.ChakraCore.Hosting.Helpers;
using ByondLang.ChakraCore.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore
{
    public class TypeMapper : IDisposable
    {
		/// <summary>
		/// Name of property to store the external object
		/// </summary>
		private const string ExternalObjectPropertyName = "_cctm_externalObject";

		/// <summary>
		/// Storage for lazy-initialized embedded objects
		/// </summary>
		private ConcurrentDictionary<EmbeddedObjectKey, Lazy<EmbeddedObject>> _lazyEmbeddedObjects;

		/// <summary>
		/// Callback for finalization of embedded object
		/// </summary>
		private JsFinalizeCallback _embeddedObjectFinalizeCallback;

		/// <summary>
		/// Synchronizer of embedded object storage's initialization
		/// </summary>
		private readonly object _embeddedObjectStorageInitializationSynchronizer = new object();

		/// <summary>
		/// Flag indicating whether the embedded object storage is initialized
		/// </summary>
		private bool _embeddedObjectStorageInitialized;

		/// <summary>
		/// Flag indicating whether this object is disposed
		/// </summary>
		private readonly InterlockedStatedFlag _disposedFlag = new InterlockedStatedFlag();

		/// <summary>
		/// Makes a mapping of value from the host type to a script type
		/// </summary>
		/// <param name="value">The source value</param>
		/// <returns>The mapped value</returns>
		public JsValue MapToScriptType(object value)
		{

			if (value == null)
				return JsValue.Null;

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
					return value is JsValue ? (JsValue)value : GetOrCreateScriptObject(value);
			}
		}

		public JsValue MapToScriptType(Delegate del) => MapToScriptType((object)del);
		public JsValue MTS(Delegate del) => MapToScriptType((object)del);
		public JsValue MTS(object value) => MapToScriptType(value);

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
					result = null;
					break;
				case JsValueType.Undefined: // Undefined is not mapped
					result = value;
					break;
				case JsValueType.Boolean:
					result = value.ToBoolean();
					break;
				case JsValueType.Number:
					result = NumericHelpers.CastDoubleValueToCorrectType(value.ToDouble());
					break;
				case JsValueType.String:
					result = value.ToString();
					break;
				case JsValueType.Function:
					JsPropertyId externalObjectPropertyId = JsPropertyId.FromString(ExternalObjectPropertyName);
					if (value.HasProperty(externalObjectPropertyId))
					{
						JsValue externalObjectValue = value.GetProperty(externalObjectPropertyId);
						result = externalObjectValue.HasExternalData ?
							GCHandle.FromIntPtr(externalObjectValue.ExternalData).Target : null;
					}

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
		public object MTH(JsValue value) => MapToHostType(value);


		/// <summary>
		/// Creates a JavaScript value from an host object if the it does not already exist
		/// </summary>
		/// <param name="obj">Instance of host type</param>
		/// <returns>JavaScript value created from an host object</returns>
		public JsValue GetOrCreateScriptObject(object obj)
		{
			if (!_embeddedObjectStorageInitialized)
			{
				lock (_embeddedObjectStorageInitializationSynchronizer)
				{
					if (!_embeddedObjectStorageInitialized)
					{
						_lazyEmbeddedObjects = new ConcurrentDictionary<EmbeddedObjectKey, Lazy<EmbeddedObject>>();
						_embeddedObjectFinalizeCallback = EmbeddedObjectFinalizeCallback;

						_embeddedObjectStorageInitialized = true;
					}
				}
			}

			var embeddedObjectKey = new EmbeddedObjectKey(obj);
			EmbeddedObject embeddedObject = _lazyEmbeddedObjects.GetOrAdd(
				embeddedObjectKey,
				key => new Lazy<EmbeddedObject>(() => CreateEmbeddedObjectOrFunction(obj))
			).Value;

			return embeddedObject.ScriptValue;
		}

		private EmbeddedObject CreateEmbeddedObjectOrFunction(object obj)
		{
			var del = obj as Delegate;
			var delArray = obj as Delegate[];
			if(delArray != null)
				return CreateEmbeddedFunction(delArray);
			if(del != null)
				return CreateEmbeddedFunction(new[] { del });

			throw new NotImplementedException("Full object automatic mapping is not implemented.");
		}

		private EmbeddedObject CreateEmbeddedFunction(Delegate[] del)
		{
			JsNativeFunction nativeFunction = (callee, isConstructCall, args, argCount, callbackData) =>
			{
				if (!SelectAndProcessFunction(del, args, argCount, out Delegate selection, out object[] processedArgs))
				{
					CreateAndSetError($"Failed to find appropriate method with {argCount} paramters.");
				};
				
				object result;

				try
				{
					result = selection.DynamicInvoke(processedArgs);
				}
				catch (Exception e)
				{
					JsValue undefinedValue = JsValue.Undefined;
					Exception exception = UnwrapException(e);
					var wrapperException = exception as JsException;
					JsValue errorValue = wrapperException != null ?
						CreateErrorFromWrapperException(wrapperException)
						:
						JsValue.CreateError(JsValue.FromString($"Host delegate invocation error: {exception.Message}"));
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

		private bool SelectAndProcessFunction(Delegate[] funs, JsValue[] args, ushort argscount, out Delegate selection, out object[] processedArgs)
		{
			selection = null;
			processedArgs = null;
			var argTypes = args.Select(v => v.ValueType).ToArray();
			var candidite = funs.Select(d =>
			{
				var mi = d.GetMethodInfo();
				var pi = mi.GetParameters();
				var isc = IsCompatibleSignature(argTypes, pi, out ParameterType[] pt);
				return new { del = d, methodInfo = mi, compatible = isc, types = pt, paramInfo = pi};
			}).Where(d => d.compatible).OrderByDescending(d => d.types.Length).FirstOrDefault();
			if (candidite == null)
				return false;

			selection = candidite.del;
			processedArgs = new object[candidite.paramInfo.Length];
			int aPos = 1;
			for (int i = 0; i < processedArgs.Length; i++)
			{
				var pt = candidite.types[i];
				switch (pt)
				{
					case ParameterType.Direct:
						processedArgs[i] = args[aPos];
						aPos++;
						break;
					case ParameterType.InjectMeta:
						var pi = candidite.paramInfo[i];
						if (pi.ParameterType == typeof(JsRuntime))
							processedArgs[i] = JsContext.Current.Runtime;
						else if (pi.ParameterType == typeof(JsContext))
							processedArgs[i] = JsContext.Current;
						else if (pi.ParameterType == typeof(TypeMapper))
							processedArgs[i] = this;
						break;
					case ParameterType.Convert:
						processedArgs[i] = MapToHostType(args[aPos]);
						Type targetType = candidite.paramInfo[i].ParameterType;
						Type currentType = processedArgs[i].GetType();
						if(targetType != currentType)
						{
							if (TypeConverter.TryConvertToType(processedArgs[i], targetType, out object convertedValue))
							{
								processedArgs[i] = convertedValue;
							}
						}
						aPos++;
						break;
					case ParameterType.GLOB:
						processedArgs[i] = JsValue.GlobalObject;
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
				if (param.ParameterType == typeof(JsContext) || param.ParameterType == typeof(JsRuntime) || param.ParameterType == typeof(TypeMapper))
				{
					parameterTypes[i] = ParameterType.InjectMeta;
					continue;
				}
				if (param.ParameterType == typeof(JsValue))
				{
					if (param.GetCustomAttribute<GlobalStateAttribute>() != null)
					{
						parameterTypes[i] = ParameterType.GLOB;
						continue;
					}
					else if (param.GetCustomAttribute<ThisAttribute>() != null)
					{
						parameterTypes[i] = ParameterType.This;
						continue;
					}
					else
					{
						// We ran out of arguments
						if (args.Length <= argPos && !param.IsOptional)
							return false;
						parameterTypes[i] = ParameterType.Direct;
						argPos++;
						continue;
					}
				}
				TypeCode typeCode = Type.GetTypeCode(param.ParameterType);
				if (AreTypesCompatible(args[argPos], param.ParameterType))
				{
					// We ran out of arguments
					if (args.Length <= argPos && !param.IsOptional)
						return false;
					parameterTypes[i] = ParameterType.Convert;
					argPos++;
				} else
				{
					return false;
				}
			}
			return true;
		}

		public static bool AreTypesCompatible(JsValueType jsType, Type type)
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
						case JsValueType.Null:
							return type.IsAssignableFrom(typeof(object)) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
						case JsValueType.Object:
						case JsValueType.Error:
						case JsValueType.Array:
						case JsValueType.Symbol:
						case JsValueType.ArrayBuffer:
						case JsValueType.TypedArray:
						case JsValueType.DataView:
							return true;
						default:
							return false;
					}
			}
		}

		private static Exception UnwrapException(Exception exception)
		{
			Exception originalException = exception;
			var targetInvocationException = exception as TargetInvocationException;

			if (targetInvocationException != null)
			{
				Exception innerException = targetInvocationException.InnerException;
				if (innerException != null)
				{
					originalException = innerException;
				}
			}

			return originalException;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SetNonEnumerableProperty(JsValue objValue, string name, JsValue value)
		{
			JsValue descriptorValue = JsValue.CreateObject();
			descriptorValue.SetProperty("enumerable", JsValue.False, true);
			descriptorValue.SetProperty("writable", JsValue.True, true);

			JsPropertyId id = JsPropertyId.FromString(name);
			objValue.DefineProperty(id, descriptorValue);
			objValue.SetProperty(id, value, true);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CreateAndSetError(string message)
		{
			JsValue errorValue = JsValue.CreateError(JsValue.FromString(message));
			JsContext.SetException(errorValue);
		}

		private static JsValue CreateErrorFromWrapperException(JsException exception)
		{
			var originalException = exception.InnerException as JsException;
			JsErrorCode errorCode = originalException != null ?
				originalException.ErrorCode : JsErrorCode.NoError;
			var description = Enum.GetName(typeof(JsErrorCode), errorCode);

			JsValue innerErrorValue = JsValue.CreateError(JsValue.FromString(description));
			innerErrorValue.SetProperty("description", JsValue.FromString(description), true);

			/*

			JsValue metadataValue = JsValue.CreateObject();


			var scriptException = exception as JsScriptException;
			if (scriptException != null)
			{
				string type = scriptException.Type;
				string documentName = scriptException.DocumentName;
				int lineNumber = scriptException.LineNumber;
				if (lineNumber > 0)
				{
					lineNumber--;
				}
				int columnNumber = scriptException.ColumnNumber;
				if (columnNumber > 0)
				{
					columnNumber--;
				}
				string sourceFragment = scriptException.SourceFragment;

				innerErrorValue.SetProperty("name", JsValue.FromString(type), true);

				var runtimeException = scriptException as JsRuntimeException;
				if (runtimeException != null)
				{
					var errorNumber = (int)errorCode;
					string callStack = runtimeException.CallStack;
					string messageWithTypeAndCallStack = CoreErrorHelpers.GenerateScriptErrorMessage(type,
						description, callStack);

					innerErrorValue.SetProperty("number", JsValue.FromInt32(errorNumber), true);
					if (!string.IsNullOrWhiteSpace(callStack))
					{
						innerErrorValue.SetProperty("stack", JsValue.FromString(messageWithTypeAndCallStack), true);
					}
				}
				else
				{
					innerErrorValue.SetProperty("url", JsValue.FromString(documentName), true);
					innerErrorValue.SetProperty("line", JsValue.FromInt32(lineNumber), true);
					innerErrorValue.SetProperty("column", JsValue.FromInt32(columnNumber), true);
					innerErrorValue.SetProperty("source", JsValue.FromString(sourceFragment), true);
				}

				metadataValue.SetProperty("url", JsValue.FromString(documentName), true);
				metadataValue.SetProperty("line", JsValue.FromInt32(lineNumber), true);
				metadataValue.SetProperty("column", JsValue.FromInt32(columnNumber), true);
				metadataValue.SetProperty("source", JsValue.FromString(sourceFragment), true);
			}

			innerErrorValue.SetProperty("metadata", metadataValue, true);
			*/
			JsValue errorValue = JsValue.CreateError(JsValue.FromString(description));
			errorValue.SetProperty("innerException", innerErrorValue, true);

			return errorValue;
		}

		private void EmbeddedObjectFinalizeCallback(IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
			{
				return;
			}

			GCHandle objHandle = GCHandle.FromIntPtr(ptr);
			object obj = objHandle.Target;
			var lazyEmbeddedObjects = _lazyEmbeddedObjects;

			if (obj != null && lazyEmbeddedObjects != null)
			{
				var embeddedObjectKey = new EmbeddedObjectKey(obj);
				Lazy<EmbeddedObject> lazyEmbeddedObject;

				if (lazyEmbeddedObjects.TryRemove(embeddedObjectKey, out lazyEmbeddedObject))
				{
					lazyEmbeddedObject.Value?.Dispose();
				}
			}

			objHandle.Free();
		}

		public void Dispose()
		{
			if (_disposedFlag.Set())
			{
				var lazyEmbeddedObjects = _lazyEmbeddedObjects;
				if (lazyEmbeddedObjects != null)
				{
					if (lazyEmbeddedObjects.Count > 0)
					{
						foreach (EmbeddedObjectKey key in lazyEmbeddedObjects.Keys)
						{
							Lazy<EmbeddedObject> lazyEmbeddedObject;

							if (lazyEmbeddedObjects.TryGetValue(key, out lazyEmbeddedObject))
							{
								lazyEmbeddedObject.Value?.Dispose();
							}
						}

						lazyEmbeddedObjects.Clear();
					}

					_lazyEmbeddedObjects = null;
				}

				_embeddedObjectFinalizeCallback = null;
			}
		}

		enum ParameterType
		{
			Direct, // Taken from args
			InjectMeta, 
			Convert, // Taken from args
			GLOB,
			This // First arg
		}
	}
}
