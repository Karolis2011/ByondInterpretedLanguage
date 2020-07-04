using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Reflection
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    sealed class JsCallableAttribute : Attribute
    {

        public JsCallableAttribute(string functionName)
        {
            FunctionName = functionName;
        }

        // This is a named argument
        public string FunctionName { get; set; }
    }
}
