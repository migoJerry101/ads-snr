using ads.Data;
using ads.Interface;
using ads.Models.Dto.ItemsDto;
using Microsoft.EntityFrameworkCore;

namespace ads.Repository
{
    public class ItemRepo : IItem
    {
        private readonly AdsContex _adsContex;

        public ItemRepo(AdsContex adsContex)
        {
            _adsContex = adsContex;
        }
        public async Task<List<string>> GetAllItemSku()
        {
            var skus = await _adsContex.Items.Select(x => x.Sku.ToString()).ToListAsync();

            return skus;
        }

        public async Task<List<ItemSkuDateDto>> GetAllSkuWithDate()
        {
            var items = await _adsContex.Items
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
