using ads.Data;
using ads.Interface;
using ads.Models.Dto.ItemsDto;
using Microsoft.EntityFrameworkCore;

namespace ads.Repository
{
    public class ItemRepo : IItem
    {
        private readonly AdsContext _adsContext;

        public ItemRepo(AdsContext adsContext)
        {
            _adsContext = adsContext;
        }
        public async Task<List<string>> GetAllItemSku()
        {
            var skus = await _adsContext.Items.Select(x => x.Sku.ToString()).ToListAsync();

            return skus;
        }

        public async Task<List<ItemSkuDateDto>> GetAllSkuWithDate()
        {
            var items = await _adsContext.Items
                .AsNoTracking()
                .Select(x =>
                    new ItemSkuDateDto()
                    { 
                        Sku = x.Sku,
                        CreatedDate = x.CreatedDate
                    })
                .ToListAsync();

            return items;
        }
    }
}
