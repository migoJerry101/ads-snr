using ads.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class ImportOldInventoryController : ControllerBase
    {

        private readonly IImportInventory _importInventory;
        private readonly ISales _sales;

        public ImportOldInventoryController(IImportInventory importInventory, ISales sales)
        {
            _importInventory = importInventory;
            _sales = sales;
        }

        [HttpPost]
        [Route("ImportInventory")]
        public async Task<IActionResult> Index(List<string> list)
        {

            foreach (var start in list)
            {
                var import = await _importInventory.GetInventory(start, start);
            }


            //string date1 = "230601";
            //string date2 = "230628";

            //var importSales = await _sales.GetSalesAsync(start, end);

            return Ok(new { successfull = list });
        }
    }
}
