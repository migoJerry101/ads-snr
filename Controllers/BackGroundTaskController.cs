using ads.Interface;
using ads.Utility;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class BackGroundTaskController : ControllerBase
    {
        private readonly IAdsBackGroundTask _adsBackGroundTask;

        public BackGroundTaskController(IAdsBackGroundTask adsBackGroundTask)
        { 
            _adsBackGroundTask = adsBackGroundTask;
        }


        [HttpGet]
        [Route("BGTask")]
        public async Task<IActionResult> Index()
        {
            var import = await _adsBackGroundTask.ExecuteTask();

            return Ok(new { successfull = "Success" });
        }
    }
}
