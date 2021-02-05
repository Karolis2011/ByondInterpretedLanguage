using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Api.Attributes
{
    /// <summary>
    /// Defines class as mappable to Js Object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class JsObjectAttribute : Attribute
    {
        public bool Freeze { get; set; }
    }
}
