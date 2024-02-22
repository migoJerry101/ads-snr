using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using ads.Utility;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using ads.Models.Dto.ItemsDto;
using System.Collections.Immutable;
using ads.Models.Dto.Sale;

namespace ads.Repository
{
    public class InventoryRepo : IInventory
    {
        private readonly IOpenQuery _openQuery;
        private readonly ILogs _logs;
        private readonly AdsContext _adsContext;
        private readonly IItem _item;
        private readonly IClub _club;

        public List<Inv> _inventoryList = new List<Inv>();

        public InventoryRepo(IOpenQuery openQuery, ILogs logs, AdsContext adsContext, IItem item, IClub club)
        {
            _logs = logs;
            _openQuery = openQuery;
            _adsContext = adsContext;
            _item = item;
            _club = club;
        }

        //Get Inventory
        public async Task<List<Inv>> GetInventoryAsync(string start, string end, IEnumerable<ItemSkuDateDto> skus, List<GeneralModel> sales, List<GeneralModel> inventory)
        {
            List<Inv> ListCsDate = new List<Inv>();

            List<Inv> ListBal = new List<Inv>();

            List<Inv> ListInventory = new List<Inv>();

            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            //skus = skus.Where(x => itemsDictionary.TryGetValue(x, out var greneralDto)).ToList();

            try
            {
                //OleDb Select Query Invetory
                using (OledbCon db = new OledbCon())
                {
                    if (db.Con.State == ConnectionState.Closed)
                    {
                        db.Con.Open();
                    }

                    var inventoryDictionary = inventory.GroupBy(y => y.INUMBR2).ToDictionary(x => x.Key, x => x.ToList());
                    var SalesDictonary = sales.ToDictionary(y => $"{y.CSSKU + y.CSSTOR}", x => x.CSDATE);


                    foreach (var item in skus)
                    {
                        if (inventoryDictionary.TryGetValue(item.Sku, out var inventoryOut))
                        {
                            foreach (var inv in inventoryOut)
                            {
                                if (SalesDictonary.TryGetValue($"{item + inv.ISTORE}", out var saleDate))
                                {
                                    var date = saleDate;

                                    ListInventory.Add(new Inv
                                    {
                                        Sku = item.Sku,
                                        Clubs = inv.ISTORE,
                                        Inventory = inv.IBHAND,
                                        Date = DateConvertion.ConvertStringDate(date),
                                    });
                                }
                                else
                                {
                                    ListInventory.Add(new Inv
                                    {
                                        Sku = item.Sku,
                                        Clubs = inv.ISTORE,
                                        Inventory = inv.IBHAND,
                                        Date = DateConvertion.ConvertStringDate(start),
                                    });
                                }
                            }
                        }
                        else
                        {

                            ListInventory.Add(new Inv
                            {
                                Sku = item.Sku,
                                Clubs = string.Empty,
                                Inventory = 0,
                                Date = DateConvertion.ConvertStringDate(start),
                            });
                        }
                    }

                    Console.WriteLine(ListInventory);

                    //Bluk insert in tbl_Data table
                    using (var transaction = db.Con.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "tbl_inv";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Date", typeof(DateTime));
                            dataTable.Columns.Add("Sku", typeof(string));
                            dataTable.Columns.Add("Inventory", typeof(decimal));
                            dataTable.Columns.Add("Clubs", typeof(string));

                            foreach (var rawData in ListInventory)
                            {
                                var row = dataTable.NewRow();
                                row["Date"] = rawData.Date;
                                row["Sku"] = rawData.Sku;
                                row["Inventory"] = rawData.Inventory;
                                row["Clubs"] = rawData.Clubs;
                                dataTable.Rows.Add(row);

                            }
                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        transaction.Commit();
                    }

                }

                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Inventory",
                    Message = "Total Rows Inserted : " + ListInventory.Count + "",
                    Record_Date = DateConvertion.ConvertStringDate(start)
                });

                _logs.InsertLogs(Log);

                return ListCsDate;

            }
            catch (Exception e)
            {
                // string TotalRows = TotalInventory(start, end);

                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "GetInventoryAsync : " + e.Message + " ",
                    Record_Date = DateConvertion.ConvertStringDate(start)
                });

                _logs.InsertLogs(Log);

                return ListCsDate;
            }
        }

        public async Task GetInventories(string dateListString, int pageSize, int offset, OledbCon db)
        {
            DateTime startLogs = DateTime.Now;
            List<Logging> Log = new List<Logging>();
            try
            {
                await Task.Run(() =>
                {

                    string strConn = "data source='199.84.0.201';Initial Catalog=ADS.UAT;User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
                    var con = new SqlConnection(strConn);

                    using (var command = new SqlCommand("_sp_GetTblDataSample1", con))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Offset", offset);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@dateListString", dateListString);
                        command.CommandTimeout = 18000;
                        var inventories = new List<Inv>();
                        con.Open();

                        // Open the connection and execute the command
                        SqlDataReader reader = command.ExecuteReader();

                        // Process the result set
                        while (reader.Read())
                        {

                            var date = reader.GetDateTime("Date");

                            Inv Olde = new Inv
                            {
                                Clubs = reader["Clubs"].ToString(),
                                Sku = reader["Sku"].ToString(),
                                Inventory = Convert.ToDecimal(reader["Inventory"].ToString()),
                                Date = date,
                            };

                            inventories.Add(Olde);
                        }

                        _inventoryList.AddRange(inventories);
                        // Close the reader and connection
                        reader.Close();
                        con.Close();
                    }

                    //string query = "select * from tbl_inv where Date in (" + dateListString + ") " +
                    //        "ORDER BY Date OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY ";

                    //using (SqlCommand cmd = new SqlCommand(query, db.Con))
                    //{
                    //    cmd.Parameters.AddWithValue("@Offset", offset);
                    //    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    //    cmd.CommandTimeout = 18000;

                    //    using (var reader = cmd.ExecuteReader())
                    //    {
                    //        while ( reader.Read())
                    //        {
                    //            var date = reader["Date"].ToString();

                    //            Inventory Olde = new Inventory
                    //            {
                    //                Clubs = reader["Clubs"].ToString(),
                    //                Sku = reader["Sku"].ToString(),
                    //                Inv = Convert.ToDecimal(reader["Inventory"].ToString()),
                    //                Date = dateConvertion.ConvertStringDate(date),
                    //            };

                    //            _inventoryList.Add(Olde);
                    //        }
                    //    }
                    //}
                });
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);

                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "GetInventories : " + e.Message + " ",
                    Record_Date = DateConvertion.ConvertStringDate(dateListString)
                });

                _logs.InsertLogs(Log);
            }

        }

        //list Inventory 
        public async Task<List<Inv>> ListInv(string dateListString, OledbCon db)
        {
            _inventoryList = new List<Inv>();
            var tasks = new List<Task>();

            //var pageSize = 400000;

            // Get the total count of rows for your date filter
            var rowCount = await CountInventory(dateListString, db);
            var pageSize = (int)Math.Ceiling((double)rowCount / 5);

            // Calculate the total number of pages
            var totalPages = (int)Math.Ceiling((double)rowCount / pageSize);
            // Calculate the total number of pages
            //50
            for (int pageNumber = 0; pageNumber < totalPages; pageNumber++)
            {
                var offset = pageSize * pageNumber;

                tasks.Add(GetInventories(dateListString.Replace("'", ""), pageSize, offset, db));
            }

            await Task.WhenAll(tasks);

            return _inventoryList;
        }

        //Total Inventory
        public string TotalInventory(string startDate, string endDate)
        {
            string totalsales = "";
            using (MsSqlCon db = new MsSqlCon())
            {
                string oledb = "Select Count(*) Total from tbl_inv where Date between @StartDate and @EndDate";

                using (SqlCommand cmd = new SqlCommand(oledb, db.Con))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalsales += reader["Total"].ToString();
                        }
                    }
                }
            }

            return totalsales;
        }

        //TotalCount of Sales
        public async Task<int> CountInventory(string dateListString, OledbCon db)
        {
            int totalCount = 0;

            string query = "select COUNT(Id) as Count from tbl_inv where Date in (" + dateListString + ") ";

            using (SqlCommand cmd = new SqlCommand(query, db.Con))
            {

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        totalCount = (int)reader["Count"];
                    }
                }
            }

            return totalCount;
        }

        public async Task<List<Inv>> GetInventoriesByDateEf(DateTime date)
        {
            var inventories = await _adsContext.Inventories.Where(x => x.Date == date).ToListAsync();

            return inventories;
        }

        public async Task<List<Inv>> GetInventoriesByDateAndClubs(DateTime date)
        {
            var clubs = await _club.GetAllClubs();
            var clubCode = clubs.Select(x => x.Number.ToString()).ToList();
            var inventories = await _adsContext.Inventories.Where(x => x.Date == date).ToListAsync();
            var filterdInventories = inventories.Where(x => x.Clubs.IsNullOrEmpty() || clubCode.Contains(x.Clubs)).ToList();

            return filterdInventories;
        }

        public async Task<List<Inv>> GetInventoriesByDate(DateTime date)
        {
            _inventoryList = new List<Inv>();
            var tasks = new List<Task>();
            var dateWithZeroTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0);
            var dateInString = $"'{dateWithZeroTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";

            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();
                var rowCount = await CountInventory(dateInString, db);

                if (rowCount == 0) return _inventoryList;

                var pageSize = (int)Math.Ceiling((double)rowCount / 5);
                var totalPages = (int)Math.Ceiling((double)rowCount / pageSize);

                for (int pageNumber = 0; pageNumber < totalPages; pageNumber++)
                {
                    int offset = pageSize * pageNumber;

                    tasks.Add(GetInventories(dateInString.Replace("'", ""), pageSize, offset, db));
                }

                await Task.WhenAll(tasks);
            }

            return _inventoryList;
        }

        public Dictionary<SalesKey, decimal> GetDictionayOfTotalInventory(List<Inv> inventories)
        {
            var inventoryDictionary = inventories
                .GroupBy(x => 
                    new SalesKey() 
                    { 
                        Sku = x.Sku,
                        Date = x.Date 
                    })
                .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Inventory)
            );

            return inventoryDictionary;
        }

        public Dictionary<string, decimal> GetDictionayOfPerClubhlInventory(List<Inv> inventories)
        {
            var inventoryDictionary = inventories.GroupBy(x => $"{x.Sku}{x.Clubs}").ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Inventory)
            );

            return inventoryDictionary;
        }

        public async Task<List<Inv>> GetEFInventoriesByDate(DateTime date)
        {
            var inventories = await _adsContext.Inventories.Where(x => x.Date == date).ToListAsync();

            return inventories;
        }

        public async Task BatchUpdateInventoryBysales(List<Sale> updatedSales)
        {
            var startLog = DateTime.Now;

            try
            {
                var date = updatedSales.First().Date;
                var invToUpdateDictionary = await _adsContext.Inventories
                    .Where(x => x.Date == date)
                    .ToDictionaryAsync(x => new { x.Sku, x.Clubs, x.Date }, y => y);

                foreach (var sales in updatedSales)
                {
                    var key = new { sales.Sku, sales.Clubs, sales.Date };
                    var hasEntry = invToUpdateDictionary.TryGetValue(key, out var invOut);

                    if (invOut != null)
                    {
                        invOut.Inventory += sales.Sales;

                        if (invOut.Inventory < 0) invOut.Inventory = 0;
                    }
                }

                await _adsContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var logs = new List<Logging>();
                Console.WriteLine("Error: " + e.Message);

                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = startLog,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "BatchUpdateInventoryBysales : " + e.Message + " ",
                    Record_Date = endLogs
                });

                _logs.InsertLogs(logs);
            }

        }

        public async Task<List<Inv>> GetInventoriesByDates(List<DateTime> dates)
        {
            var startLog = DateTime.Now;

            try
            {
                var inventories = new List<Inv>();

                foreach (var date in dates)
                {
                    var inventoriesDayZero = await _adsContext.Inventories.Where(x => x.Date == date).ToListAsync();

                    inventories.AddRange(inventoriesDayZero);
                }

                return inventories;
            }
            catch (Exception error)
            {
                var logs = new List<Logging>();
                var endLogs = DateTime.Now;

                logs.Add(new Logging
                {
                    StartLog = startLog,
                    EndLog = endLogs,
                    Action = "GetInventoriesByDates",
                    Message = error.Message,
                    Record_Date = endLogs
                });

                _logs.InsertLogs(logs);

                throw;
            }
        }

        public async Task<List<Inv>> GetInventoriesWithFilteredSku(Dictionary<string, List<string>> sku, List<DateTime> days)
        {
            var startLog = DateTime.Now;

            try
            {
                var inventories = new List<Inv>();

                foreach (var day in days)
                {
                    var hasSku = sku.TryGetValue($"{day:yyyy-MM-dd HH:mm:ss.fff}", out var skuOut);

                    if (hasSku)
                    {
                        var distinct = skuOut.Distinct();

                        var inventoriesToday = await _adsContext.Inventories
                            .Where(x => x.Date == day && distinct.Contains(x.Sku))
                            .ToListAsync();

                        inventories.AddRange(inventoriesToday);
                    }

                }

                return inventories;
            }
            catch (Exception error)
            {
                var logs = new List<Logging>();
                var endLogs = DateTime.Now;

                logs.Add(new Logging
                {
                    StartLog = startLog,
                    EndLog = endLogs,
                    Action = "GetInventoriesWithFilter",
                    Message = error.Message,
                    Record_Date = endLogs
                });

                _logs.InsertLogs(logs);

                throw;
            }
        }
    }
}
