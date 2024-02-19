using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto.ItemsDto;
using ads.Models.Dto.Sale;
using ads.Utility;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ads.Repository
{
    public class SalesRepo : ISales
    {
        private readonly IOpenQuery _openQuery;
        private readonly ILogs _logs;
        private readonly AdsContext _adsContext;
        private readonly IItem _item;
        private readonly IConfiguration _configuration;
        private readonly IClub _club;

        //private readonly DateConvertion dateConvertion = new DateConvertion();
        private List<Sale> _saleList = new List<Sale>();

        public SalesRepo(IOpenQuery openQuery, ILogs logs, AdsContext adsContext, IItem item, IConfiguration configuration, IClub club)
        {
            _openQuery = openQuery;
            _logs = logs;
            _adsContext = adsContext;
            _item = item;
            _configuration = configuration;
            _club = club;
        }

        private List<Sale> GenerateListOfDataRows(List<GeneralModel> datas, string sku, bool hasSales, bool useStartDate, string? date)
        {
            List<Sale> listOfOledb = new List<Sale>();

            foreach (var data in datas)
            {
                var Olde = new Sale
                {
                    Sku = sku,
                    Clubs = data.CSSTOR ?? data.ISTORE,
                    Sales = hasSales ? data.CSQTY : 0,
                    Date = DateConvertion.ConvertStringDate(!useStartDate ? data.CSDATE : date),
                };

                listOfOledb.Add(Olde);
            }

            return listOfOledb;
        }

        //Get Sales
        public async Task<List<Sale>> GetSalesAsync(string start, string end, IEnumerable<ItemSkuDateDto> skus, List<GeneralModel> listOfSales, List<GeneralModel> inventories)
        {
            List<Sale> listOfOledb = new List<Sale>();
            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            try
            {
                //OleDb Select Query
                using (OledbCon db = new OledbCon())
                {
                    if (db.Con.State == ConnectionState.Closed)
                    {
                        db.Con.Open();
                    }

                    var inventoryLookup = inventories.GroupBy(x => x.INUMBR2).ToDictionary(group => group.Key, group => group.ToList());
                    var salesLookup = listOfSales.GroupBy(x => x.CSSKU).ToDictionary(group => group.Key, group => group.ToList());

                    foreach (var item in skus)
                    {
                        var hasSales = salesLookup.TryGetValue(item.Sku, out var salesOut);

                        if (hasSales && (salesOut is not null))
                        {
                            foreach (var data in salesOut)
                            {
                                var Olde = new Sale
                                {
                                    Sku = item.Sku,
                                    Clubs = data.CSSTOR,
                                    Sales = data.CSQTY,
                                    Date = DateConvertion.ConvertStringDate(data.CSDATE),
                                };

                                listOfOledb.Add(Olde);
                            }
                        }

                        var hasInv = inventoryLookup.TryGetValue(item.Sku, out var inventoryOut);

                        if (hasInv && (inventoryOut is not null))
                        {
                            //remove inventory entry in sales
                            var salesList = salesOut != null ? salesOut?.Select(x => x.CSSTOR).AsEnumerable() : new List<string>();
                            var sanitizedInv = inventoryOut.Where(x => !salesList.Contains(x.ISTORE));

                            foreach (var data in sanitizedInv)
                            {
                                var Olde = new Sale
                                {
                                    Sku = item.Sku,
                                    Clubs = data.ISTORE,
                                    Sales = 0,
                                    Date = DateConvertion.ConvertStringDate(start),
                                };

                                listOfOledb.Add(Olde);
                            }
                        }

                        if (!hasInv & !hasSales)
                        {
                            // this entry is in the master list of sku, but not yet part of sales and enventory table
                            // this is used when computing chain/all store ADS
                            // this is filtered out when computing ADS of per store per sku
                            var Olde = new Sale
                            {
                                Sku = item.Sku,
                                Clubs = string.Empty,
                                Sales = 0,
                                Date = DateConvertion.ConvertStringDate(start),
                            };

                            listOfOledb.Add(Olde);
                        }
                    }

                    //Bluk insert in tbl_Data table
                    using (var transaction = db.Con.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "tbl_sales_data";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Clubs", typeof(string));
                            dataTable.Columns.Add("Sku", typeof(string));
                            dataTable.Columns.Add("Sales", typeof(decimal));
                            dataTable.Columns.Add("Date", typeof(DateTime));

                            foreach (var rowData in listOfOledb)
                            {
                                var row = dataTable.NewRow();
                                row["Clubs"] = rowData.Clubs;
                                row["Sku"] = rowData.Sku;
                                row["Sales"] = rowData.Sales;
                                row["Date"] = rowData.Date;
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
                    Action = "Sales",
                    Message = "Total Rows Inserted : " + listOfOledb.Count + "",
                    Record_Date = DateConvertion.ConvertStringDate(start)
                });

                _logs.InsertLogs(Log);

                return listOfOledb;
            }
            catch (Exception e)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "GetSalesAsync : " + e.Message + " ",
                    Record_Date = DateConvertion.ConvertStringDate(start)
                });

                _logs.InsertLogs(Log);

                return listOfOledb;
            }


        }

        public void DeleteSalesByDate(DateTime date)
        {
            DateTime startLogs = DateTime.Now;
            List<Logging> Log = new List<Logging>();

            try
            {
                var strConn = _configuration["ConnectionStrings:DatabaseConnection"];
                //string strConn = "data source='199.84.0.201';Initial Catalog=ADS.UAT;User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
                var con = new SqlConnection(strConn);

                using (var command = new SqlCommand("_sp_DeleteSalesByDate", con))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@date", date);
                    command.CommandTimeout = 18000;
                    con.Open();

                    // Open the connection and execute the command
                    SqlDataReader reader = command.ExecuteReader();

                    reader.Close();
                    con.Close();
                }
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
                    Message = "GetAllSales : " + e.Message + " ",
                    Record_Date = date
                });

                _logs.InsertLogs(Log);
            }
        }

        public async Task GetAllSales(string dateListString, int pageSize, int offset, OledbCon db)
        {
            DateTime startLogs = DateTime.Now;

            List<Logging> Log = new List<Logging>();

            try
            {
                await Task.Run(() =>
                {
                    /*                    string query = "select * from tbl_data where Date in (" + dateListString + ") " +
                                            "ORDER BY Date OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY ";*/

                    string strConn = "data source='199.84.0.201';Initial Catalog=ADS.UAT;User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
                    var con = new SqlConnection(strConn);

                    using (var command = new SqlCommand("_sp_GetTblDataSample", con))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Offset", offset);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@dateListString", dateListString);
                        command.CommandTimeout = 18000;
                        con.Open();
                        var sales = new List<Sale>();

                        // Open the connection and execute the command
                        SqlDataReader reader = command.ExecuteReader();

                        // Process the result set
                        while (reader.Read())
                        {

                            Sale Olde = new Sale
                            {
                                Clubs = reader["Clubs"].ToString(),
                                Sku = reader["Sku"].ToString(),
                                Sales = Convert.ToDecimal(reader["Sales"].ToString()),
                                Date = reader.GetDateTime("Date"),
                            };

                            sales.Add(Olde);
                        }

                        _saleList.AddRange(sales);
                        // Close the reader and connection
                        reader.Close();
                        con.Close();
                    }
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
                    Message = "GetAllSales : " + e.Message + " ",
                    Record_Date = DateConvertion.ConvertStringDate(dateListString)
                });

                _logs.InsertLogs(Log);
            }
        }

        //list Data
        public async Task<List<Sale>> ListSales(string dateListString, OledbCon db)
        {
            List<Sale> list = new List<Sale>();
            var tasks = new List<Task>();
            _saleList = new List<Sale>();

            // Get the total count of rows for your date filter
            var rowCount = await CountSales(dateListString, db);
            var pageSize = (int)Math.Ceiling((double)rowCount / 5);

            // Calculate the total number of pages
            var totalPages = (int)Math.Ceiling((double)rowCount / pageSize);

            for (int pageNumber = 0; pageNumber < totalPages; pageNumber++)
            {
                int offset = pageSize * pageNumber;

                tasks.Add(GetAllSales(dateListString.Replace("'", ""), pageSize, offset, db));
            }

            await Task.WhenAll(tasks);

            return _saleList;
        }

        //Total Sales
        public string TotalSales(string startDate, string endDate)
        {
            var totalsales = "";
            using (MsSqlCon db = new MsSqlCon())
            {
                string oledb = "Select Count(*) Total from TBL_DATA where Date between @StartDate and @EndDate";

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

            return totalsales.ToString();
        }

        //TotalCount of Sales
        public async Task<int> CountSales(string dateListString, OledbCon db)
        {
            int totalCount = 0;

            string query = "select COUNT(*) as Count from tbl_data where Date in (" + dateListString + ") ";

            using (SqlCommand cmd = new SqlCommand(query, db.Con))
            {
                cmd.CommandTimeout = 18000;

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

        public async Task<List<Sale>> GetSalesByDateEf(DateTime date)
        {
            var sales = await _adsContext.Sales.Where(x => x.Date == date).ToListAsync();

            return sales;
        }

        public async Task<List<Sale>> GetSalesByDateAndClub(DateTime date)
        {
            var clubs = await _club.GetAllClubs();
            var clubCode = clubs.Select(x => x.Number.ToString()).ToList();
            var sales = await _adsContext.Sales.Where(x => x.Date == date).ToListAsync();
            var filteredSales = sales.Where(x => x.Clubs.IsNullOrEmpty() || clubCode.Contains(x.Clubs)).ToList();

            return filteredSales;
        }
        
        public async Task<List<Sale>> GetSalesByDate(DateTime date)
        {
            List<Sale> list = new List<Sale>();
            var tasks = new List<Task>();
            _saleList = new List<Sale>();
            var dateWithZeroTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0);
            var dateInString = $"'{dateWithZeroTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";

            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();
                var rowCount = await CountSales(dateInString, db);

                if (rowCount == 0) return _saleList;

                var pageSize = (int)Math.Ceiling((double)rowCount / 5);
                var totalPages = (int)Math.Ceiling((double)rowCount / pageSize);

                for (int pageNumber = 0; pageNumber < totalPages; pageNumber++)
                {
                    int offset = pageSize * pageNumber;

                    tasks.Add(GetAllSales(dateInString.Replace("'", ""), pageSize, offset, db));
                }

                await Task.WhenAll(tasks);

            }

            return _saleList;
        }

        public Dictionary<string, decimal> GetDictionayOfTotalSales(List<Sale> sales)
        {
            var salesDictionary = sales.GroupBy(x => x.Sku).ToDictionary(
                 group => group.Key,
                 group =>
                 {
                     var count = group.Count();

                     if (count > 1)
                     {
                         return group.Where(x => x.Sales >= 0).Sum(item => item.Sales);
                     }

                     return group.Sum(item => item.Sales);
                 }
             );

            return salesDictionary;
        }

        public async Task DeleteSalesByDateAsync(DateTime date)
        {
            var startLog = DateTime.Now;
            var logs = new List<Logging>();

            try
            {
                var sales = _adsContext.Sales.Where(c => c.Date == date);
                _adsContext.Sales.RemoveRange(sales);

                await _adsContext.SaveChangesAsync();

                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = startLog,
                    EndLog = endLogs,
                    Action = "DeleteSalesByDateAsync",
                    Message = "Total Sales Deleted : " + sales.Count() + "",
                    Record_Date = DateTime.Now
                });

                _logs.InsertLogs(logs);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);

                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = startLog,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "DeleteSalesByDateAsync : " + e.Message + " ",
                    Record_Date = endLogs
                });

                _logs.InsertLogs(logs);
            }
        }

        public List<Sale> GetAdjustedSalesValue(List<Sale> sales, IEnumerable<Sale> reImportedSales)
        {
            var salesWithDiff = new List<Sale>();
            var currentSalesDitionary = sales.ToDictionary(x => new { x.Sku, x.Clubs, x.Date });

            foreach (var sale in reImportedSales)
            {
                var hasSales = currentSalesDitionary.TryGetValue(new { sale.Sku, sale.Clubs, sale.Date }, out var saleOut);

                //redeploy after reimport remove rounding off
                if (hasSales && (sale.Sales != saleOut.Sales))
                {
                    //sales - reimported data
                    //salesOut - current data
                    if (sale.Sales < 0)
                    {
                        saleOut.Sales = Math.Abs(sale.Sales);
                    }
                    else
                    {
                        var diffrenceInSales = saleOut.Sales - sale.Sales;
                        saleOut.Sales = diffrenceInSales;
                    }

                    salesWithDiff.Add(saleOut);
                }
            }

            return salesWithDiff;
        }

        public async Task<List<Sale>> GetSalesByDates(List<DateTime> dates)
        {
            var startLog = DateTime.Now;
            var logs = new List<Logging>();

            try
            {
                var salesOfDayZeroes = new List<Sale>();

                foreach (var date in dates)
                {
                    var sales = await _adsContext.Sales.Where(c => c.Date == date).ToListAsync();
                    salesOfDayZeroes.AddRange(sales);
                }

                return salesOfDayZeroes;
            }
            catch (Exception error)
            {
                DateTime endLogs = DateTime.Now;
                logs.Add(new Logging
                {
                    StartLog = startLog,
                    EndLog = endLogs,
                    Action = "GetSalesByDates",
                    Message = error.Message,
                    Record_Date = endLogs
                });

                _logs.InsertLogs(logs);

                throw;
            }
        }

        public Dictionary<SalesKey, decimal> GetDictionayOfTotalSalesWithSalesKey(List<Sale> sales)
        {
            var startLog = DateTime.Now;

            try
            {
                var salesDictionary = sales
                    .GroupBy(x => new SalesKey() { Date = x.Date, Sku = x.Sku})
                    .ToDictionary(
                     group => group.Key,
                     group =>
                     {
                         var count = group.Count();

                         if (count > 1) return group.Where(x => x.Sales >= 0).Sum(item => item.Sales);
                         
                         return group.Sum(item => item.Sales);
                     });

                return salesDictionary;
            }
            catch (Exception error)
            {
                var logs = new List<Logging>();
                var endLogs = DateTime.Now;

                logs.Add(new Logging
                {
                    StartLog = startLog,
                    EndLog = endLogs,
                    Action = "GetDictionayOfTotalSalesWithSalesKey",
                    Message = error.Message,
                    Record_Date = endLogs
                });

                _logs.InsertLogs(logs);

                throw;
            }
        }

        public async Task<List<Sale>> GetSalesWithFilteredSku(Dictionary<string, List<string>> sku, List<DateTime> days)
        {
            var startLog = DateTime.Now;

            try
            {
                var sales = new List<Sale>();

                foreach (var day in days)
                {
                    var hasSku = sku.TryGetValue($"{day:yyyy-MM-dd HH:mm:ss.fff}", out var skuOut);

                    if (hasSku)
                    {
                        var distinctSku = skuOut.Distinct().ToList();

                        var salesToday = await _adsContex.Sales
                            .Where(x => x.Date == day && distinctSku.Contains(x.Sku))
                            .ToListAsync();

                        sales.AddRange(salesToday);
                    }
                }

                return sales;
            }
            catch (Exception error)
            {
                var logs = new List<Logging>();
                var endLogs = DateTime.Now;

                logs.Add(new Logging
                {
                    StartLog = startLog,
                    EndLog = endLogs,
                    Action = "GetSalesWithFilteredSku",
                    Message = error.Message,
                    Record_Date = endLogs
                });

                _logs.InsertLogs(logs);

                throw;
            }
        }
    }
}
