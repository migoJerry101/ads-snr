using ads.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
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

        [HttpPost]
        [Route("GenerateChainReport")]
        public async Task<ActionResult> GenerateChainReport(DateTime startDate, DateTime endDate, IEnumerable<int> skus)
        {
            var reportDtos = await _totalAdsChain.GenerateAdsChainReportDto(startDate, endDate, skus);
            var report = _excel.ExportDataToExcelByDate(reportDtos);

            var file = File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AdsReport.xlsx");

            return file;
        }
    }
}
