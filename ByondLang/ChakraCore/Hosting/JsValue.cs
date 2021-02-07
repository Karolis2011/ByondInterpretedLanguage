using System;
using System.Runtime.InteropServices;

namespace ByondLang.ChakraCore.Hosting
{
    /// <summary>
    ///     A JavaScript value.
    /// </summary>
    /// <remarks>
    ///     A JavaScript value is one of the following types of values: Undefined, Null, Boolean, 
    ///     String, Number, or Object.
    /// </remarks>
    public struct JsValue
    {
        /// <summary>
        /// The reference.
        /// </summary>
        private readonly IntPtr reference;

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsValue"/> struct.
        /// </summary>
        /// <param name="reference">The reference.</param>
        private JsValue(IntPtr reference) => this.reference = reference;

        /// <summary>
        ///     Gets an invalid value.
        /// </summary>
        public static JsValue Invalid => new JsValue(IntPtr.Zero);

        /// <summary>
        ///     Gets the value of <c>undefined</c> in the current script context.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public static JsValue Undefined
        {
            get
            {
                Native.ThrowIfError(Native.JsGetUndefinedValue(out JsValue value));
                return value;
            }
        }

        /// <summary>
        ///     Gets the value of <c>null</c> in the current script context.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public static JsValue Null
        {
            get
            {
                Native.ThrowIfError(Native.JsGetNullValue(out JsValue value));
                return value;
            }
        }

        /// <summary>
        ///     Gets the value of <c>true</c> in the current script context.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public static JsValue True
        {
            get
            {
                Native.ThrowIfError(Native.JsGetTrueValue(out JsValue value));
                return value;
            }
        }

        /// <summary>
        ///     Gets the value of <c>false</c> in the current script context.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public static JsValue False
        {
            get
            {
                Native.ThrowIfError(Native.JsGetFalseValue(out JsValue value));
                return value;
            }
        }

        /// <summary>
        ///     Gets the global object in the current script context.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public static JsValue GlobalObject
        {
            get
            {
                Native.ThrowIfError(Native.JsGetGlobalObject(out JsValue value));
                return value;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the value is valid.
        /// </summary>
        public bool IsValid
        {
            get { return reference != IntPtr.Zero; }
        }

        /// <summary>
        ///     Gets the JavaScript type of the value.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>The type of the value.</returns>
        public JsValueType ValueType
        {
            get
            {
                Native.ThrowIfError(Native.JsGetValueType(this, out JsValueType type));
                return type;
            }
        }

        /// <summary>
        ///     Gets the length of a <c>String</c> value.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>The length of the string.</returns>
        public int StringLength
        {
            get
            {
                Native.ThrowIfError(Native.JsGetStringLength(this, out int length));
                return length;
            }
        }

        /// <summary>
        ///     Gets or sets the prototype of an object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public JsValue Prototype
        {
            get
            {
                Native.ThrowIfError(Native.JsGetPrototype(this, out JsValue prototypeReference));
                return prototypeReference;
            }

            set
            {
                Native.ThrowIfError(Native.JsSetPrototype(this, value));
            }
        }

        /// <summary>
        ///     Gets a value indicating whether an object is extensible or not.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public bool IsExtensionAllowed
        {
            get
            {
                Native.ThrowIfError(Native.JsGetExtensionAllowed(this, out bool allowed));
                return allowed;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether an object is an external object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public bool HasExternalData
        {
            get
            {
                Native.ThrowIfError(Native.JsHasExternalData(this, out bool hasExternalData));
                return hasExternalData;
            }
        }

        /// <summary>
        ///     Gets or sets the data in an external object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public IntPtr ExternalData
        {
            get
            {
                Native.ThrowIfError(Native.JsGetExternalData(this, out IntPtr data));
                return data;
            }

            set
            {
                Native.ThrowIfError(Native.JsSetExternalData(this, value));
            }
        }

        /// <summary>
        ///     Creates a <c>Boolean</c> value from a <c>bool</c> value.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="value">The value to be converted.</param>
        /// <returns>The converted value.</returns>
        public static JsValue FromBoolean(bool value)
        {
            Native.ThrowIfError(Native.JsBoolToBoolean(value, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a <c>Number</c> value from a <c>double</c> value.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="value">The value to be converted.</param>
        /// <returns>The new <c>Number</c> value.</returns>
        public static JsValue FromDouble(double value)
        {
            Native.ThrowIfError(Native.JsDoubleToNumber(value, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a <c>Number</c> value from a <c>int</c> value.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="value">The value to be converted.</param>
        /// <returns>The new <c>Number</c> value.</returns>
        public static JsValue FromInt32(int value)
        {
            Native.ThrowIfError(Native.JsIntToNumber(value, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a <c>String</c> value from a string pointer.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="value">The string  to convert to a <c>String</c> value.</param>
        /// <returns>The new <c>String</c> value.</returns>
        public static JsValue FromString(string value)
        {
            Native.ThrowIfError(Native.JsPointerToString(value, new UIntPtr((uint)value.Length), out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new <c>Object</c>.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>The new <c>Object</c>.</returns>
        public static JsValue CreateObject()
        {
            Native.ThrowIfError(Native.JsCreateObject(out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new <c>Object</c> that stores some external data.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="data">External data that the object will represent. May be null.</param>
        /// <param name="finalizer">
        ///     A callback for when the object is finalized. May be null.
        /// </param>
        /// <returns>The new <c>Object</c>.</returns>
        public static JsValue CreateExternalObject(IntPtr data, JsFinalizeCallback finalizer)
        {
            Native.ThrowIfError(Native.JsCreateExternalObject(data, finalizer, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript function.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="function">The method to call when the function is invoked.</param>
        /// <returns>The new function object.</returns>
        public static JsValue CreateFunction(JsNativeFunction function)
        {
            Native.ThrowIfError(Native.JsCreateFunction(function, IntPtr.Zero, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript function.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="function">The method to call when the function is invoked.</param>
        /// <param name="callbackData">Data to be provided to all function callbacks.</param>
        /// <returns>The new function object.</returns>
        public static JsValue CreateFunction(JsNativeFunction function, IntPtr callbackData)
        {
            Native.ThrowIfError(Native.JsCreateFunction(function, callbackData, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript named function.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="name">Nane for this function</param>
        /// <param name="function">The method to call when the function is invoked.</param>
        /// <returns>The new function object.</returns>
        public static JsValue CreateNamedFunction(JsValue name, JsNativeFunction function)
        {
            Native.ThrowIfError(Native.JsCreateNamedFunction(name, function, IntPtr.Zero, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript named function.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="name">Nane for this function</param>
        /// <param name="function">The method to call when the function is invoked.</param>
        /// <returns>The new function object.</returns>
        public static JsValue CreateNamedFunction(string name, JsNativeFunction function)
        {
            return CreateNamedFunction(FromString(name), function);
        }

        /// <summary>
        ///     Creates a new JavaScript named function.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="name">Nane for this function</param>
        /// <param name="function">The method to call when the function is invoked.</param>
        /// <param name="callbackData">Data to be provided to all function callbacks.</param>
        /// <returns>The new function object.</returns>
        public static JsValue CreateNamedFunction(JsValue name, JsNativeFunction function, IntPtr callbackData)
        {
            Native.ThrowIfError(Native.JsCreateNamedFunction(name, function, callbackData, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript named function.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="name">Nane for this function</param>
        /// <param name="function">The method to call when the function is invoked.</param>
        /// <param name="callbackData">Data to be provided to all function callbacks.</param>
        /// <returns>The new function object.</returns>
        public static JsValue CreateNamedFunction(string name, JsNativeFunction function, IntPtr callbackData)
        {
            return CreateNamedFunction(FromString(name), function, callbackData);
        }


        /// <summary>
        ///     Creates a JavaScript array object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="length">The initial length of the array.</param>
        /// <returns>The new array object.</returns>
        public static JsValue CreateArray(uint length)
        {
            Native.ThrowIfError(Native.JsCreateArray(length, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript error object
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="message">Message for the error object.</param>
        /// <returns>The new error object.</returns>
        public static JsValue CreateError(JsValue message)
        {
            Native.ThrowIfError(Native.JsCreateError(message, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript RangeError error object
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="message">Message for the error object.</param>
        /// <returns>The new error object.</returns>
        public static JsValue CreateRangeError(JsValue message)
        {
            Native.ThrowIfError(Native.JsCreateRangeError(message, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript ReferenceError error object
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="message">Message for the error object.</param>
        /// <returns>The new error object.</returns>
        public static JsValue CreateReferenceError(JsValue message)
        {
            Native.ThrowIfError(Native.JsCreateReferenceError(message, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript SyntaxError error object
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="message">Message for the error object.</param>
        /// <returns>The new error object.</returns>
        public static JsValue CreateSyntaxError(JsValue message)
        {
            Native.ThrowIfError(Native.JsCreateSyntaxError(message, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript TypeError error object
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="message">Message for the error object.</param>
        /// <returns>The new error object.</returns>
        public static JsValue CreateTypeError(JsValue message)
        {
            Native.ThrowIfError(Native.JsCreateTypeError(message, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript URIError error object
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="message">Message for the error object.</param>
        /// <returns>The new error object.</returns>
        public static JsValue CreateUriError(JsValue message)
        {
            Native.ThrowIfError(Native.JsCreateURIError(message, out JsValue reference));
            return reference;
        }

        /// <summary>
        ///     Creates a new JavaScript URIError error object
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="message">Message for the error object.</param>
        /// <returns>The new error object.</returns>
        public static JsValue CreatePromise(out JsValue resolveFunction, out JsValue rejectFunction)
        {
            Native.ThrowIfError(Native.JsCreatePromise(out JsValue promise, out resolveFunction, out rejectFunction));
            return promise;
        }

        /// <summary>
        ///     Adds a reference to the object.
        /// </summary>
        /// <remarks>
        ///     This only needs to be called on objects that are not going to be stored somewhere on 
        ///     the stack. Calling AddRef ensures that the JavaScript object the value refers to will not be freed 
        ///     until Release is called
        /// </remarks>
        /// <returns>The object's new reference count.</returns>
        public uint AddRef()
        {
            Native.ThrowIfError(Native.JsAddRef(this, out uint count));
            return count;
        }

        /// <summary>
        ///     Releases a reference to the object.
        /// </summary>
        /// <remarks>
        ///     Removes a reference that was created by AddRef.
        /// </remarks>
        /// <returns>The object's new reference count.</returns>
        public uint Release()
        {
            Native.ThrowIfError(Native.JsRelease(this, out uint count));
            return count;
        }

        /// <summary>
        ///     Retrieves the <c>bool</c> value of a <c>Boolean</c> value.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>The converted value.</returns>
        public bool ToBoolean()
        {
            Native.ThrowIfError(Native.JsBooleanToBool(this, out bool value));
            return value;
        }

        /// <summary>
        ///     Retrieves the <c>double</c> value of a <c>Number</c> value.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     This function retrieves the value of a Number value. It will fail with 
        ///     <c>InvalidArgument</c> if the type of the value is not <c>Number</c>.
        ///     </para>
        ///     <para>
        ///     Requires an active script context.
        ///     </para>
        /// </remarks>
        /// <returns>The <c>double</c> value.</returns>
        public double ToDouble()
        {
            Native.ThrowIfError(Native.JsNumberToDouble(this, out double value));
            return value;
        }

        /// <summary>
        ///     Retrieves the <c>int</c> value of a <c>Number</c> value.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     This function retrieves the value of a Number value. It will fail with
        ///     <c>InvalidArgument</c> if the type of the value is not <c>Number</c>.
        ///     </para>
        ///     <para>
        ///     Requires an active script context.
        ///     </para>
        /// </remarks>
        /// <returns>The <c>int</c> value.</returns>
        public int ToInt32()
        {
            Native.ThrowIfError(Native.JsNumberToInt(this, out int value));
            return value;
        }

        /// <summary>
        ///     Retrieves the string pointer of a <c>String</c> value.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     This function retrieves the string pointer of a <c>String</c> value. It will fail with 
        ///     <c>InvalidArgument</c> if the type of the value is not <c>String</c>.
        ///     </para>
        ///     <para>
        ///     Requires an active script context.
        ///     </para>
        /// </remarks>
        /// <returns>The string.</returns>
        public new string ToString()
        {
            Native.ThrowIfError(Native.JsStringToPointer(this, out IntPtr buffer, out UIntPtr length));
            return Marshal.PtrToStringUni(buffer, (int)length);
        }

        /// <summary>
        ///     Converts the value to <c>Boolean</c> using regular JavaScript semantics.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>The converted value.</returns>
        public JsValue ConvertToBoolean()
        {
            Native.ThrowIfError(Native.JsConvertValueToBoolean(this, out JsValue booleanReference));
            return booleanReference;
        }

        /// <summary>
        ///     Converts the value to <c>Number</c> using regular JavaScript semantics.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>The converted value.</returns>
        public JsValue ConvertToNumber()
        {
            Native.ThrowIfError(Native.JsConvertValueToNumber(this, out JsValue numberReference));
            return numberReference;
        }

        /// <summary>
        ///     Converts the value to <c>String</c> using regular JavaScript semantics.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>The converted value.</returns>
        public JsValue ConvertToString()
        {
            Native.ThrowIfError(Native.JsConvertValueToString(this, out JsValue stringReference));
            return stringReference;
        }

        /// <summary>
        ///     Converts the value to <c>Object</c> using regular JavaScript semantics.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>The converted value.</returns>
        public JsValue ConvertToObject()
        {
            Native.ThrowIfError(Native.JsConvertValueToObject(this, out JsValue objectReference));
            return objectReference;
        }


        /// <summary>
        ///     Gets an object's property.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="id">The ID of the property.</param>
        /// <returns>The value of the property.</returns>
        public JsValue GetProperty(string id)
        {
            return GetProperty(JsPropertyId.FromString(id));
        }

        /// <summary>
        ///     Sets an object to not be extensible.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        public void PreventExtension()
        {
            Native.ThrowIfError(Native.JsPreventExtension(this));
        }

        /// <summary>
        ///     Gets a property descriptor for an object's own property.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="propertyId">The ID of the property.</param>
        /// <returns>The property descriptor.</returns>
        public JsValue GetOwnPropertyDescriptor(JsPropertyId propertyId)
        {
            Native.ThrowIfError(Native.JsGetOwnPropertyDescriptor(this, propertyId, out JsValue descriptorReference));
            return descriptorReference;
        }

        /// <summary>
        ///     Gets the list of all properties on the object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <returns>An array of property names.</returns>
        public JsValue GetOwnPropertyNames()
        {
            Native.ThrowIfError(Native.JsGetOwnPropertyNames(this, out JsValue propertyNamesReference));
            return propertyNamesReference;
        }

        /// <summary>
        ///     Determines whether an object has a property.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="propertyId">The ID of the property.</param>
        /// <returns>Whether the object (or a prototype) has the property.</returns>
        public bool HasProperty(JsPropertyId propertyId)
        {
            Native.ThrowIfError(Native.JsHasProperty(this, propertyId, out bool hasProperty));
            return hasProperty;
        }

        /// <summary>
        ///     Gets an object's property.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="id">The ID of the property.</param>
        /// <returns>The value of the property.</returns>
        public JsValue GetProperty(JsPropertyId id)
        {
            Native.ThrowIfError(Native.JsGetProperty(this, id, out JsValue propertyReference));
            return propertyReference;
        }

        /// <summary>
        ///     Sets an object's property.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="id">The ID of the property.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="useStrictRules">The property set should follow strict mode rules.</param>
        public void SetProperty(JsPropertyId id, JsValue value, bool useStrictRules)
        {
            Native.ThrowIfError(Native.JsSetProperty(this, id, value, useStrictRules));
        }

        /// <summary>
        ///     Sets an object's property.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="id">The ID of the property.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="useStrictRules">The property set should follow strict mode rules.</param>
        public void SetProperty(string id, JsValue value, bool useStrictRules)
        {
            SetProperty(JsPropertyId.FromString(id), value, useStrictRules);
        }

        /// <summary>
        ///     Deletes an object's property.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="useStrictRules">The property set should follow strict mode rules.</param>
        /// <returns>Whether the property was deleted.</returns>
        public JsValue DeleteProperty(JsPropertyId propertyId, bool useStrictRules)
        {
            Native.ThrowIfError(Native.JsDeleteProperty(this, propertyId, useStrictRules, out JsValue returnReference));
            return returnReference;
        }

        /// <summary>
        ///     Defines a new object's own property from a property descriptor.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="propertyDescriptor">The property descriptor.</param>
        /// <returns>Whether the property was defined.</returns>
        public bool DefineProperty(JsPropertyId propertyId, JsValue propertyDescriptor)
        {
            Native.ThrowIfError(Native.JsDefineProperty(this, propertyId, propertyDescriptor, out bool result));
            return result;
        }

        /// <summary>
        ///     Defines a new object's own property from a property descriptor.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="propertyDescriptor">The property descriptor.</param>
        /// <returns>Whether the property was defined.</returns>
        public bool DefineProperty(string propertyId, JsValue propertyDescriptor)
        {
            return DefineProperty(JsPropertyId.FromString(propertyId), propertyDescriptor);
        }

        /// <summary>
        ///     Test if an object has a value at the specified index.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="index">The index to test.</param>
        /// <returns>Whether the object has an value at the specified index.</returns>
        public bool HasIndexedProperty(JsValue index)
        {
            Native.ThrowIfError(Native.JsHasIndexedProperty(this, index, out bool hasProperty));
            return hasProperty;
        }

        /// <summary>
        ///     Retrieve the value at the specified index of an object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="index">The index to retrieve.</param>
        /// <returns>The retrieved value.</returns>
        public JsValue GetIndexedProperty(JsValue index)
        {
            Native.ThrowIfError(Native.JsGetIndexedProperty(this, index, out JsValue propertyReference));
            return propertyReference;
        }

        /// <summary>
        ///     Set the value at the specified index of an object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="index">The index to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetIndexedProperty(JsValue index, JsValue value)
        {
            Native.ThrowIfError(Native.JsSetIndexedProperty(this, index, value));
        }

        /// <summary>
        ///     Set the value at the specified index of an object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="index">The index to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetIndexedProperty(int index, JsValue value)
        {
            SetIndexedProperty(FromInt32(index), value);
        }

        /// <summary>
        ///     Delete the value at the specified index of an object.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="index">The index to delete.</param>
        public void DeleteIndexedProperty(JsValue index)
        {
            Native.ThrowIfError(Native.JsDeleteIndexedProperty(this, index));
        }

        /// <summary>
        ///     Compare two JavaScript values for equality.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     This function is equivalent to the "==" operator in JavaScript.
        ///     </para>
        ///     <para>
        ///     Requires an active script context.
        ///     </para>
        /// </remarks>
        /// <param name="other">The object to compare.</param>
        /// <returns>Whether the values are equal.</returns>
        public bool Equals(JsValue other)
        {
            Native.ThrowIfError(Native.JsEquals(this, other, out bool equals));
            return equals;
        }

        /// <summary>
        ///     Compare two JavaScript values for strict equality.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     This function is equivalent to the "===" operator in JavaScript.
        ///     </para>
        ///     <para>
        ///     Requires an active script context.
        ///     </para>
        /// </remarks>
        /// <param name="other">The object to compare.</param>
        /// <returns>Whether the values are strictly equal.</returns>
        public bool StrictEquals(JsValue other)
        {
            Native.ThrowIfError(Native.JsStrictEquals(this, other, out bool equals));
            return equals;
        }

        /// <summary>
        ///     Invokes a function.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="arguments">The arguments to the call.</param>
        /// <returns>The <c>Value</c> returned from the function invocation, if any.</returns>
        public JsValue CallFunction(params JsValue[] arguments)
        {

            if (arguments.Length > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(arguments));
            }

            Native.ThrowIfError(Native.JsCallFunction(this, arguments, (ushort)arguments.Length, out JsValue returnReference));
            return returnReference;
        }

        /// <summary>
        ///     Invokes a function as a constructor.
        /// </summary>
        /// <remarks>
        ///     Requires an active script context.
        /// </remarks>
        /// <param name="arguments">The arguments to the call.</param>
        /// <returns>The <c>Value</c> returned from the function invocation.</returns>
        public JsValue ConstructObject(params JsValue[] arguments)
        {
            if (arguments.Length > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(arguments));
            }
            Native.ThrowIfError(Native.JsConstructObject(this, arguments, (ushort)arguments.Length, out JsValue returnReference));
            return returnReference;
        }


        public static Func<JsValue, string> JsonStringify()
        {
            var glob = GlobalObject;
            var func = glob.GetProperty("JSON").GetProperty("stringify");
            return (val) =>
            {
                var res = func.CallFunction(glob, val);
                return res.ToString();
            };
        }
    }
}