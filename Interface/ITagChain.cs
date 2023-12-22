using ads.Models.Data;

namespace ads.Interface
{
    public interface ITagChain
    {
        Task<List<TagChain>> GetTagsByDateAsync(DateTime date);
        Task BatchCreateTagChainsByDateAsync(DateTime date);
    }
}
