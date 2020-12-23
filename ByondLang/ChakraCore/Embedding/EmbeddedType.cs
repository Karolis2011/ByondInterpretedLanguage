﻿using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;

namespace ByondLang.ChakraCore.Embedding
{
	/// <summary>
	/// Embedded type
	/// </summary>
	internal sealed class EmbeddedType : EmbeddedItem
	{
		/// <summary>
		/// Constructs an instance of the embedded type
		/// </summary>
		/// <param name="hostType">Host type</param>
		/// <param name="scriptValue">JavaScript value created from an host type</param>
		public EmbeddedType(Type hostType, JsValueRaw scriptValue)
			: base(hostType, null, scriptValue, new List<JsNativeFunction>())
		{ }

		/// <summary>
		/// Constructs an instance of the embedded type
		/// </summary>
		/// <param name="hostType">Host type</param>
		/// <param name="scriptValue">JavaScript value created from an host type</param>
		/// <param name="nativeFunctions">List of native functions, that used to access to members of type</param>
		public EmbeddedType(Type hostType, JsValueRaw scriptValue, IList<JsNativeFunction> nativeFunctions)
			: base(hostType, null, scriptValue, nativeFunctions)
		{ }

		#region EmbeddedItem overrides

		/// <summary>
		/// Gets a value that indicates if the host item is an instance
		/// </summary>
		public override bool IsInstance
		{
			get { return false; }
		}

		#endregion
	}
}