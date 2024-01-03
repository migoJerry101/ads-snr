using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ads.Repository
{
    public class TagChainRepo : ITagChain
    {
        private readonly AdsContex _adsContex;
        private readonly ILogs _logger;
        private readonly ISales _sales;
        private readonly IInventory _inventory;
        private readonly IClub _club;
        private readonly IItem _item;

        public TagChainRepo(
            AdsContex adsContex,
            ILogs logger,
            ISales sales,
            IInventory inventory,
            IClub club,
            IItem item)
        {
            _adsContex = adsContex;
            _logger = logger;
            _sales = sales;
            _inventory = inventory;
            _club = club;
            _item = item;
        }

        public async Task BatchCreateTagChainsByDateAsync(DateTime date)
        {
            var log = new List<Logging>();
            var startLogs = DateTime.Now;

            try
            {
                var tags = new List<TagChain>();
                //get sales by date
                var sales = await _sales.GetSalesByDateEf(date);
                //get Inventory by date
                var inventories = await _inventory.GetEFInventoriesByDate(date);

                //grou by sku
                var salesDictionary = _sales.GetDictionayOfTotalSales(sales);
                var inventoryDictionary = _inventory.GetDictionaryOfTotalInventory(inventories);
                var sku = await _item.GetAllItemSku();

                foreach (var item in sku)
                {
                    var tag = new TagChain() 
                    {
                        Sku = item,
                        Date = date,
                        IsPbiDivisor = false,
                        IsAdsDivisor = false,
                        IsOutofStocksWithOutSale = false
                    };

                    var hasSale = salesDictionary.TryGetValue(item, out var totalSales);
                    var hasInv = inventoryDictionary.TryGetValue(item, out var totalInventory);

                    if((hasSale && totalSales == 0) && (hasInv && totalInventory ==0)) tag.IsOutofStocksWithOutSale = true;

                    if (hasSale) 
                    {
                        tag.IsPbiDivisor = true;
                    }

                    if (totalInventory > 0)
                    {
                        tag.IsAdsDivisor = true;
                    }

                    if (totalInventory == 0 && totalSales > 0)
                    {
                        tag.IsAdsDivisor = true;
                    }

                    tags.Add(tag);

                    await SaveTagChains(tags, date);
                }
            }
            catch (Exception error)
            {

                var endLogs = DateTime.Now;
                log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Tags for Ads Chain",
                    Message = $"Error: {error.Message}",
                    Record_Date = date.Date
                });

                _logger.InsertLogs(log);

                throw;
            }
        }

        private async Task SaveTagChains(List<TagChain> tags, DateTime date)
        {
            var logs = new List<Logging>();
            var startLogs = DateTime.Now;

            try
            {
                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();

                    using (var transaction = db.Con.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "tbl_TagChains";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Sku", typeof(string));
                            dataTable.Columns.Add("Date", typeof(DateTime));
                            dataTable.Columns.Add("IsAdsDivisor", typeof(bool));
                            dataTable.Columns.Add("IsPbiDivisor", typeof(bool));
                            dataTable.Columns.Add("IsOutofStocksWithOutSale", typeof(bool));


                            foreach (var tag in tags)
                            {
                                var row = dataTable.NewRow();
                                row["Sku"] = tag.Sku;
                                row["Date"] = tag.Date;
                                row["Ads"] = tag.IsAdsDivisor;
                                row["Pbi"] = tag.IsPbiDivisor;
                                row["IsOutofStocksWithOutSale"] = tag.IsOutofStocksWithOutSale;

                                dataTable.Rows.Add(row);
                            }

                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        transaction.Commit();
                    }

                    DateTime endLogs = DateTime.Now;
                    logs.Add(new Logging
                    {
                        StartLog = startLogs,
                        EndLog = endLogs,
                        Action = "Tags for Ads Chain",
                        Message = $"Total Clubs Tag Inserted: {tags.Count}",
                        Record_Date = date.Date
                    });

                    _logger.InsertLogs(logs);

                }
            }
            catch (Exception error)
            {

                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Tags for Ads Chain",
                    Message = $"Error: {error.Message}",
                    Record_Date = date.Date
                });

                _logger.InsertLogs(logs);
            }
        }

        public async Task<List<TagChain>> GetTagsByDateAsync(DateTime date)
        {
            var logs = new List<Logging>();
            var startLogs = DateTime.Now;

            try
            {
                var tags = await _adsContex.TagChains.Where(x => x.Date == date).ToListAsync();

                return tags;
            }
            catch (Exception error)
            {
                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Get Tags ByDate",
                    Message = error.Message,
                    Record_Date = endLogs.Date
                });

                _logger.InsertLogs(logs);

                throw;
            }
        }
    }
}
