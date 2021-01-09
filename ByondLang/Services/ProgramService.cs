using ByondLang.Api;
using ByondLang.ChakraCore.Hosting;
using ByondLang.Interface;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByondLang.Services
{
    public class ProgramService : Api.Program.ProgramBase
    {
        static ProgramType type = ProgramType.None;
        static BaseProgram program = null;
        static Runtime runtime;

        public ProgramService(IServiceProvider serviceProvider)
        {
            runtime = new Runtime(serviceProvider);
        }

        internal async Task NewProgram<T>(Func<Runtime, JsContext, ChakraCore.TypeMapper, T> initializer) where T : BaseProgram
        {
            program = await runtime.BuildContext(initializer);
        }

        public override async Task<StatusResponse> status(StatusRequest request, ServerCallContext context)
        {
            if(program == null)
            {
                if (request.Initialize)
                {
                    switch (request.Type)
                    {
                        case ProgramType.None:
                            throw new Exception("Unspecified program type");
                        case ProgramType.ComputerProgram:
                            await NewProgram((r, c, tm) => new ComputerProgram(r, c, tm));
                            break;
                        default:
                            break;
                    }
                } else {
                    return new StatusResponse()
                    {
                        Type = ProgramType.None
                    };
                }
            } 
            var response =  new StatusResponse()
            {
                Type = type
            };
            if(program is ComputerProgram computerProgram)
            {
                response.Terminal = new TerminalState() {
                    Buffer = computerProgram.GetTerminalBuffer()
                };   
            }
            return response;
        }

        public override async Task<VoidMessage> execute(ExecuteRequest request, ServerCallContext context)
        {
            await program?.ExecuteScript(request.Code);
            return new VoidMessage();
        }

        public override Task<VoidMessage> handleTopic(TopicRequest request, ServerCallContext context)
        {
            if(program is ComputerProgram computerProgram)
            {
                computerProgram.HandleTopic(request.TopicId, request.Data);
            }
            else
            {
                throw new Exception("Invalid type.");
            }
            return Task.FromResult(new VoidMessage());
        }

        public override Task<VoidMessage> recycle(VoidMessage request, ServerCallContext context)
        {
            program.Dispose();
            program = null;
            type = ProgramType.None;
            return Task.FromResult(request);
        }

        public void FullDispose()
        {
            program?.Dispose();
            runtime?.Dispose();
        }
    }
}
