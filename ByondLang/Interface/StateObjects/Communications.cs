using ByondLang.ChakraCore.Hosting;
using ByondLang.ChakraCore.Reflection;
using ByondLang.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Interface.StateObjects
{
    [JsObject]
    public class Communications
    {
        private NTSL3Service service;


        public Communications(NTSL3Service service)
        {
            this.service = service;
        }

        [JsCallable]
        public void setSignalHandler(JsValue handler)
        {
            throw new NotImplementedException();
        }
    }
}
