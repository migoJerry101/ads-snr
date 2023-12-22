using ads.Models.Data;

namespace ads.Interface
{
    public interface ITagClubs
    {
        Task<List<TagClub>> GetTagsByDateAsync(DateTime date);
        Task BatchCreateTagClubsByDateAsync(DateTime date);
    }
}
