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
                            JsErrorCode innerError = JsGetAndClearException(out JsValue errorObject);

                            if (innerError != JsErrorCode.NoError)
                            {
                                throw new JsFatalException(innerError);
                            }
                            throw JsScriptException.FromError(error, errorObject, "Script error");
                        }

                    case JsErrorCode.ScriptCompile:
                        {
                            JsErrorCode innerError = JsGetAndClearException(out JsValue errorObject);

                            if (innerError != JsErrorCode.NoError)
                            {
                                throw new JsFatalException(innerError);
                            }
                            throw JsScriptException.FromError(error, errorObject, "Compile error");
                        }

                    case JsErrorCode.ScriptTerminated:
                        throw new JsScriptException(error, JsValue.Invalid, "Script was terminated.");

                    case JsErrorCode.ScriptEvalDisabled:
                        throw new JsScriptException(error, JsValue.Invalid, "Eval of strings is disabled in this runtime.");

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
        internal static extern JsErrorCode JsAddRef(JsValue reference, out uint count);

        [DllImport(DllName, EntryPoint = "JsRelease")]
        internal static extern JsErrorCode JsContextRelease(JsContext reference, out uint count);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsRelease(JsValue reference, out uint count);

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
        internal static extern JsErrorCode JsGetUndefinedValue(out JsValue undefinedValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetNullValue(out JsValue nullValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetTrueValue(out JsValue trueValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetFalseValue(out JsValue falseValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsBoolToBoolean(bool value, out JsValue booleanValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsBooleanToBool(JsValue booleanValue, out bool boolValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConvertValueToBoolean(JsValue value, out JsValue booleanValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetValueType(JsValue value, out JsValueType type);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDoubleToNumber(double doubleValue, out JsValue value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsIntToNumber(int intValue, out JsValue value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsNumberToDouble(JsValue value, out double doubleValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConvertValueToNumber(JsValue value, out JsValue numberValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetStringLength(JsValue sringValue, out int length);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JsErrorCode JsPointerToString(string value, UIntPtr stringLength, out JsValue stringValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsStringToPointer(JsValue value, out IntPtr stringValue, out UIntPtr stringLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConvertValueToString(JsValue value, out JsValue stringValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetGlobalObject(out JsValue globalObject);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateObject(out JsValue obj);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateExternalObject(IntPtr data, JsFinalizeCallback finalizeCallback, out JsValue obj);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConvertValueToObject(JsValue value, out JsValue obj);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPrototype(JsValue obj, out JsValue prototypeObject);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetPrototype(JsValue obj, JsValue prototypeObject);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetExtensionAllowed(JsValue obj, out bool value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsPreventExtension(JsValue obj);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetProperty(JsValue obj, JsPropertyId propertyId, out JsValue value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetOwnPropertyDescriptor(JsValue obj, JsPropertyId propertyId, out JsValue propertyDescriptor);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetOwnPropertyNames(JsValue obj, out JsValue propertyNames);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetProperty(JsValue obj, JsPropertyId propertyId, JsValue value, bool useStrictRules);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasProperty(JsValue obj, JsPropertyId propertyId, out bool hasProperty);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDeleteProperty(JsValue obj, JsPropertyId propertyId, bool useStrictRules, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDefineProperty(JsValue obj, JsPropertyId propertyId, JsValue propertyDescriptor, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasIndexedProperty(JsValue obj, JsValue index, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetIndexedProperty(JsValue obj, JsValue index, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetIndexedProperty(JsValue obj, JsValue index, JsValue value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDeleteIndexedProperty(JsValue obj, JsValue index);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsEquals(JsValue obj1, JsValue obj2, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsStrictEquals(JsValue obj1, JsValue obj2, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasExternalData(JsValue obj, out bool value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetExternalData(JsValue obj, out IntPtr externalData);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetExternalData(JsValue obj, IntPtr externalData);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateArray(uint length, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCallFunction(JsValue function, JsValue[] arguments, ushort argumentCount, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsConstructObject(JsValue function, JsValue[] arguments, ushort argumentCount, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateFunction(JsNativeFunction nativeFunction, IntPtr externalData, out JsValue function);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateError(JsValue message, out JsValue error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateRangeError(JsValue message, out JsValue error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateReferenceError(JsValue message, out JsValue error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateSyntaxError(JsValue message, out JsValue error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateTypeError(JsValue message, out JsValue error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateURIError(JsValue message, out JsValue error);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasException(out bool hasException);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetAndClearException(out JsValue exception);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetException(JsValue exception);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsDisableRuntimeExecution(JsRuntime runtime);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsEnableRuntimeExecution(JsRuntime runtime);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsIsRuntimeExecutionDisabled(JsRuntime runtime, out bool isDisabled);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetObjectBeforeCollectCallback(JsValue reference, IntPtr callbackState, JsObjectBeforeCollectCallback beforeCollectCallback);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateNamedFunction(JsValue name, JsNativeFunction nativeFunction, IntPtr callbackState, out JsValue function);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateArrayBuffer(uint byteLength, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateTypedArray(JsTypedArrayType arrayType, JsValue arrayBuffer, uint byteOffset,
            uint elementLength, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateDataView(JsValue arrayBuffer, uint byteOffset, uint byteOffsetLength, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetArrayBufferStorage(JsValue arrayBuffer, out IntPtr buffer, out uint bufferLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetTypedArrayStorage(JsValue typedArray, out IntPtr buffer, out uint bufferLength, out JsTypedArrayType arrayType, out int elementSize);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetDataViewStorage(JsValue dataView, out IntPtr buffer, out uint bufferLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPropertyIdType(JsPropertyId propertyId, out JsPropertyIdType propertyIdType);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateSymbol(JsValue description, out JsValue symbol);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetSymbolFromPropertyId(JsPropertyId propertyId, out JsValue symbol);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPropertyIdFromSymbol(JsValue symbol, out JsPropertyId propertyId);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetOwnPropertySymbols(JsValue obj, out JsValue propertySymbols);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsNumberToInt(JsValue value, out int intValue);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetIndexedPropertiesToExternalData(JsValue obj, IntPtr data, JsTypedArrayType arrayType, uint elementLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetIndexedPropertiesExternalData(JsValue obj, IntPtr data, out JsTypedArrayType arrayType, out uint elementLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsHasIndexedPropertiesExternalData(JsValue obj, out bool value);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsInstanceOf(JsValue obj, JsValue constructor, out bool result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreateExternalArrayBuffer(IntPtr data, uint byteLength, JsFinalizeCallback finalizeCallback, IntPtr callbackState, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetTypedArrayInfo(JsValue typedArray, out JsTypedArrayType arrayType, out JsValue arrayBuffer, out uint byteOffset, out uint byteLength);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetContextOfObject(JsValue obj, out JsContext context);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetContextData(JsContext context, out IntPtr data);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetContextData(JsContext context, IntPtr data);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetPromiseContinuationCallback(
            JsPromiseContinuationCallback promiseContinuationCallback, IntPtr callbackState);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsCreatePromise(out JsValue promise, out JsValue resolveFunction, out JsValue rejectFunction);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPromiseState(JsValue promise, out JsPromiseState state);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsGetPromiseResult(JsValue promise, out JsValue result);

        [DllImport(DllName)]
        internal static extern JsErrorCode JsSetHostPromiseRejectionTracker(
           JsHostPromiseRejectionTrackerCallback promiseRejectionTrackerCallback, IntPtr callbackState);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JsErrorCode JsParse(JsValue script, JsSourceContext sourceContext, JsValue sourceUrl, JsParseScriptAttributes parseAttributes, out JsValue result);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern JsErrorCode JsRun(JsValue script, JsSourceContext sourceContext, JsValue sourceUrl, JsParseScriptAttributes parseAttributes, out JsValue result);

        // TODO:
        // https://github.com/Microsoft/ChakraCore/issues/4324

    }
}
