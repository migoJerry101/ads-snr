using ads.Models.Data;

namespace ads.Interface
{
    public interface IImportInventory
    {
        Task<List<Inventory>> GetInventory(string start, string end);
    }
}
