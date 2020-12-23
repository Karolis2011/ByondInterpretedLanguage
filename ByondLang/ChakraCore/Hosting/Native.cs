using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Hosting
{
    /// <summary>
    ///     Native interfaces.
    /// </summary>
    public static class Native
    {
        /// <summary>
        /// Throws if a native method returns an error code.
        /// </summary>
        /// <param name="error">The error.</param>
        internal static void ThrowIfError(JsErrorCode error)
        {
            if (error != JsErrorCode.NoError)
            {
                switch (error)
                {
                    case JsErrorCode.InvalidArgument:
                        throw new JsUsageException(error, "Invalid argument.");

                    case JsErrorCode.NullArgument:
                        throw new JsUsageException(error, "Null argument.");

                    case JsErrorCode.NoCurrentContext:
                        throw new JsUsageException(error, "No current context.");

                    case JsErrorCode.InExceptionState:
                        throw new JsUsageException(error, "Runtime is in exception state.");

                    case JsErrorCode.NotImplemented:
                        throw new JsUsageException(error, "Method is not implemented.");

                    case JsErrorCode.WrongThread:
                        throw new JsUsageException(error, "Runtime is active on another thread.");

                    case JsErrorCode.RuntimeInUse:
                        throw new JsUsageException(error, "Runtime is in use.");

                    case JsErrorCode.BadSerializedScript:
                        throw new JsUsageException(error, "Bad serialized script.");

                    case JsErrorCode.InDisabledState:
                        throw new JsUsageException(error, "Runtime is disabled.");

                    case JsErrorCode.CannotDisableExecution:
                        throw new JsUsageException(error, "Cannot disable execution.");

                    case JsErrorCode.AlreadyDebuggingContext:
                        throw new JsUsageException(error, "Context is already in debug mode.");

                    case JsErrorCode.HeapEnumInProgress:
                        throw new JsUsageException(error, "Heap enumeration is in progress.");

                    case JsErrorCode.ArgumentNotObject:
                        throw new JsUsageException(error, "Argument is not an object.");

                    case JsErrorCode.InProfileCallback:
                        throw new JsUsageException(error, "In a profile callback.");

                    case JsErrorCode.InThreadServiceCallback:
                        throw new JsUsageException(error, "In a thread service callback.");

                    case JsErrorCode.CannotSerializeDebugScript:
                        throw new JsUsageException(error, "Cannot serialize a debug script.");

                    case JsErrorCode.AlreadyProfilingContext:
                        throw new JsUsageException(error, "Already profiling this context.");

                    case JsErrorCode.IdleNotEnabled:
                        throw new JsUsageException(error, "Idle is not enabled.");

                    case JsErrorCode.OutOfMemory:
                        throw new JsEngineException(error, "Out of memory.");

                    case JsErrorCode.ScriptException:
                        {
                            JsErrorCode innerError = JsGetAndClearException(out JsValueRaw errorObject);

                            if (innerError != JsErrorCode.NoError)
                            {
                                throw new JsFatalException(innerError);
                            }
                            throw JsScriptException.FromError(error, errorObject, "Script error");
                        }

                    case JsErrorCode.ScriptCompile:
                        {
                            JsErrorCode innerError = JsGetAndClearException(out JsValueRaw errorObject);

                            if (innerError != JsErrorCode.NoError)
                            {
                                throw new JsFatalException(innerError);
                            }
                            throw JsScriptException.FromError(error, errorObject, "Compile error");
                        }

                    case JsErrorCode.ScriptTerminated:
                        throw new JsScriptException(error, JsValueRaw.Invalid, "Script was terminated.");

                    case JsErrorCode.ScriptEvalDisabled:
                        throw new JsScriptException(error, JsValueRaw.Invalid, "Eval of strings is disabled in this runtime.");

                    case JsErrorCode.Fatal:
                        throw new JsFatalException(error);

                    default:
                        throw new JsFatalException(error);
                }
            }
        }

        const string DllName = "ChakraCore";

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateRuntime(JsRuntimeAttributes attributes, JsThreadServiceCallback threadService, out JsRuntime runtime);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCollectGarbage(JsRuntime handle);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDisposeRuntime(JsRuntime handle);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetRuntimeMemoryUsage(JsRuntime runtime, out UIntPtr memoryUsage);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetRuntimeMemoryLimit(JsRuntime runtime, out UIntPtr memoryLimit);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetRuntimeMemoryLimit(JsRuntime runtime, UIntPtr memoryLimit);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetRuntimeMemoryAllocationCallback(JsRuntime runtime, IntPtr callbackState, JsMemoryAllocationCallback allocationCallback);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetRuntimeBeforeCollectCallback(JsRuntime runtime, IntPtr callbackState, JsBeforeCollectCallback beforeCollectCallback);

        [DllImport(DllName, EntryPoint = "JsAddRef")]
        internal static extern JsErrorCode JsContextAddRef(JsContext reference, out uint count);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsAddRef(JsValueRaw reference, out uint count);

        [DllImport(DllName, EntryPoint = "JsRelease")]
        internal static extern JsErrorCode JsContextRelease(JsContext reference, out uint count);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsRelease(JsValueRaw reference, out uint count);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateContext(JsRuntime runtime, out JsContext newContext);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetCurrentContext(out JsContext currentContext);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetCurrentContext(JsContext context);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetRuntime(JsContext context, out JsRuntime runtime);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsIdle(out uint nextIdleTick);

        

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JsErrorCode JsGetPropertyIdFromName(string name, out JsPropertyId propertyId);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JsErrorCode JsGetPropertyNameFromId(JsPropertyId propertyId, out string name);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetUndefinedValue(out JsValueRaw undefinedValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetNullValue(out JsValueRaw nullValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetTrueValue(out JsValueRaw trueValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetFalseValue(out JsValueRaw falseValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsBoolToBoolean(bool value, out JsValueRaw booleanValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsBooleanToBool(JsValueRaw booleanValue, out bool boolValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConvertValueToBoolean(JsValueRaw value, out JsValueRaw booleanValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetValueType(JsValueRaw value, out JsValueType type);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDoubleToNumber(double doubleValue, out JsValueRaw value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsIntToNumber(int intValue, out JsValueRaw value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsNumberToDouble(JsValueRaw value, out double doubleValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConvertValueToNumber(JsValueRaw value, out JsValueRaw numberValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetStringLength(JsValueRaw sringValue, out int length);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JsErrorCode JsPointerToString(string value, UIntPtr stringLength, out JsValueRaw stringValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsStringToPointer(JsValueRaw value, out IntPtr stringValue, out UIntPtr stringLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConvertValueToString(JsValueRaw value, out JsValueRaw stringValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetGlobalObject(out JsValueRaw globalObject);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateObject(out JsValueRaw obj);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateExternalObject(IntPtr data, JsFinalizeCallback finalizeCallback, out JsValueRaw obj);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConvertValueToObject(JsValueRaw value, out JsValueRaw obj);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPrototype(JsValueRaw obj, out JsValueRaw prototypeObject);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetPrototype(JsValueRaw obj, JsValueRaw prototypeObject);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetExtensionAllowed(JsValueRaw obj, out bool value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsPreventExtension(JsValueRaw obj);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetProperty(JsValueRaw obj, JsPropertyId propertyId, out JsValueRaw value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetOwnPropertyDescriptor(JsValueRaw obj, JsPropertyId propertyId, out JsValueRaw propertyDescriptor);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetOwnPropertyNames(JsValueRaw obj, out JsValueRaw propertyNames);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetProperty(JsValueRaw obj, JsPropertyId propertyId, JsValueRaw value, bool useStrictRules);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasProperty(JsValueRaw obj, JsPropertyId propertyId, out bool hasProperty);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDeleteProperty(JsValueRaw obj, JsPropertyId propertyId, bool useStrictRules, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDefineProperty(JsValueRaw obj, JsPropertyId propertyId, JsValueRaw propertyDescriptor, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasIndexedProperty(JsValueRaw obj, JsValueRaw index, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetIndexedProperty(JsValueRaw obj, JsValueRaw index, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetIndexedProperty(JsValueRaw obj, JsValueRaw index, JsValueRaw value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDeleteIndexedProperty(JsValueRaw obj, JsValueRaw index);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsEquals(JsValueRaw obj1, JsValueRaw obj2, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsStrictEquals(JsValueRaw obj1, JsValueRaw obj2, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasExternalData(JsValueRaw obj, out bool value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetExternalData(JsValueRaw obj, out IntPtr externalData);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetExternalData(JsValueRaw obj, IntPtr externalData);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateArray(uint length, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCallFunction(JsValueRaw function, JsValueRaw[] arguments, ushort argumentCount, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConstructObject(JsValueRaw function, JsValueRaw[] arguments, ushort argumentCount, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateFunction(JsNativeFunction nativeFunction, IntPtr externalData, out JsValueRaw function);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateError(JsValueRaw message, out JsValueRaw error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateRangeError(JsValueRaw message, out JsValueRaw error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateReferenceError(JsValueRaw message, out JsValueRaw error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateSyntaxError(JsValueRaw message, out JsValueRaw error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateTypeError(JsValueRaw message, out JsValueRaw error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateURIError(JsValueRaw message, out JsValueRaw error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasException(out bool hasException);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetAndClearException(out JsValueRaw exception);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetException(JsValueRaw exception);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDisableRuntimeExecution(JsRuntime runtime);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsEnableRuntimeExecution(JsRuntime runtime);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsIsRuntimeExecutionDisabled(JsRuntime runtime, out bool isDisabled);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetObjectBeforeCollectCallback(JsValueRaw reference, IntPtr callbackState, JsObjectBeforeCollectCallback beforeCollectCallback);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateNamedFunction(JsValueRaw name, JsNativeFunction nativeFunction, IntPtr callbackState, out JsValueRaw function);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateArrayBuffer(uint byteLength, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateTypedArray(JsTypedArrayType arrayType, JsValueRaw arrayBuffer, uint byteOffset,
            uint elementLength, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateDataView(JsValueRaw arrayBuffer, uint byteOffset, uint byteOffsetLength, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetArrayBufferStorage(JsValueRaw arrayBuffer, out IntPtr buffer, out uint bufferLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetTypedArrayStorage(JsValueRaw typedArray, out IntPtr buffer, out uint bufferLength, out JsTypedArrayType arrayType, out int elementSize);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetDataViewStorage(JsValueRaw dataView, out IntPtr buffer, out uint bufferLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPropertyIdType(JsPropertyId propertyId, out JsPropertyIdType propertyIdType);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateSymbol(JsValueRaw description, out JsValueRaw symbol);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetSymbolFromPropertyId(JsPropertyId propertyId, out JsValueRaw symbol);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPropertyIdFromSymbol(JsValueRaw symbol, out JsPropertyId propertyId);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetOwnPropertySymbols(JsValueRaw obj, out JsValueRaw propertySymbols);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsNumberToInt(JsValueRaw value, out int intValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetIndexedPropertiesToExternalData(JsValueRaw obj, IntPtr data, JsTypedArrayType arrayType, uint elementLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetIndexedPropertiesExternalData(JsValueRaw obj, IntPtr data, out JsTypedArrayType arrayType, out uint elementLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasIndexedPropertiesExternalData(JsValueRaw obj, out bool value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsInstanceOf(JsValueRaw obj, JsValueRaw constructor, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateExternalArrayBuffer(IntPtr data, uint byteLength, JsFinalizeCallback finalizeCallback, IntPtr callbackState, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetTypedArrayInfo(JsValueRaw typedArray, out JsTypedArrayType arrayType, out JsValueRaw arrayBuffer, out uint byteOffset, out uint byteLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetContextOfObject(JsValueRaw obj, out JsContext context);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetContextData(JsContext context, out IntPtr data);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetContextData(JsContext context, IntPtr data);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetPromiseContinuationCallback(
            JsPromiseContinuationCallback promiseContinuationCallback, IntPtr callbackState);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreatePromise(out JsValueRaw promise, out JsValueRaw resolveFunction, out JsValueRaw rejectFunction);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPromiseState(JsValueRaw promise, out JsPromiseState state);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPromiseResult(JsValueRaw promise, out JsValueRaw result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetHostPromiseRejectionTracker(
           JsHostPromiseRejectionTrackerCallback promiseRejectionTrackerCallback, IntPtr callbackState);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JsErrorCode JsParse(JsValueRaw script, JsSourceContext sourceContext, JsValueRaw sourceUrl, JsParseScriptAttributes parseAttributes, out JsValueRaw result);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JsErrorCode JsRun(JsValueRaw script, JsSourceContext sourceContext, JsValueRaw sourceUrl, JsParseScriptAttributes parseAttributes, out JsValueRaw result);

        // TODO:
        // https://github.com/Microsoft/ChakraCore/issues/4324

    }
}
