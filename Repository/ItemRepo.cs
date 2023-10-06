using ads.Data;
using ads.Interface;
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
    }
}
