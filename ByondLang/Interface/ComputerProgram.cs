using ByondLang.ChakraCore.Hosting;
using ByondLang.Interface.StateObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Interface
{
    public class ComputerProgram : BaseProgram
    {
        private Terminal terminal;
        public string ComputerRef { get; private set; }

        public ComputerProgram(Runtime runtime, JsContext context, ChakraCore.TypeMapper typeMapper, string computerRef) : base(runtime, context, typeMapper)
        {
            ComputerRef = computerRef;
            terminal = new Terminal(computerRef);
        }

        public override void InstallInterfaces()
        {
            base.InstallInterfaces();
            // Install APIs: term
            var glob = JsValue.GlobalObject;
            glob.SetProperty("Term", terminal.GetRepresentation(_typeMapper), true);
        }
    }
}
