using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.State
{
    public interface IExecutionContext : IDisposable
    {
        Task ExecuteScript(string script);
        Task InitializeProgram(Api.ProgramType type);
        Task<string> GetBuffer();
        void Start(Func<int> portGenerator, IServiceProvider serviceProvider);
        Task HandleTopic(string topic, string data);
        Task<bool> Recycle();
        Task SetDebuggingState(bool state);
    }
}
