using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ads.Repository
{
    public class TagClubRepo : ITagClubs
    {
        private readonly AdsContex _adsContex;
        private readonly ILogs _logger;
        private readonly ISales _sales;
        private readonly IInventory _inventory;
        private readonly IClub _club;

        public TagClubRepo(
            AdsContex adsContex,
            ILogs logger,
            ISales sales,
            IInventory inventory,
            IClub club)
        {
            _adsContex = adsContex;
            _logger = logger;
            _sales = sales;
            _inventory = inventory;
            _club = club;
        }
        public  async Task BatchCreateTagClubsByDateAsync(DateTime date)
        {
            var log = new List<Logging>();
            var startLogs = DateTime.Now;

            try
            {
                var tags = new List<TagClub>();
                //get sales by date
                var sales = await _sales.GetSalesByDateEf(date);
                //get Inventory by date
                var inventories = await _inventory.GetEFInventoriesByDate(date);
                var inventoryDictionary = inventories.ToDictionary(x => new { x.Sku, x.Clubs }, y => y.Inventory);
                //get Clubs
                var clubs = await _club.GetClubsDictionaryByDate(date);

                //create Tags
                foreach (var sale in sales)
                {
                    var tag = new TagClub()
                    {
                        Sku = sale.Sku,
                        Club = sale.Clubs,
                        Date = date.Date,
                        Pbi = false,
                        Ads = false
                    };

                    var hasInventory = inventoryDictionary.TryGetValue(new { sale.Sku, sale.Clubs }, out var invOut);

                    if (sale.Sales > 0)
                    {
                        tag.Pbi = true;
                    }

                    if (invOut > 0)
                    {
                       tag.Ads = true;
                    }

                    if (invOut == 0 && sale.Sales > 0)
                    {
                        tag.Ads = true;
                    }

                    tags.Add(tag);
                }

                await SaveTagClubs(tags, date);
            }
            catch (Exception error)
            {
                var endLogs = DateTime.Now;
                log.Add(new Logging
                 { 
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Tags for Ads Clubs",
                    Message = $"Error: {error.Message}",
                    Record_Date = date.Date
                 });

                _logger.InsertLogs(log);

                throw;
            }
        }

        private async Task SaveTagClubs(List<TagClub> tags, DateTime date)
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
                            bulkCopy.DestinationTableName = "tbl_TagClubs";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Sku", typeof(string));
                            dataTable.Columns.Add("Club", typeof(decimal));
                            dataTable.Columns.Add("Date", typeof(DateTime));
                            dataTable.Columns.Add("Ads", typeof(bool));
                            dataTable.Columns.Add("Pbi", typeof (bool));

                            foreach (var tag in tags)
                            {
                                var row = dataTable.NewRow();
                                row["Sku"] = tag.Sku;
                                row["Club"] = tag.Club;
                                row["Date"] = tag.Date;
                                row["Ads"] = tag.Ads;
                                row["Pbi"] = tag.Ads;

                                dataTable.Rows.Add(row);
                            }

                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        transaction.Commit();
                    }
                }

                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Tags for Ads Clubs",
                    Message = $"Total Clubs Tag Inserted: {tags.Count}",
                    Record_Date = date.Date
                });

                _logger.InsertLogs(logs);
            }
            catch (Exception error)
            {
                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Tags for Ads Clubs",
                    Message = $"Error: {error.Message}",
                    Record_Date = date.Date
                });

                _logger.InsertLogs(logs);
            }
        }

        public async Task<List<TagClub>> GetTagsByDateAsync(DateTime date)
        {
            var logs = new List<Logging>();
            var startLogs = DateTime.Now;

            try
            {
                var tags = await _adsContex.TagClubs.Where(x => x.Date == date).ToListAsync();

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
