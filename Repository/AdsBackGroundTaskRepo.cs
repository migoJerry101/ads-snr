using ads.Data;
using ads.Interface;
using Quartz;

namespace ads.Repository
{
    public class AdsBackGroundTaskRepo : IAdsBackGroundTask
    {
        private readonly IInvetory _inventory;
        private readonly ISales _sales;
        private readonly IAds _ads;
        private readonly IOpenQuery _openQuery;

        public AdsBackGroundTaskRepo(IInvetory invetory, ISales sales, IAds ads, IOpenQuery openQuery)
        {
            _inventory = invetory;
            _sales = sales;
            _ads = ads;
            _openQuery = openQuery;
        }

        public async Task<string> ExecuteTask()
        {
            //string id = Guid.NewGuid().ToString();
            //string message = "This job will be executed again at: " +
            //context.NextFireTimeUtc.ToString();

            try
            {
                // Get the current date
                DateTime currentDate = DateTime.Now;

                // Subtract one day
                DateTime previousDate = currentDate.AddDays(-1);

                ////////Actual Record or Final Setup
                //string startDate = previousDate.ToString("yyMMdd");
                //string endDate = previousDate.ToString("yyMMdd");
                
                string startDate = "";
                string endDate = "";

                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();

                    var inventory = await _openQuery.ListIventory(db);
                    var skus = await _openQuery.ListOfAllSKu(db);
                    var sales = await _openQuery.ListOfSales(db, startDate, endDate);

                    await _inventory.GetInventoryAsync(startDate, startDate, skus, sales, inventory);
                    await _sales.GetSalesAsync(startDate, startDate, skus, sales, inventory);
                }

                //await _ads.GetComputation();

                return "Success";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

                return "Error : " + e.Message + " ";
            }

            //return await Task.CompletedTask;
        }
    }
}
