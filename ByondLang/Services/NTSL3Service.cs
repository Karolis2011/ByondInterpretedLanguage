using ByondLang.Api;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ByondLang.Services
{
    public class NTSL3Service
    {
        private readonly Dictionary<int, State.IExecutionContext> programs = new Dictionary<int, State.IExecutionContext>();
        private int lastId = 1;
        public IConfiguration _config;
        private IServiceProvider serviceProvider;
        private int nextPort = 10000;
        private readonly Queue<State.IExecutionContext> recycledPrograms = new Queue<State.IExecutionContext>();

        public NTSL3Service(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            this.serviceProvider = serviceProvider;
            _config = configuration;
        }

        internal void Reset()
        {
            foreach (var program in programs)
            {
                _ = recycle(program.Value);
            }
            programs.Clear();
            lastId = 1;
            nextPort = 10000;
        }

        private int GenerateNewId() => lastId++;
        private int GenerateNewPort() => nextPort++;

        public async Task Execute(int id, string code)
        {
            await GetProgram(id).ExecuteScript(code);
        }

        public State.IExecutionContext GetProgram(int id)
        {
            if (!programs.ContainsKey(id))
                throw new ArgumentException("Provided ID is not found.");

            var p = programs[id];
            return p;
        }

        private State.IExecutionContext obtainNewProgram()
        {
            if(recycledPrograms.Count > 0)
            {
                var p = recycledPrograms.Dequeue();
                p.Start(GenerateNewPort, serviceProvider);
                return p;
            } else
            {
                State.IExecutionContext p;
                if (_config.GetValue("inProcess", false))
                    p = new State.LocalExecutionContext();
                else
                    p = new State.RemoteExecutionContext();
                p.Start(GenerateNewPort, serviceProvider);
                return p;
            }
        }

        internal int NewProgram(ProgramType programType)
        {
            var program = obtainNewProgram();
            var id = GenerateNewId();
            programs.Add(id, program);
            program.InitializeProgram(programType);
            return id;
        }

        internal void Remove(int id)
        {
            var p = GetProgram(id);
            programs.Remove(id);

            _ = recycle(p);
        }

        private async Task recycle(State.IExecutionContext program)
        {
            bool result = await program.Recycle();
            if(result)
            {
                recycledPrograms.Enqueue(program);
            } else
            {
                program.Dispose();
            }
        }
    }
}
