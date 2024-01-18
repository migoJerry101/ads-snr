using ads.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class SalesController : Controller
    {
        private readonly ISales _sales;

        public SalesController(ISales sales)
        {
            _sales = sales;
        }

        [HttpPost]
        [Route("GetSalesByDateAndClub")]
        public async Task<IActionResult> GetSalesByDateAndClub(DateTime date)
        {
            var sales = await _sales.GetSalesByDateAndClub(date);
            sales = sales.OrderByDescending(x => x.Clubs).ToList();

            return Ok(sales);
        }
    }
}
