using ads.Data;
using ads.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class ImportController : ControllerBase
    {
        private readonly IInvetory _invetory;
        private readonly ISales _sales;
        private readonly IAds _ads;

        public ImportController(IInvetory invetory, ISales sales, IAds ads)
        {
            _invetory = invetory;
            _sales = sales;
            _ads = ads;
        }

        [HttpPost]
        [Route("GetInventory")]
        public async Task<IActionResult> GetInventory(string dates)
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                //list of Inventory within 56 days in Local DB
                var listInventoryResult = await _invetory.ListInv(dates, db);

                return Ok(new { listInventoryResult });
            }
        }

        [HttpPost]
        [Route("GetSales")]
        public async Task<IActionResult> GetSales(string dates)
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                var listOfSales = await _sales.ListSales(dates, db);

                return Ok(new { listOfSales });
            }
        }
    }
}
