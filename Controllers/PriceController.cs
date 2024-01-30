using ads.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class PriceController : ControllerBase
    {
        private readonly IPrice _price;
        public PriceController(IPrice price)
        {
            _price = price;
        }

        [HttpPost]
        [Route("GetHistoricalPriceFromCsv")]
        public async Task<IActionResult> GetHistoricalPriceFromCsv(DateTime date)
        {
             await _price.GetHistoricalPriceFromCsv(date);

            return Ok();
        }
    }
}
