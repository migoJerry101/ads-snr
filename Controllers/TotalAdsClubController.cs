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
        private readonly IClub _club;

        public TotalAdsClubController(ITotalAdsClub totalAdsClub, IClub club)
        {
            _totalAdsClub = totalAdsClub;
            _club = club;
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
        [Route("GetClubs")]
        public async Task<ActionResult> GetClubs()
        {
            var ads = await _club.GetAllClubs();

            return Ok(ads);
        }
    }
}
