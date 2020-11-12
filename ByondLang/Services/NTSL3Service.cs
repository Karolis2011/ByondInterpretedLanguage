using ByondLang.Api;
using ByondLang.ChakraCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Services
{
    public class NTSL3Service
    {
        private readonly Dictionary<int, State.Program> programs = new Dictionary<int, State.Program>();
        private int lastId = 1;
        public IServiceProvider serviceProvider;
        private int nextPort = 10000;

        public NTSL3Service(IServiceProvider services)
        {
            serviceProvider = services;
        }

        internal void Reset()
        {
            foreach (var program in programs)
            {
                program.Value.Dispose();
            }
            programs.Clear();
            lastId = 1;
            nextPort = 10000;
        }

        private int GenerateNewId()
        {
            return lastId++;
        }

        internal async Task Execute(int id, string code)
        {
            await GetProgram(id).ExecuteScript(code);
        }

        internal State.Program GetProgram(int id)
        {
            if (!programs.ContainsKey(id))
                throw new ArgumentException("Provided ID is not found.");

            var p = programs[id];
            return p;
        }

        internal int NewProgram(ProgramType programType)
        {
            var program = new State.Program();
            var id = GenerateNewId();
            program.Spawn(nextPort++);
            programs.Add(id, program);
            _ = program.InitializeProgram(programType);
            return id;
        }

        internal void Remove(int id)
        {
            var p = GetProgram(id);
            programs.Remove(id);
            p.Dispose();
        }
    }
}
