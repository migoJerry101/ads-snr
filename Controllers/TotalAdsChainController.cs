using ads.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    public class TotalAdsChainController : ControllerBase
    {
        private readonly ITotalAdsChain _totalAdsChain;

        public TotalAdsChainController (ITotalAdsChain totalAdsChain)
        {
            _totalAdsChain = totalAdsChain;
        }

        [HttpGet]
        [Route("GetTotalAdsChain")]
        public IActionResult GetTotalAdsChain()
        {
            var chain = _totalAdsChain.GetTotalAdsChain();

            return Ok(chain);
        }
    }
}
