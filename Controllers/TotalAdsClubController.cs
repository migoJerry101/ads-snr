using ads.Interface;
using ads.Models.Dto;
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

        public TotalAdsClubController(ITotalAdsClub totalAdsClub)
        {
            _totalAdsClub = totalAdsClub;
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
    }
}
