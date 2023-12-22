using ads.Models.Data;

namespace ads.Interface
{
    public interface IClub
    {
        Task<List<Club>> GetAllClubs();
        Task<Dictionary<int, DateTime>> GetClubsDictionary();
        Task<Dictionary<int, int>> GetClubsDictionaryByDate(DateTime date);
    }
}
