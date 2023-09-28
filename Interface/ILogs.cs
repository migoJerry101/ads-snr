using ads.Models.Data;

namespace ads.Interface
{
    public interface ILogs
    {
        void InsertLogs(List<Logging> logging);
        List<Logging> SelectLastLogs();
    }
}
