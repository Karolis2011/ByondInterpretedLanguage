using ByondLang.ChakraCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.UnitTests.ChakraCore
{
    [JsObject]
    public class TMTestClass
    {
        [JsMapped]
        public int Flag { get; set; } = 0;


        [JsCallable]
        public bool TryTrue()
        {
            return true;
        }

        [JsCallable("false")]
        public bool TryFalse()
        {
            return false;
        }
    }
}
