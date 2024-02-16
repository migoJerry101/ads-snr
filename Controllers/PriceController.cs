using ads.Interface;
using ads.Utility;
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
        public async Task<IActionResult> GetHistoricalPriceFromCsv(string date)
        {
            var dateFormat = DateConvertion.ConvertStringDate(date);
            await _price.GetHistoricalPriceFromCsv(dateFormat);

            return Ok();
        }

        [HttpPost]
        [Route("GetHistoricalPriceByDatesFromCsv")]
        public async Task<IActionResult> GetHistoricalPriceByDatesFromCsv(List<string> dates)
        {
            foreach (var date in dates)
            {
                var dateFormat = DateConvertion.ConvertStringDate(date);
                await _price.GetHistoricalPriceFromCsv(dateFormat);
            }

            return Ok();
        }
    }
}
