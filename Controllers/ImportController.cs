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
        private readonly IOpenQuery _openQuery;
        private readonly IInventoryBackup _inventoryBackup;

        public ImportController(IInvetory invetory, ISales sales, IAds ads, IOpenQuery openQuery, IInventoryBackup inventoryBackup)
        {
            _invetory = invetory;
            _sales = sales;
            _ads = ads;
            _openQuery = openQuery;
            _inventoryBackup = inventoryBackup;
        }

        [HttpPost]
        [Route("ImportInventoryAndSales")]
        public async Task<IActionResult> Import(List<string> dates)
        {
            //foreach (var item in dates)
            //{
            //    await _invetory.GetInventoryAsync(item, item);
            //     await _sales.GetSalesAsync(item, item);

            //}

            await _ads.ComputeAds();

            return Ok();
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

        [HttpPost]
        [Route("Computation")]
        public async Task<IActionResult> GetComputation(string start)
        {
            var computation = await _ads.GetComputation(start);

            return Ok(computation);
        }

        [HttpPost]
        [Route("ImportClubs")]
        public async Task<IActionResult> ImportClubs()
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                await _openQuery.ImportItems(db);

                return Ok();
            }
        }

        [HttpPost]
        [Route("ImportInventoryBackUp")]
        public async Task<IActionResult> ImportInventoryBackUp()
        {
            //await _inventoryBackup.ManualImport();

            return Ok();
        }
    }
}
