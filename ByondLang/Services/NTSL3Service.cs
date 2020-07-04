using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Services
{
    public class NTSL3Service
    {
        JsRuntime runtime;

        public NTSL3Service()
        {
            // Init runtime
            

            Native.JsSetPromiseContinuationCallback(PromiseHandler, IntPtr.Zero);
        }



        private void PromiseHandler(JsValue task, IntPtr callbackState)
        {
            task.AddRef();

            try
            {
                task.CallFunction(JsValue.GlobalObject);
            }
            finally
            {
                task.Release();
            }
        }


    }
}
