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
	[Obsolete]
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
		public JsValueRaw MapToScriptType(object value)
		{

			if (value == null)
				return JsValueRaw.Null;

			TypeCode typeCode = Type.GetTypeCode(value.GetType());

			switch (typeCode)
			{
				case TypeCode.Boolean:
					return (bool)value ? JsValueRaw.True : JsValueRaw.False;

				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return JsValueRaw.FromInt32(Convert.ToInt32(value));

				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return JsValueRaw.FromDouble(Convert.ToDouble(value));

				case TypeCode.Char:
				case TypeCode.String:
					return JsValueRaw.FromString((string)value);

				default:
					if (value is JsValueRaw jsValue)
						return jsValue;
					else
						return GetOrCreateScriptObject(value);
			}
		}

		public JsValueRaw MapToScriptType(Delegate del) => MapToScriptType((object)del);
		public JsValueRaw MTS(Delegate del) => MapToScriptType((object)del);
		public JsValueRaw MTS(object value) => MapToScriptType(value);

		/// <summary>
		/// Makes a mapping of value from the script type to a host type
		/// </summary>
		/// <param name="value">The source value</param>
		/// <returns>The mapped value</returns>
		public object MapToHostType(JsValueRaw value)
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
						JsValueRaw externalObjectValue = value.GetProperty(externalObjectPropertyId);
						result = externalObjectValue.HasExternalData ?
							GCHandle.FromIntPtr(externalObjectValue.ExternalData).Target : null;
					}

					result ??= value.ConvertToObject();
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
		public object MTH(JsValueRaw value) => MapToHostType(value);


		/// <summary>
		/// Creates a JavaScript value from an host object if the it does not already exist
		/// </summary>
		/// <param name="obj">Instance of host type</param>
		/// <returns>JavaScript value created from an host object</returns>
		public JsValueRaw GetOrCreateScriptObject(object obj)
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
			if (delArray != null)
				return CreateEmbeddedFunction(delArray);
			else if (del != null)
				return CreateEmbeddedFunction(del);
			else
				return CreateEmbeddedObject(obj);

		}

		private EmbeddedObject CreateEmbeddedObject(object obj)
		{
			GCHandle objHandle = GCHandle.Alloc(obj);
			IntPtr objPtr = GCHandle.ToIntPtr(objHandle);
			JsValueRaw objValue = JsValueRaw.CreateExternalObject(objPtr, _embeddedObjectFinalizeCallback);

			var embeddedObject = new EmbeddedObject(obj, objValue);
			var embeddingOptions = GetEmbeddingObjectOptions(obj);

			ProjectFields(embeddedObject, embeddingOptions);
			ProjectProperties(embeddedObject, embeddingOptions);
			ProjectMethods(embeddedObject, embeddingOptions);
			if(embeddingOptions.Freeze)
				FreezeObject(objValue);

			return embeddedObject;
		}

		private void ProjectFields(EmbeddedItem externalItem, EmbeddingObjectOptions options)
		{
			Type type = externalItem.HostType;
			object obj = externalItem.HostObject;
			JsValueRaw typeValue = externalItem.ScriptValue;
			bool instance = externalItem.IsInstance;
			IList<JsNativeFunction> nativeFunctions = externalItem.NativeFunctions;

			string typeName = type.FullName;
			BindingFlags defaultBindingFlags = ReflectionHelpers.GetDefaultBindingFlags(instance);
			FieldInfo[] fields = type.GetFields(defaultBindingFlags).Where(options.IsMapped).ToArray();

			foreach (FieldInfo field in fields)
			{
				string fieldName = field.Name;

				JsValueRaw descriptorValue = JsValueRaw.CreateObject();
				descriptorValue.SetProperty("enumerable", JsValueRaw.True, true);

                JsValueRaw nativeGetFunction(JsValueRaw callee, bool isConstructCall, JsValueRaw[] args, ushort argCount, IntPtr callbackData)
                {
                    if (instance && obj == null)
                    {
                        CreateAndSetError($"Context error while invoking getter '{fieldName}'.");
                        return JsValueRaw.Undefined; ;
                    }
                    object result;

                    try
                    {
                        result = field.GetValue(obj);
                    }
                    catch (Exception e)
                    {
                        Exception exception = UnwrapException(e);
                        var wrapperException = exception as JsException;
                        JsValueRaw errorValue;

                        if (wrapperException != null)
                        {
                            errorValue = CreateErrorFromWrapperException(wrapperException);
                        }
                        else
                        {
                            string errorMessage = instance ?
                                $"Error ocured while reading field '{fieldName}': {exception.Message}"
                                :
                                $"Erorr ocured while reading static field '{fieldName}' from type '{typeName}': {exception.Message}"
                                ;
                            errorValue = JsValueRaw.CreateError(JsValueRaw.FromString(errorMessage));
                        }
                        JsContext.SetException(errorValue);

                        return JsValueRaw.Undefined;
                    }

                    JsValueRaw resultValue = MapToScriptType(result);

                    return resultValue;
                }
                nativeFunctions.Add(nativeGetFunction);

				JsValueRaw getMethodValue = JsValueRaw.CreateFunction(nativeGetFunction);
				descriptorValue.SetProperty("get", getMethodValue, true);

                JsValueRaw nativeSetFunction(JsValueRaw callee, bool isConstructCall, JsValueRaw[] args, ushort argCount, IntPtr callbackData)
                {
                    if (instance && obj == null)
                    {
                        CreateAndSetError($"Invalid context got host object field {fieldName}.");
                        return JsValueRaw.Undefined;
                    }

                    object value = MapToHostType(args[1]);
                    ReflectionHelpers.FixFieldValueType(ref value, field);

                    try
                    {
                        field.SetValue(obj, value);
                    }
                    catch (Exception e)
                    {
                        Exception exception = UnwrapException(e);
                        var wrapperException = exception as JsException;
                        JsValueRaw errorValue;

                        if (wrapperException != null)
                        {
                            errorValue = CreateErrorFromWrapperException(wrapperException);
                        }
                        else
                        {
                            string errorMessage = instance ?
                                $"Failed to set value for hosts object field '{fieldName}': {exception.Message}"
                                :
                                $"Failed to set value for static type '{typeName}' field '{fieldName}': {exception.Message}"
                                ;
                            errorValue = JsValueRaw.CreateError(JsValueRaw.FromString(errorMessage));
                        }
                        JsContext.SetException(errorValue);

                        return JsValueRaw.Undefined;
                    }

                    return JsValueRaw.Undefined;
                }
                nativeFunctions.Add(nativeSetFunction);

				JsValueRaw setMethodValue = JsValueRaw.CreateFunction(nativeSetFunction);
				descriptorValue.SetProperty("set", setMethodValue, true);

				typeValue.DefineProperty(fieldName, descriptorValue);
			}
		}

		private void ProjectProperties(EmbeddedItem externalItem, EmbeddingObjectOptions options)
		{
			Type type = externalItem.HostType;
			object obj = externalItem.HostObject;
			JsValueRaw typeValue = externalItem.ScriptValue;
			IList<JsNativeFunction> nativeFunctions = externalItem.NativeFunctions;
			bool instance = externalItem.IsInstance;

			string typeName = type.FullName;
			BindingFlags defaultBindingFlags = ReflectionHelpers.GetDefaultBindingFlags(instance);
			PropertyInfo[] properties = type.GetProperties(defaultBindingFlags).Where(options.IsMapped).ToArray();

			foreach (PropertyInfo property in properties)
			{
				string propertyName = property.Name;

				JsValueRaw descriptorValue = JsValueRaw.CreateObject();
				descriptorValue.SetProperty("enumerable", JsValueRaw.True, true);

				if (property.GetGetMethod() != null)
				{
                    JsValueRaw nativeGetFunction(JsValueRaw callee, bool isConstructCall, JsValueRaw[] args, ushort argCount, IntPtr callbackData)
                    {
                        if (instance && obj == null)
                        {
                            CreateAndSetError($"Invalid context for '{propertyName}' property.");
                            return JsValueRaw.Undefined;
                        }

                        object result;

                        try
                        {
                            result = property.GetValue(obj, new object[0]);
                        }
                        catch (Exception e)
                        {
                            Exception exception = UnwrapException(e);
                            var wrapperException = exception as JsException;
                            JsValueRaw errorValue;

                            if (wrapperException != null)
                            {
                                errorValue = CreateErrorFromWrapperException(wrapperException);
                            }
                            else
                            {
                                string errorMessage = instance ?
                                    $"Property '{propertyName}' get operation failed: {exception.Message}"
                                    :
                                    $"Property '{propertyName}' of static type '{typeName}' get operation failed: {exception.Message}"
                                    ;
                                errorValue = JsValueRaw.CreateError(JsValueRaw.FromString(errorMessage));
                            }
                            JsContext.SetException(errorValue);

                            return JsValueRaw.Undefined;
                        }

                        JsValueRaw resultValue = MapToScriptType(result);

                        return resultValue;
                    }
                    nativeFunctions.Add(nativeGetFunction);

					JsValueRaw getMethodValue = JsValueRaw.CreateFunction(nativeGetFunction);
					descriptorValue.SetProperty("get", getMethodValue, true);
				}

				if (property.GetSetMethod() != null)
				{
                    JsValueRaw nativeSetFunction(JsValueRaw callee, bool isConstructCall, JsValueRaw[] args, ushort argCount, IntPtr callbackData)
                    {
                        JsValueRaw undefinedValue = JsValueRaw.Undefined;

                        if (instance && obj == null)
                        {
                            CreateAndSetError($"Invalid context for '{propertyName}' property.");
                            return undefinedValue;
                        }

                        object value = MapToHostType(args[1]);
                        ReflectionHelpers.FixPropertyValueType(ref value, property);

                        try
                        {
                            property.SetValue(obj, value, new object[0]);
                        }
                        catch (Exception e)
                        {
                            Exception exception = UnwrapException(e);
                            var wrapperException = exception as JsException;
                            JsValueRaw errorValue;

                            if (wrapperException != null)
                            {
                                errorValue = CreateErrorFromWrapperException(wrapperException);
                            }
                            else
                            {
                                string errorMessage = instance ?
                                    $"Host object property '{propertyName}' setting failed: {exception.Message}"
                                    :
                                    $"Host type '{typeName}' property '{propertyName}' setting failed: {exception.Message}"
                                    ;
                                errorValue = JsValueRaw.CreateError(JsValueRaw.FromString(errorMessage));
                            }
                            JsContext.SetException(errorValue);

                            return undefinedValue;
                        }

                        return undefinedValue;
                    }
                    nativeFunctions.Add(nativeSetFunction);

					JsValueRaw setMethodValue = JsValueRaw.CreateFunction(nativeSetFunction);
					descriptorValue.SetProperty("set", setMethodValue, true);
				}

				typeValue.DefineProperty(propertyName, descriptorValue);
			}
		}

		private void ProjectMethods(EmbeddedItem externalItem, EmbeddingObjectOptions options)
		{
			Type type = externalItem.HostType;
			object obj = externalItem.HostObject;
			JsValueRaw typeValue = externalItem.ScriptValue;
			IList<JsNativeFunction> nativeFunctions = externalItem.NativeFunctions;
			bool instance = externalItem.IsInstance;

			string typeName = type.FullName;
			BindingFlags defaultBindingFlags = ReflectionHelpers.GetDefaultBindingFlags(instance);
			var methods = type.GetMethods(defaultBindingFlags).Select(options.ExtendInfo)
				.Where((m) => m.IsMapped);
			var methodGroups = methods.GroupBy(m => m.Name);

			foreach (var methodGroup in methodGroups)
			{
				string methodName = methodGroup.Key;
				MethodInfo[] methodCandidates = methodGroup.Select(m => m.Info).ToArray();

                JsValueRaw nativeFunction(JsValueRaw callee, bool isConstructCall, JsValueRaw[] args, ushort argCount, IntPtr callbackData)
                {
                    if (instance && obj == null)
                    {
                        CreateAndSetError($"Invalid context while calling method '{methodName}'.");
                        return JsValueRaw.Undefined;
                    }

                    if (!SelectAndProcessFunction(methodCandidates, args, argCount, out MethodInfo bestSelection, out object[] processedArgs))
                    {
                        CreateAndSetError($"Suitable method '{methodName}' was not found.");
                        return JsValueRaw.Undefined;
                    }

                    object result;

                    try
                    {
                        result = bestSelection.Invoke(obj, processedArgs);
                    }
                    catch (Exception e)
                    {
                        Exception exception = UnwrapException(e);
                        var wrapperException = exception as JsException;
                        JsValueRaw errorValue;

                        if (wrapperException != null)
                        {
                            errorValue = CreateErrorFromWrapperException(wrapperException);
                        }
                        else
                        {
                            string errorMessage = instance ?
                                $"Host method '{methodName}' invocation error: {exception.Message}"
                                :
                                $"Host static type '{typeName}' method '{methodName}' invocation error: {exception.Message}"
                                ;
                            errorValue = JsValueRaw.CreateError(JsValueRaw.FromString(errorMessage));
                        }
                        JsContext.SetException(errorValue);

                        return JsValueRaw.Undefined;
                    }

                    JsValueRaw resultValue = MapToScriptType(result);

                    return resultValue;
                }
                nativeFunctions.Add(nativeFunction);

				JsValueRaw methodValue = JsValueRaw.CreateNamedFunction(methodName, nativeFunction);
				typeValue.SetProperty(methodName, methodValue, true);
			}
		}

		private EmbeddingObjectOptions GetEmbeddingObjectOptions(object obj)
		{
			return new EmbeddingObjectOptions(obj.GetType());
		}

		private EmbeddedObject CreateEmbeddedFunction(Delegate del)
		{
            JsValueRaw nativeFunction(JsValueRaw callee, bool isConstructCall, JsValueRaw[] args, ushort argCount, IntPtr callbackData)
            {
                var methodInfo = del.GetMethodInfo();
                var parameters = methodInfo.GetParameters();

                if (!IsCompatibleSignature(args.Select(v => v.ValueType).ToArray(), parameters, out ParameterType[] prameterTypes))
                {
                    CreateAndSetError($"Method signature is incompatible with passed arguments.");
                }
                if (!ProcessFunction(parameters, prameterTypes, args, argCount, out object[] processedArgs))
                {
                    CreateAndSetError($"Failed to process arguments.");
                }

                object result;

                try
                {
                    result = del.DynamicInvoke(processedArgs);
                }
                catch (Exception e)
                {
                    JsValueRaw undefinedValue = JsValueRaw.Undefined;
                    Exception exception = UnwrapException(e);
                    var wrapperException = exception as JsException;
                    JsValueRaw errorValue = wrapperException != null ?
                        CreateErrorFromWrapperException(wrapperException)
                        :
                        JsValueRaw.CreateError(JsValueRaw.FromString($"Host delegate invocation error: {exception.Message}"));
                    ;
                    JsContext.SetException(errorValue);

                    return undefinedValue;
                }

                JsValueRaw resultValue = MapToScriptType(result);

                return resultValue;
            }

            GCHandle delHandle = GCHandle.Alloc(del);
			IntPtr delPtr = GCHandle.ToIntPtr(delHandle);
			JsValueRaw objValue = JsValueRaw.CreateExternalObject(delPtr, _embeddedObjectFinalizeCallback);

			JsValueRaw functionValue = JsValueRaw.CreateFunction(nativeFunction);
			SetNonEnumerableProperty(functionValue, ExternalObjectPropertyName, objValue);

			var embeddedObject = new EmbeddedObject(del, functionValue,
				new List<JsNativeFunction> { nativeFunction });

			return embeddedObject;
		}

		private EmbeddedObject CreateEmbeddedFunction(Delegate[] del)
		{
            JsValueRaw nativeFunction(JsValueRaw callee, bool isConstructCall, JsValueRaw[] args, ushort argCount, IntPtr callbackData)
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
                    JsValueRaw undefinedValue = JsValueRaw.Undefined;
                    Exception exception = UnwrapException(e);
                    var wrapperException = exception as JsException;
                    JsValueRaw errorValue = wrapperException != null ?
                        CreateErrorFromWrapperException(wrapperException)
                        :
                        JsValueRaw.CreateError(JsValueRaw.FromString($"Host delegate invocation error: {exception.Message}"));
                    ;
                    JsContext.SetException(errorValue);

                    return undefinedValue;
                }

                JsValueRaw resultValue = MapToScriptType(result);

                return resultValue;
            }

            GCHandle delHandle = GCHandle.Alloc(del);
			IntPtr delPtr = GCHandle.ToIntPtr(delHandle);
			JsValueRaw objValue = JsValueRaw.CreateExternalObject(delPtr, _embeddedObjectFinalizeCallback);

			JsValueRaw functionValue = JsValueRaw.CreateFunction(nativeFunction);
			SetNonEnumerableProperty(functionValue, ExternalObjectPropertyName, objValue);

			var embeddedObject = new EmbeddedObject(del, functionValue,
				new List<JsNativeFunction> { nativeFunction });

			return embeddedObject;
		}

		private bool SelectAndProcessFunction(Delegate[] funs, JsValueRaw[] args, ushort argscount, out Delegate selection, out object[] processedArgs)
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
			return ProcessFunction(candidite.paramInfo, candidite.types, args, argscount, out processedArgs);
		}

		private bool SelectAndProcessFunction(MethodInfo[] funs, JsValueRaw[] args, ushort argscount, out MethodInfo selection, out object[] processedArgs)
		{
			selection = null;
			processedArgs = null;
			var argTypes = args.Select(v => v.ValueType).ToArray();
			var candidite = funs.Select(mi =>
			{
				var pi = mi.GetParameters();
				var isc = IsCompatibleSignature(argTypes, pi, out ParameterType[] pt);
				return new { methodInfo = mi, compatible = isc, types = pt, paramInfo = pi };
			}).Where(d => d.compatible).OrderByDescending(d => d.types.Length).FirstOrDefault();
			if (candidite == null)
				return false;

			selection = candidite.methodInfo;
			return ProcessFunction(candidite.paramInfo, candidite.types, args, argscount, out processedArgs);
		}

		private bool ProcessFunction(ParameterInfo[] parameterInfos, ParameterType[] parameterTypes, JsValueRaw[] args,  ushort argscount, out object[] processedArgs)
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
					case ParameterType.InjectMeta:
						var pi = parameterInfos[i];
						if (pi.ParameterType == typeof(JsRuntime))
							processedArgs[i] = JsContext.Current.Runtime;
						else if (pi.ParameterType == typeof(JsContext))
							processedArgs[i] = JsContext.Current;
						else if (pi.ParameterType == typeof(TypeMapper))
							processedArgs[i] = this;
						break;
					case ParameterType.Convert:
						processedArgs[i] = MapToHostType(args[aPos]);
						Type targetType = parameterInfos[i].ParameterType;
						Type currentType = processedArgs[i].GetType();
						if (targetType != currentType)
						{
							if (TypeConverter.TryConvertToType(processedArgs[i], targetType, out object convertedValue))
							{
								processedArgs[i] = convertedValue;
							}
						}
						aPos++;
						break;
					case ParameterType.GLOB:
						processedArgs[i] = JsValueRaw.GlobalObject;
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
				if (param.ParameterType == typeof(JsValueRaw))
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
				// We ran out of arguments
				if (args.Length <= argPos && !param.IsOptional)
					return false;
				if (AreTypesCompatible(args[argPos], param.ParameterType))
				{
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
		private static void SetNonEnumerableProperty(JsValueRaw objValue, string name, JsValueRaw value)
		{
			JsValueRaw descriptorValue = JsValueRaw.CreateObject();
			descriptorValue.SetProperty("enumerable", JsValueRaw.False, true);
			descriptorValue.SetProperty("writable", JsValueRaw.True, true);

			JsPropertyId id = JsPropertyId.FromString(name);
			objValue.DefineProperty(id, descriptorValue);
			objValue.SetProperty(id, value, true);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CreateAndSetError(string message)
		{
			JsValueRaw errorValue = JsValueRaw.CreateError(JsValueRaw.FromString(message));
			JsContext.SetException(errorValue);
		}

		private static JsValueRaw CreateErrorFromWrapperException(JsException exception)
		{
			var originalException = exception.InnerException as JsException;
			JsErrorCode errorCode = originalException != null ?
				originalException.ErrorCode : JsErrorCode.NoError;
			var description = Enum.GetName(typeof(JsErrorCode), errorCode);

			JsValueRaw innerErrorValue = JsValueRaw.CreateError(JsValueRaw.FromString(description));
			innerErrorValue.SetProperty("description", JsValueRaw.FromString(description), true);

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
			JsValueRaw errorValue = JsValueRaw.CreateError(JsValueRaw.FromString(description));
			errorValue.SetProperty("innerException", innerErrorValue, true);

			return errorValue;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FreezeObject(JsValueRaw objValue)
		{
			JsValueRaw freezeMethodValue = JsValueRaw.GlobalObject
				.GetProperty("Object")
				.GetProperty("freeze");
			freezeMethodValue.CallFunction(objValue);
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

                if (lazyEmbeddedObjects.TryRemove(embeddedObjectKey, out Lazy<EmbeddedObject> lazyEmbeddedObject))
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

                            if (lazyEmbeddedObjects.TryGetValue(key, out Lazy<EmbeddedObject> lazyEmbeddedObject))
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
