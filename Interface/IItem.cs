namespace ads.Interface
{
    public interface IItem
    {
        Task<List<string>> GetAllItemSku();
    }
}
