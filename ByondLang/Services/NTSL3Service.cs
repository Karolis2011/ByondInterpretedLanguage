using ByondLang.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ByondLang.Services
{
    public class NTSL3Service
    {
        private readonly Dictionary<int, BaseProgram> programs = new Dictionary<int, BaseProgram>();
        private int lastId = 1;
        public IConfiguration _config;
        private IServiceProvider serviceProvider;

        public NTSL3Service(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            this.serviceProvider = serviceProvider;
            _config = configuration;
        }

        internal void Reset()
        {
            foreach (var program in programs)
            {
                program.Value.Dispose();
            }
            programs.Clear();
            lastId = 1;
        }

        private int GenerateNewId() => lastId++;

        public async Task Execute(int id, string code)
        {
            await GetProgram(id).ExecuteScript(code);
        }

        public BaseProgram GetProgram(int id) => GetProgram<BaseProgram>(id);

        public PType GetProgram<PType>(int id) where PType : BaseProgram
        {
            if (!programs.ContainsKey(id))
                throw new ArgumentException("Provided ID is not found.");

            var p = programs[id];
            if (p is PType program)
                return program;
            else
                throw new Exception("Tried to get wrong type of program.");
        }

        internal int NewProgram<PType>(Func<IServiceProvider, PType> constructor) where PType : BaseProgram
        {
            if (programs.Count > _config.GetValue("MaxExecutors", 50))
                throw new Exception("Too many programs.");
            var program = constructor(serviceProvider);
            var id = GenerateNewId();
            programs.Add(id, program);
            _ = program.InitializeState();

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
