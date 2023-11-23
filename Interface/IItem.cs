using ads.Models.Dto.ItemsDto;

namespace ads.Interface
{
    public interface IItem
    {
        Task<List<string>> GetAllItemSku();
        Task<List<ItemSkuDateDto>> GetAllSkuWithDate();
    }
}
