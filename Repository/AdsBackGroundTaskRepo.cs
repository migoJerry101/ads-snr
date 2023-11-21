using ads.Data;
using ads.Interface;
using ads.Utility;
using Quartz;

namespace ads.Repository
{
    public class AdsBackGroundTaskRepo : IAdsBackGroundTask
    {
        private readonly IInventory _inventory;
        private readonly ISales _sales;
        private readonly IAds _ads;
        private readonly IOpenQuery _openQuery;
        private readonly IItem _item;

        public AdsBackGroundTaskRepo(IInventory invetory, ISales sales, IAds ads, IOpenQuery openQuery, IItem item)
        {
            _inventory = invetory;
            _sales = sales;
            _ads = ads;
            _openQuery = openQuery;
            _item = item;
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
                    await _openQuery.ImportItems(db);
                    await _openQuery.ImportClubs(db);

                    var itemList = await _item.GetAllSkuWithDate();
                    var dateFormat = DateConvertion.ConvertStringDate(startDate);
                    var items = itemList.Where(x => x.CreatedDate <= dateFormat);

                    var inventory = await _openQuery.ListIventory(db);
                    var skus = await _openQuery.ListOfAllSKu(db);
                    var sales = await _openQuery.ListOfSales(db, startDate, endDate);

                    await _inventory.GetInventoryAsync(startDate, startDate, items, sales, inventory);
                    await _sales.GetSalesAsync(startDate, startDate, items, sales, inventory);
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
