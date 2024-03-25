using ads.Interface;
using ads.Models.Dto.AdsChain;
using ads.Utility;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class TotalAdsClubController : ControllerBase
    {
        private readonly ITotalAdsClub _totalAdsClub;
        private readonly IClub _club;
        private readonly IExcel _excel;

        public TotalAdsClubController(ITotalAdsClub totalAdsClub, IClub club, IExcel excel)
        {
            _totalAdsClub = totalAdsClub;
            _club = club;
            _excel = excel;
        }

        [HttpPost]
        [Route("GetPaginatedTotalAdsClubs")]
        public async Task<ActionResult> GetPaginatedTotalAdsClubs([FromBody]TotalAdsChainPaginationDto data)
        {
            var ads = await _totalAdsClub.GetPaginatedTotalAdsClubs(data);

            var dto = new
            {
                Data = ads.Item1,
                PageCount = ads.totalPages,
            };

            return Ok(dto);
        }

        [HttpPost]
        [Route("GenerateAdsClubsReportDto")]
        public async Task<ActionResult> GenerateAdsClubsReportDto(DateTime startDate, DateTime endDate, IEnumerable<int> skus)
        {
            var reportDtos = await _totalAdsClub.GenerateAdsClubsReportDto(startDate, endDate, skus);

            var report = _excel.ExportDataToExcelByDate(reportDtos);

            var file = File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AdsReport.xlsx");

            return file;
        }

        [HttpPost]
        [Route("UpdateClubTotalAverageSales")]
        public async Task<ActionResult> UpdateClubTotalAverageSales(string date)
        {
            var dateFormat = DateConvertion.ConvertStringDate(date);
            await _totalAdsClub.UpdateClubTotalAverageSales(dateFormat);

            return Ok();
        }
    }
}
