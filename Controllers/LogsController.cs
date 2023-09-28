using ads.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class LogsController : ControllerBase
    {
        private readonly ILogs _logs;

        public LogsController(ILogs logs) { _logs = logs; }


        [HttpGet]
        [Route("SelectLastLogs")]
        public IActionResult Index()
        {
            var log = _logs.SelectLastLogs();

            return Ok(log);
        }
    }
}
