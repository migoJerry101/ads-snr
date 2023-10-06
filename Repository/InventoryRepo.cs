using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using ads.Utility;
using Microsoft.IdentityModel.Tokens;

namespace ads.Repository
{
    public class InventoryRepo : IInvetory
    {
        private readonly IOpenQuery _openQuery;
        private readonly ILogs _logs ;

        private readonly DateConvertion dateConvertion = new DateConvertion();
        public List<Inventory> _inventoryList = new List<Inventory>();

        public InventoryRepo(IOpenQuery openQuery, ILogs logs)
        { 
            _logs = logs;
            _openQuery = openQuery;
        }

        //Get Inventory
        public async Task<List<Inventory>> GetInventoryAsync(string start, string end, List<GeneralModel> skus, List<GeneralModel> sales, List<GeneralModel> inventory)
        {
            List<Inventory> ListCsDate = new List<Inventory>();

            List<Inventory> ListBal = new List<Inventory>();

            List<Inventory> ListInventory = new List<Inventory>();

            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            try
            {
                //OleDb Select Query Invetory
                using (OledbCon db = new OledbCon())
                {
                    if (db.Con.State == ConnectionState.Closed)
                    {
                        db.Con.Open();
                    }

                    var skuDictionary = skus.ToDictionary(x => x.INUMBR);
                    
                    var inventoryDictionary = inventory.GroupBy(y=> y.INUMBR2).ToDictionary(x => x.Key, x=> x.ToList());
                    var SalesDictonary = sales.ToDictionary(y => $"{y.CSSKU + y.CSSTOR}", x => x.CSDATE);


                    foreach (var sku in skus)
                    {
                        if (inventoryDictionary.TryGetValue(sku.INUMBR, out var inventoryOut))
                        {
                            foreach (var inv in inventoryOut)
                            {
                                if (SalesDictonary.TryGetValue($"{sku.INUMBR + inv.ISTORE}", out var saleDate))
                                {
                                    var date = saleDate;

                                    ListInventory.Add(new Inventory
                                    {
                                        Sku = sku.INUMBR,
                                        Clubs = inv.ISTORE,
                                        Inv = inv.IBHAND,
                                        Date = dateConvertion.ConvertStringDate(date),
                                    });
                                }
                                else
                                {
                                    ListInventory.Add(new Inventory
                                    {
                                        Sku = sku.INUMBR,
                                        Clubs = inv.ISTORE,
                                        Inv = inv.IBHAND,
                                        Date = dateConvertion.ConvertStringDate(start),
                                    });
                                }
                            }
                        }
                        else
                        {

                            ListInventory.Add(new Inventory
                            {
                                Sku = sku.INUMBR,
                                Clubs = string.Empty,
                                Inv = 0,
                                Date = dateConvertion.ConvertStringDate(start),
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
                                row["Inventory"] = rawData.Inv;
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
                    Record_Date = dateConvertion.ConvertStringDate(start)
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
                    Record_Date = dateConvertion.ConvertStringDate(start)
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
                        var inventories = new List<Inventory>();
                        con.Open();

                        // Open the connection and execute the command
                        SqlDataReader reader = command.ExecuteReader();

                        // Process the result set
                        while (reader.Read())
                        {

                            var date = reader.GetDateTime("Date");

                            Inventory Olde = new Inventory
                            {
                                Clubs = reader["Clubs"].ToString(),
                                Sku = reader["Sku"].ToString(),
                                Inv = Convert.ToDecimal(reader["Inventory"].ToString()),
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
                    Record_Date = dateConvertion.ConvertStringDate(dateListString)  
                });

                _logs.InsertLogs(Log);
            }

        }

        //list Inventory 
        public async Task<List<Inventory>> ListInv(string dateListString, OledbCon db)
        {
            _inventoryList = new List<Inventory>();
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

            string query = "select COUNT(Id) as Count from tbl_inv where Date in (" + dateListString +") ";

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

        public async Task<List<Inventory>> GetInventoriesByDate(DateTime date)
        {
            _inventoryList = new List<Inventory>();
            var tasks = new List<Task>();
            var dateWithZeroTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0);
            var dateInString = $"'{dateWithZeroTime.ToString("yyyy-MM-dd HH:mm:ss.fff")}'";

            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();
                var rowCount = await CountInventory(dateInString, db);

                if(rowCount == 0) return _inventoryList;

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

        public Dictionary<string, decimal> GetDictionayOfTotalInventory(List<Inventory> inventories)
        {
            var inventoryDictionary = inventories.GroupBy(x => x.Sku).ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Inv)
            );

            return inventoryDictionary;
        }
    }
}
