using ads.Models.Data;

namespace ads.Interface
{
    public interface IImportInventory
    {
        Task<List<Inv>> GetInventory(string start, string end);
    }
}
