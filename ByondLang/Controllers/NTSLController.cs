using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ByondLang.Interface;
using ByondLang.Language;
using ByondLang.Models;
using ByondLang.Models.Request;
using ByondLang.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ByondLang.Controllers
{
    [ApiController]
    public class NTSLController : ControllerBase
    {
        NTSL3Service _newService;
        ILogger _log;
        public NTSLController(NTSL3Service newService, ILogger<NTSLController> logger)
        {
            _newService = newService;
            _log = logger;
        }


        [HttpGet("/clear")]
        public int Clear()
        {
            _newService.Reset();
            return 1;
        }

        [HttpGet("/new_program")]
        public async Task<int> NewProgram([FromQuery] ProgramType type, [FromQuery(Name = "ref")] string computerRef = "")
        {
            switch (type)
            {
                case ProgramType.Computer:
                    return await _newService.NewProgram((r, c, m) => new ComputerProgram(r, c, m, computerRef));
                case ProgramType.TCom:
                    return await _newService.NewProgram((r, c, m) => new TComProgram(r, c, m));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [HttpGet("/execute")]
        public int Execute([FromQuery] int id, [FromQuery] string code = "")
        {
            _newService.Execute(id, code);
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
            var program = _newService.GetProgram<ComputerProgram>(id);
            return program.GetTerminalBuffer();
        }

        [HttpGet("/computer/topic")]
        public int TopicCall([FromQuery] int id, [FromQuery] string topic = "", [FromQuery] string data = "")
        {
            var program = _newService.GetProgram<ComputerProgram>(id);
            program.HandleTopic(topic, data);
            return 1;
        }

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

        public enum ProgramType
        {
            Computer,
            TCom
        }
    }
}
