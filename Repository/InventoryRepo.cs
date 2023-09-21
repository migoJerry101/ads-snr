using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using ads.Utility;

namespace ads.Repository
{
    public class InventoryRepo : IInvetory
    {
        private readonly IOpenQuery _openQuery;
        private readonly ILogs _logs ;

        private readonly DateConvertion dateConvertion = new DateConvertion();

        public InventoryRepo(IOpenQuery openQuery, ILogs logs)
        { 
            _logs = logs;
            _openQuery = openQuery;
        }

        //Get Inventory
        public async Task<List<Inventory>> GetInventoryAsync(string start, string end)
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
                    await db.OpenAsync(); // Open the connection asynchronously

                    //Implement for checking
                    //var TBLSTR = await ListOfAllStore(db);
                    var INVSMST = await _openQuery.ListOfAllSKu(db);
                    var CSHDET = await _openQuery.ListOfSales(db, start, end);
                    var INVBAL = await _openQuery.ListIventory(db);

                    //var store = TBLSTR.ToDictionary(x => x.STRNUM);

                    foreach (var item in INVSMST)
                    {
                        var DATE = "";

                        Inventory Olde;

                        var result = INVBAL.Where(x => x.INUMBR2 == item.INUMBR);

                        //store.TryGetValue(item2.INUMBR2);

                        if (result.Any())
                        {
                            foreach (var item2 in result)
                            {
                                var result2 = CSHDET.Where(x => x.CSSKU == item2.INUMBR2);

                                if (result2.Any())
                                {
                                    foreach (var item3 in result2)
                                    {
                                        DATE = item2.CSDATE;

                                        Olde = new Inventory
                                        {
                                            Sku = item.INUMBR,
                                            Clubs = item3.CSSTOR,
                                            Inv = item2.IBHAND,
                                            Date = dateConvertion.ConvertStringDate(item3.CSDATE),
                                        };

                                        ListInventory.Add(Olde);
                                    }
                                }
                                else
                                {
                                    Olde = new Inventory
                                    {
                                        Sku = item.INUMBR,
                                        Clubs = item2.ISTORE,
                                        Inv = item2.IBHAND,
                                        Date = dateConvertion.ConvertStringDate(start),
                                    };

                                    ListInventory.Add(Olde);
                                }
                            }
                        }
                        else
                        {
                            Olde = new Inventory
                            {
                                Sku = item.INUMBR,
                                Clubs = "Null",
                                Inv = 0,
                                Date = dateConvertion.ConvertStringDate(start),
                            };

                            ListInventory.Add(Olde);
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
                string TotalRows = TotalInventory(start, end);

                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Inventory",
                    Message = "Total Rows Inserted : " + TotalRows + "",
                    Record_Date = start
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
                    Message = "Inventory : " + e.Message + " ",
                    Record_Date = start
                });

                _logs.InsertLogs(Log);

                return ListCsDate;
            }
        }

        //list Inventory 
        public async Task<List<Inventory>> ListInv(string dateListString, OledbCon db)
        {
            List<Inventory> list = new List<Inventory>();

            var pageSize = 400000;

            // Get the total count of rows for your date filter
            var rowCount = await CountInventory(dateListString, db);

            // Calculate the total number of pages
            var totalPages = (int)Math.Ceiling((double)rowCount / pageSize);
            for (int pageNumber = 0; pageNumber < totalPages; pageNumber++)
            {
                int offset = pageSize * pageNumber;

                //string query = "select * from tbl_inv where Date in (" + dateListString + ") ";   
                string query = "select * from tbl_inv where Date in (" + dateListString + ") " +
                        "ORDER BY Date OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY ";
                //string query = "select * from tbl_inv where Date = '230911' ";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Inventory Olde = new Inventory
                            {
                                Clubs = reader["Clubs"].ToString(),
                                Sku = reader["Sku"].ToString(),
                                Inv = Convert.ToDecimal(reader["Inventory"].ToString()),
                                Date = Convert.ToDateTime(reader["Date"].ToString()),
                            };

                            Console.WriteLine(reader["Date"].ToString());
                            list.Add(Olde);
                        }
                    }
                }
            }
            return list.ToList();
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

            string query = "select COUNT(*) as Count from tbl_inv where Date in (" + dateListString + ") ";

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
    }
}
