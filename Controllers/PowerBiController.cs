using ads.Interface;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    public class PowerBiController : ControllerBase
    {
        private readonly IPowerBiAds _powerBiAds;

        public PowerBiController(IPowerBiAds powerBiAds)
        {
            _powerBiAds = powerBiAds;
        }

        public async Task<IActionResult> ComputePowerBiAdsAsync(DateTime date)
        {
            await _powerBiAds.ComputePowerBiAdsAsync(date);

            return Ok();
        }
    }
}
