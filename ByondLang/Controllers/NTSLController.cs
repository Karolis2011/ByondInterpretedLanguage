using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ByondLang.Interface;
using ByondLang.Models;
using ByondLang.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ByondLang.Controllers
{
    [ApiController]
    public class NTSLController : ControllerBase
    {
        NTSL3Service _newService;
        ILogger _log;
        IConfiguration _config;
        public NTSLController(NTSL3Service newService, ILogger<NTSLController> logger, IConfiguration configuration)
        {
            _newService = newService;
            _log = logger;
            _config = configuration;
        }

        [HttpGet("/clear")]
        public int Clear()
        {
            _newService.Reset();
            return 1;
        }

        [HttpGet("/new_program")]
        public int NewProgram([FromQuery] ProgramType type)
        {
            switch (type)
            {
                case ProgramType.Computer:
                    return _newService.NewProgram((s) => new ComputerProgram(s));
                case ProgramType.TCom:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }

        [HttpGet("/execute")]
        public async Task<int> Execute([FromQuery] int id, [FromQuery] string code = "")
        {
            await _newService.Execute(id, code);
            return 1;
        }

        [HttpGet("/remove")]
        public int Remove([FromQuery] int id)
        {
            _newService.Remove(id);
            return 1;
        }



        [HttpGet("/computer/get_buffer")]
        public string GetBuffer([FromQuery] int id)
        {
            return _newService.GetProgram<ComputerProgram>(id).GetTerminalBuffer();
        }

        [HttpGet("/computer/topic")]
        public int TopicCall([FromQuery] int id, [FromQuery] string topic = "", [FromQuery] string data = "")
        {
            var program = _newService.GetProgram<ComputerProgram>(id);
            program.HandleTopic(topic, data);
            return 1;
        }

        [HttpGet("/debug/set")]
        public int DebugSet([FromQuery] int id, [FromQuery] int state)
        {
            var program = _newService.GetProgram(id);
            program.SetDebuggingState(Convert.ToBoolean(state));
            return 1;
        }

        /// <summary>
        /// Gets new events sent by 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("/events/get")]
        public IEnumerable<Event> EventsGet([FromQuery] int id)
        {
            var program = _newService.GetProgram(id);
            program.SetDebuggingState(Convert.ToBoolean(state));
            return 1;
        }

        /*

        [HttpPost("/tcom/process")]
        public async Task<int> ProcessSignal(TComProcessRequest request)
        {
            var program = _newService.GetProgram<TComProgram>(request.Id);
            await program.ProcessSignal(request.Signal);
            return 1;
        }

        [HttpGet("/tcom/get")]
        public TComSignal[] GetSignals([FromQuery] int id)
        {
            var program = _newService.GetProgram<TComProgram>(id);
            return program.GetSignals();
        }
        */

        public enum ProgramType
        {
            Computer,
            TCom
        }
    }
}
