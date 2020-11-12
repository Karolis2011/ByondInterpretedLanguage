using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class JsCallableAttribute : Attribute
    {
        public JsCallableAttribute()
        {
        }
        public JsCallableAttribute(string functionName)
        {
            FunctionName = functionName;
        }

        public string FunctionName { get; set; } = null;
    }
}
