using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Utility;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ads.Repository
{
    public class SalesRepo : ISales
    {
        private readonly IOpenQuery _openQuery;
        private readonly ILogs _logs;

        private readonly DateConvertion dateConvertion = new DateConvertion();

        public SalesRepo(IOpenQuery openQuery, ILogs logs)
        { 
            _openQuery = openQuery;
            _logs = logs;
        }

        //Get Sales
        public async Task<List<DataRows>> GetSalesAsync(string start, string end)
        {
            List<DataRows> transformedData = new List<DataRows>();

            List<DataRows> listOfOledb = new List<DataRows>();

            List<DataRows> listOfTBLSTR = new List<DataRows>();

            List<DataRows> listOfINVMST = new List<DataRows>();

            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            try
            {

                //OleDb Select Query
                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();

                    //var groupedCSHDET = CSHDET.GroupBy(x => x.CSSKU).ToDictionary(group => group.Key, group => group.ToList());
                    ////var dicInv = CSHDET.ToDictionary(x => x.CSSKU, y => new { y.CSQTY, y.CSDATE, y.CSSTOR });

                    //foreach (var item in INVSMST)
                    //{
                    //    if (groupedCSHDET.TryGetValue(item.INUMBR, out var salesList))
                    //    {
                    //        DataRows Olde = new DataRows
                    //        {
                    //            Clubs = item.CSSTOR,
                    //            Sku = item.INUMBR,
                    //            Sales = salesList.Sum(sales => sales.CSQTY),
                    //            Date = salesList.First().CSDATE, // Assuming you want the date from the first matching record
                    //        };

                    //        listOfOledb.Add(Olde);
                    //    }
                    //}

                    var inventorys = await _openQuery.ListIventory(db);
                    var skus = await _openQuery.ListOfAllSKu(db);
                    var listOfSales = await _openQuery.ListOfSales(db, start, end);

                    foreach (var item in skus)
                    {
                        var DATE = "";
                        DataRows Olde;

                        var result2 = listOfSales.Where(x => x.CSSKU == item.INUMBR);

                        if (result2.Any())
                        {
                            foreach (var item2 in result2)
                            {
                                DATE = item2.CSDATE;

                                Olde = new DataRows
                                {
                                    Sku = item.INUMBR,
                                    Clubs = item2.CSSTOR,
                                    Sales = item2.CSQTY,
                                    Date = dateConvertion.ConvertStringDate(item2.CSDATE),
                                };

                                listOfOledb.Add(Olde);
                            }
                        }
                        else
                        {
                            var result3 = inventorys.Where(x => x.INUMBR2 == item.INUMBR);
                            if (result3.Any())
                            {
                                foreach (var item2 in result3)
                                {
                                    Olde = new DataRows
                                    {
                                        Sku = item.INUMBR,
                                        Clubs = item2.ISTORE,
                                        Sales = 0,
                                        Date = dateConvertion.ConvertStringDate(start),
                                    };

                                    listOfOledb.Add(Olde);
                                }
                            }
                            else
                            {
                                Olde = new DataRows
                                {
                                    Sku = item.INUMBR,
                                    Clubs = "Null",
                                    Sales = 0,
                                    Date = dateConvertion.ConvertStringDate(start),
                                };

                                listOfOledb.Add(Olde);
                            }
                        }
                    }

                    //Bluk insert in tbl_Data table
                    using (var transaction = db.Con.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "tbl_data";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Clubs", typeof(string));
                            dataTable.Columns.Add("Sku", typeof(string));
                            //dataTable.Columns.Add("Inventory", typeof(decimal));
                            dataTable.Columns.Add("Sales", typeof(decimal));
                            dataTable.Columns.Add("Date", typeof(DateTime));

                            foreach (var rowData in listOfOledb)
                            {
                                var row = dataTable.NewRow();
                                row["Clubs"] = rowData.Clubs;
                                row["Sku"] = rowData.Sku;
                                //row["Inventory"] = rowData.Inventory;
                                row["Sales"] = rowData.Sales;
                                row["Date"] = rowData.Date;
                                dataTable.Rows.Add(row);
                            }
                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        transaction.Commit();
                    }
                }

                var TotalRows = TotalSales(start, end);
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Sales",
                    Message = "Total Rows Inserted : " + TotalRows + "",
                    Record_Date = start
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
                    Message = "Sales : " + e.Message + "",
                    Record_Date = start
                });

                _logs.InsertLogs(Log);

                return listOfOledb;
            }


        }

        //list Data
        public async Task<List<DataRows>> ListSales(string dateListString, OledbCon db)
        {
            List<DataRows> list = new List<DataRows>();

            var pageSize = 400000;

            // Get the total count of rows for your date filter
            var rowCount = await CountSales(dateListString, db);

            // Calculate the total number of pages
            var totalPages = (int)Math.Ceiling((double)rowCount / pageSize);

            for (int pageNumber = 0; pageNumber < totalPages; pageNumber++)
            {
                int offset = pageSize * pageNumber;

                string query = "select * from tbl_data where Date in (" + dateListString + ") " +
                    "ORDER BY Date OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY ";
                //string query = "select * from tbl_data where Date = '230911' " +
                //    "ORDER BY Date OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY ";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            DataRows Olde = new DataRows
                            {
                                Clubs = reader["Clubs"].ToString(),
                                Sku = reader["Sku"].ToString(),
                                Sales = Convert.ToDecimal(reader["Sales"].ToString()),
                                Date = Convert.ToDateTime(reader["Date"].ToString()),
                            };
                            Console.WriteLine(reader["Clubs"].ToString());
                            list.Add(Olde);
                        }
                    }
                }
            }

            return list;
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

    }
}
