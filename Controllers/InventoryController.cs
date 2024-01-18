using ads.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class InventoryController : Controller
    {
        private readonly IInventory _inventory;

        public InventoryController(IInventory inventory)
        {
            _inventory = inventory;
        }

        [HttpPost]
        [Route("GetInventoriesByDateAndClubs")]
        public async Task<IActionResult> GetInventoriesByDateAndClubs(DateTime date)
        {
            var inventories = await _inventory.GetInventoriesByDateAndClubs(date);
            var test = inventories.OrderByDescending(x => x.Clubs).ToList();
            return Ok();
        }
    }
}
