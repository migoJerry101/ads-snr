using ads.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class DataController : ControllerBase
    {
        private readonly IData _data;
        public DataController(IData data)
        {
            _data = data;
        }

        [HttpGet]
        [Route("GetData")]
        public async Task<IActionResult> GetDataAsync(string start, string end)
        {
            var transformedData = await _data.GetDataAsync(start,end);
            return Ok(transformedData);
        }

        [HttpGet]
        [Route("GetInventory")]
        public async Task<IActionResult> GetInventoryAsync(string start, string end)
        {
            var transformedData = await _data.GetInventoryAsync(start, end);
            return Ok(transformedData);
        }

       
    }
}
