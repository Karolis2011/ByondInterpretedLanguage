using ByondLang.Api;
using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Services
{
    public class NTSL3StateService: IDisposable
    {
        public ProgramType type = ProgramType.None;
        public BaseProgram program = null;
        public IServiceProvider serviceProvider = null;

        public NTSL3StateService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void Dispose()
        {
            program?.Dispose();
        }
    }
}
