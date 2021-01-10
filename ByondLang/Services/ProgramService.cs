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
        NTSL3StateService _state;

        public ProgramService(NTSL3StateService stateService)
        {
            _state = stateService;
        }

        internal async Task NewProgram<T>(Func<Runtime, JsContext, ChakraCore.TypeMapper, T> initializer) where T : BaseProgram
        {
            _state.program = await _state.runtime.BuildContext(initializer);
        }

        public override async Task<StatusResponse> status(StatusRequest request, ServerCallContext context)
        {
            if(_state.program == null)
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
                Type = _state.type
            };
            if(_state.program is ComputerProgram computerProgram)
            {
                response.Terminal = new TerminalState() {
                    Buffer = computerProgram.GetTerminalBuffer()
                };   
            }
            return response;
        }

        public override async Task<VoidMessage> execute(ExecuteRequest request, ServerCallContext context)
        {
            await _state.program?.ExecuteScript(request.Code);
            return new VoidMessage();
        }

        public override Task<VoidMessage> handleTopic(TopicRequest request, ServerCallContext context)
        {
            if(_state.program is ComputerProgram computerProgram)
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
            _state.program?.Dispose();
            _state.program = null;
            return Task.FromResult(request);
        }

        public void FullDispose()
        {
            _state.Dispose();
            _state = null;
        }
    }
}
