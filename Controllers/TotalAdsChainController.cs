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

        [HttpPost]
        [Route("GenerateChainReport")]
        public async Task<IActionResult> GenerateChainReport(DateTime startDate, DateTime endDate)
        {
            var reportDtos = await _totalAdsChain.GenerateAdsChainReportDto(startDate, endDate);
            var report = _excel.ExportDataToExcelByDate(reportDtos);

            var file = File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AdsChain-{startDate:M/d/yyyy}.xlsx");

            return file;
        }
    }
}
