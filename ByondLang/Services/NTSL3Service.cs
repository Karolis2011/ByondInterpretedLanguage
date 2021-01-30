using ByondLang.ChakraCore.Hosting;
using ByondLang.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Services
{
    public class NTSL3Service
    {
        Runtime runtime;
        private readonly Dictionary<int, BaseProgram> programs = new Dictionary<int, BaseProgram>();
        private int lastId = 1;
        public IServiceProvider serviceProvider; 

        public NTSL3Service(IServiceProvider services)
        {
            serviceProvider = services;
            runtime = new Runtime(this);
        }

        ~NTSL3Service()
        {
            foreach (var program in programs)
            {
                _ = recycle(program.Value);
            }
            runtime.Dispose();
        }

        internal void Reset()
        {
            runtime.Dispose();
            runtime = new Runtime(this);
            programs.Clear();
            lastId = 1;
        }

        private int GenerateNewId()
        {
            return lastId++;
        }

        internal async Task<int> NewProgram<T>(Func<Runtime, JsContext, ChakraCore.TypeMapper, T> initializer) where T : BaseProgram
        {
            var id = GenerateNewId();
            var p = await runtime.BuildContext(initializer);
            programs[id] = p;
            return id;
        }

        internal void Execute(int id, string code)
        {
            if (!programs.ContainsKey(id))
                throw new ArgumentException("Provided ID is not found.");

            GetProgram(id).ExecuteScript(code);
        }

        internal T GetProgram<T>(int id) where T : BaseProgram
        {
            if (!programs.ContainsKey(id))
                throw new ArgumentException("Provided ID is not found.");

            var p = programs[id];
            if (!(p is T))
                throw new Exception("Invalid program Type.");
            return (T)p;
        }

        internal BaseProgram GetProgram(int id) => GetProgram<BaseProgram>(id);

        internal void Remove(int id)
        {
            var p = GetProgram(id);
            programs.Remove(id);

            _ = p.Recycle();
            recycledPrograms.Enqueue(p);
        }
    }
}
