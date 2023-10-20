using ads.Interface;
using ads.Models.Dto;
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

        public TotalAdsChainController (ITotalAdsChain totalAdsChain)
        {
            _totalAdsChain = totalAdsChain;
        }

        [HttpPost]
        [Route("GetTotalAdsChain")]
        public async Task<IActionResult> GetTotalAdsChain(TotalAdsChainPaginationDto data)
        {
            var chain = await _totalAdsChain.GetTotalAdsChain(data);

            var dto = new
            {
                Data = chain.Item1,
                PageCount = chain.totalPages,
            };

            return Ok(dto);
        }
    }
}
