using ads.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    public class TotalAdsChainController : ControllerBase
    {
        private readonly ITotalAdsChain _totalAdsChain;
        private readonly IExcel _excel;

        public TotalAdsChainController(ITotalAdsChain totalAdsChain, IExcel excel)
        {
            _totalAdsChain = totalAdsChain;
            _excel = excel;
        }

        [HttpGet]
        [Route("GetTotalAdsChain")]
        public IActionResult GetTotalAdsChain()
        {
            var chain = _totalAdsChain.GetTotalAdsChain();

            return Ok(chain);
        }

        //[HttpGet]
        //[Route("GenerateChainReport")]
        //public IActionResult GenerateChainReport()
        //{

        //    var report = _excel.ExportDataToExcelByDate();

        //    var file = File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "IFILE.xlsx");

        //    return file;
        //}
    }
}
