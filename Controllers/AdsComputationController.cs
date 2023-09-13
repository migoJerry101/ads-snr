using ads.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace ads.Controllers
{
    public class AdsComputationController : ControllerBase
    {
        private readonly IAdsComputation _ads;
        public AdsComputationController(IAdsComputation ads)
        {
            _ads = ads;
        }

        [HttpGet]
        [Route("GetCompute")]
        public async Task<IActionResult> GetComputeAsync()
        {
            var transformedData = await _ads.GetTotalApdAsync();
            return Ok(transformedData);
        }
    }
}
