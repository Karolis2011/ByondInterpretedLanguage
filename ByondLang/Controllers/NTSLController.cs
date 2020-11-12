using System;
using System.Threading.Tasks;
using ByondLang.Models;
using ByondLang.Models.Request;
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
            return _newService.NewProgram(PTypeToPType(type));
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
        public async Task<string> GetBuffer([FromQuery] int id)
        {
            return await _newService.GetProgram(id).GetBuffer();
        }

        [HttpGet("/computer/topic")]
        public async Task<int> TopicCall([FromQuery] int id, [FromQuery] string topic = "", [FromQuery] string data = "")
        {
            var program = _newService.GetProgram(id);
            await program.HandleTopic(topic, data);
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

        private Api.ProgramType PTypeToPType(ProgramType type)
        {
            return type switch
            {
                ProgramType.Computer => Api.ProgramType.ComputerProgram,
                ProgramType.TCom => Api.ProgramType.None, // TODO change this
                _ => Api.ProgramType.None,
            };
        }
    }
}
