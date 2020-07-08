using ByondLang.ChakraCore.Hosting.Helpers;
using ByondLang.ChakraCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ByondLang.ChakraCore.Embedding
{
    public class EmbeddingObjectOptions
    {
        public bool Freeze { get; set; }
        public bool ShouldMap { get; set; }

        public EmbeddingObjectOptions()
        {
            ShouldMap = true;
            Freeze = true;
        }

        public EmbeddingObjectOptions(Type typeToMap)
        {
            var attr = typeToMap.GetCustomAttribute<JsObjectAttribute>();
            if (attr != null)
            {
                ShouldMap = true;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Attempted to map forbidden Type.");
            }
        }

        public bool IsMapped(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<JsMappedAttribute>();
            if (attr != null)
                return true;
            return false;
        }

        public bool IsMapped(PropertyInfo property)
        {
            var attr = property.GetCustomAttribute<JsMappedAttribute>();
            if (attr != null)
                return true;
            return false;
        }

        public ExtendedMethodInfo ExtendInfo(MethodInfo info)
        {
            var ni = new ExtendedMethodInfo()
            {
                Info = info
            };
            if (!ReflectionHelpers.IsFullyFledgedMethod(info))
            {
                ni.IsMapped = false;
                return ni;
            }
            var attr = info.GetCustomAttribute<JsCallableAttribute>();
            if(attr == null)
            {
                ni.IsMapped = false;
                return ni;
            }
            ni.Name = attr.FunctionName;
            ni.IsMapped = true;
            return ni;
        } 


        public class ExtendedMethodInfo
        {
            public MethodInfo Info;
            private string _name = null;
            public bool IsMapped = false;

            public string Name
            {
                get
                {
                    if (_name == null)
                        return Info.Name;
                    else
                        return _name;
                }
                set { _name = value; }
            }

        }
    }
}
