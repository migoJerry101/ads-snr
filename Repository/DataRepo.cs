using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Quartz;

using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.IO;
using Newtonsoft.Json;
using ads.Models.Data;
using ads.Data;
using Dapper;
using static System.Net.WebRequestMethods;
//Final Code
namespace ads.Repository
{
    public class DataRepo : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            string id = Guid.NewGuid().ToString();
            string message = "This job will be executed again at: " +
            context.NextFireTimeUtc.ToString();

            try
            {
                // Get the current date
                DateTime currentDate = DateTime.Now;

                // Subtract one day
                DateTime previousDate = currentDate.AddDays(-1);

                ////////Actual Record or Final Setup
                string startDate = previousDate.ToString("yyMMdd");
                string endDate = previousDate.ToString("yyMMdd");

                //string startDate = "230916";
                //string endDate = "230916";

                await GetInventoryAsync(startDate, endDate);
                await GetSalesAsync(startDate, endDate);

                await GetComputation();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            //return await Task.CompletedTask;
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

                    var inventorys = await ListIventory(db);
                    var skus = await ListOfAllSKu(db);
                    var listOfSales = await ListOfSales(db, start, end);

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
                                    Sales = item.CSQTY,
                                    Date = item2.CSDATE,
                                };

                                listOfOledb.Add(Olde);
                            }
                        }
                        else
                        {
                            var result3 = inventorys.Where(x=> x.INUMBR2 == item.INUMBR);

                            foreach (var item2 in result2)
                            {
                                Olde = new DataRows
                                {
                                    Sku = item.INUMBR,
                                    Clubs = item2.CSSTOR,
                                    Sales = 0,
                                    Date = start,
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
                            dataTable.Columns.Add("Date", typeof(string));

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

                InsertLogs(Log);

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
                    Message = "Sales : "+e.Message+"",
                    Record_Date = start
                });

                InsertLogs(Log);

                return listOfOledb;
            }


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
                    var INVSMST = await ListOfAllSKu(db);
                    var CSHDET = await ListOfSales(db, start, end);
                    var INVBAL = await ListIventory(db);

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
                                            Date = item3.CSDATE,
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
                                        Date = start,
                                    };

                                    ListInventory.Add(Olde);
                                }
                            }
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
                            dataTable.Columns.Add("Date", typeof(string));
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

                InsertLogs(Log);

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
                    Message = "Inventory : "+e.Message+" ",
                    Record_Date = start
                });

                InsertLogs(Log);

                return ListCsDate;
            }


           
        }
        //Insert Logs
        public void InsertLogs(List<Logging> logging)
        {
            using (MsSqlCon db = new MsSqlCon())
            {
                String query = "INSERT INTO tbl_logs (StartLogs,EndLogs,Action,Message,Record_Date) VALUES (@StartLogs,@EndLogs,@Action, @Message,@Record_Date)";

                using (SqlCommand command = new SqlCommand(query, db.Con))
                {
                    command.Parameters.Add("@StartLogs", SqlDbType.DateTime);
                    command.Parameters.Add("@EndLogs", SqlDbType.DateTime);
                    command.Parameters.Add("@Action", SqlDbType.VarChar);
                    command.Parameters.Add("@Message", SqlDbType.VarChar);
                    command.Parameters.Add("@Record_Date", SqlDbType.VarChar);

                    foreach (var rawData in logging)
                    {
                        command.Parameters["@StartLogs"].Value = rawData.StartLog;
                        command.Parameters["@EndLogs"].Value = rawData.EndLog;
                        command.Parameters["@Action"].Value = rawData.Action;
                        command.Parameters["@Message"].Value = rawData.Message;
                        command.Parameters["@Record_Date"].Value = rawData.Record_Date;

                        int result = command.ExecuteNonQuery();

                        if (result <= 0)
                        {
                            Console.WriteLine("Error inserting data into Database!");
                        }
                    }
                }
            }

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

        public async Task<List<TotalADS>> GetComputation()
        {
            List<TotalADS> returnlist = new List<TotalADS>();

            //Start Logs
            List <Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            List<DataRows> listData = new List<DataRows>();

            DateTime currentDate = DateTime.Now;

            string startDate = currentDate.ToString("yyMMdd");
           // string startDate = "230913";

           //Date Ranges of Computation of 56 days
            string dateListString = string.Join(",", DateCompute(startDate).Select(date => $"'{date}'"));
            dateListString = dateListString.TrimEnd(',');

            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                //list of Inventory within 56 days in Local DB
                var listInventoryResult = await ListInv(dateListString, db);
                //list of Sales within 56 days in Local DB
                var listSalesResult = await ListSales(dateListString, db);

                //Per SKU
                await GetTotalApdAsync(listInventoryResult, listSalesResult, dateListString);
                //Per Store
                await GetTotalSkuAndClubsAsync(listInventoryResult, listSalesResult, dateListString);

            }
            return returnlist;
        }
        
        public async Task<List<TotalADS>> GetTotalApdAsync(List<Inventory> listInventoryResult, List<DataRows> listSalesResult, string dateListString)
        {
            //Start Logs
            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            List<DataRows> listData = new List<DataRows>();

            string[] dateParts = dateListString.Split(',');
            string fistDatePart = dateParts.FirstOrDefault();
            string lastDatePart = dateParts.LastOrDefault();
            string firstDate = fistDatePart.Trim('\'');
            string lastDate = lastDatePart.Trim('\'');

            List<TotalADS> totalAPDs = new List<TotalADS>();

            try
            {
                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();

                    //var listInv = await ListInv(dateListString, db);

                    //var listDataResult = await ListData(dateListString, db);

                    var joinDataInv = listSalesResult.Join(
                         listInventoryResult,
                         x => x.Sku,
                         y => y.Sku,
                         (x, y) => new DataRows
                         {
                             Clubs = x.Clubs,
                             Sku = x.Sku,
                             Inventory = x.Inventory,
                             Sales = (x.Sales > 0) ? x.Sales : 0,
                             Date = x.Date
                         });

                    //GroupBy SKU
                    var groupedData = joinDataInv.GroupBy(item => new { item.Sku, item.Date });

                    //GroupBy SKU
                    listData = groupedData.SelectMany(group => group).DistinctBy(item => new { item.Sku, item.Date }).ToList();


                    //Filter sku and sum of sales
                    var groupedBy = listData.GroupBy(x => x.Sku).ToDictionary(
                                     group => group.Key,
                                     group => group.Sum(item => item.Sales)
                                 );

                    List<TotalDiv> divs = new List<TotalDiv>();

                    //Distinct of SKU
                    var filter = listData.Select(x => new
                    {
                        Sku = x.Sku,
                    }).Distinct().ToList();

                    foreach (var f in filter)
                    {
                        var checkSku = listData.Where(x => x.Sku == f.Sku && x.Sales == 0 && x.Inventory == 0);
                        var totalDiv = listData.Select(x => x.Date).Distinct().Count();

                        if (checkSku.Any())
                        {
                            foreach (var s in checkSku)
                            {
                                totalDiv -= 1;
                            }

                            divs.Add(new TotalDiv { sku = f.Sku, total = totalDiv });
                        }
                        else
                        {
                            divs.Add(new TotalDiv { sku = f.Sku, total = totalDiv });
                        }

                        decimal result = 0;

                        groupedBy.TryGetValue(f.Sku.ToString(), out decimal totalSales);

                        if (totalSales >= long.MinValue && totalSales <= long.MaxValue)
                        {
                            result = (long)totalSales;
                        }

                        decimal totalAPDDecimal = 0;

                        var search = divs.SingleOrDefault(x => x.sku == f.Sku);

                        if (search != null)
                        {
                            if (totalDiv != 0)
                            {

                                totalAPDDecimal = Math.Round(result / totalDiv, 2);
                                Console.WriteLine(totalAPDDecimal);

                                groupedBy.TryGetValue(f.Sku.ToString(), out decimal salesOut);
                                long totalAPD = Convert.ToInt64(totalAPDDecimal);
                                Console.WriteLine(totalAPD);

                                totalAPDs.Add(new TotalADS
                                {
                                    Divisor = totalDiv,
                                    Sales = salesOut,
                                    Ads = totalAPD,
                                    Date = lastDate,
                                    Sku = f.Sku.ToString(),
                                    StartDate = lastDate,
                                    EndDate = firstDate
                                });
                            }
                        }
                    }

                    Console.WriteLine(totalAPDs);

                    //Bluk insert
                    using (var transaction = db.Con.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "tbl_totalAds";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Sku", typeof(string));
                            dataTable.Columns.Add("Sales", typeof(decimal));
                            //dataTable.Columns.Add("Inventory", typeof(decimal));
                            dataTable.Columns.Add("Divisor", typeof(string));
                            //dataTable.Columns.Add("Date", typeof(string));
                            dataTable.Columns.Add("Ads", typeof(decimal));
                            dataTable.Columns.Add("StartDate", typeof(string));
                            dataTable.Columns.Add("EndDate", typeof(string));

                            foreach (var rawData in totalAPDs)
                            {
                                var row = dataTable.NewRow();
                                row["Sku"] = rawData.Sku;
                                row["Sales"] = rawData.Sales;
                                //row["Inventory"] = rawData.Inventory;
                                row["Divisor"] = rawData.Divisor;
                                //row["Date"] = rawData.Date;
                                row["Ads"] = rawData.Ads;
                                row["StartDate"] = rawData.StartDate;
                                row["EndDate"] = rawData.EndDate;
                                dataTable.Rows.Add(row);
                            }
                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        transaction.Commit();
                    }

                    DateTime endLogs = DateTime.Now;
                    Log.Add(new Logging
                    {
                        StartLog = startLogs,
                        EndLog = endLogs,
                        Action = "Total ADS",
                        Message = "Total Sku Inserted : " + filter.Count + "",
                        Record_Date = lastDate
                    });

                    InsertLogs(Log);

                    return totalAPDs;
                }
            }
            catch (Exception e)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "Total Sku  : " + e.Message + "",
                    Record_Date = lastDate
                });

                InsertLogs(Log);

                return totalAPDs;
            }
        }

        public async Task<List<TotalADS>> GetTotalSkuAndClubsAsync(List<Inventory> listInv, List<DataRows> listDataResult, string dateListString)
        {
            //Start Logs
            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            List<DataRows> listData = new List<DataRows>();

            string[] dateParts = dateListString.Split(',');
            string fistDatePart = dateParts.FirstOrDefault();
            string lastDatePart = dateParts.LastOrDefault();
            string firstDate = fistDatePart.Trim('\'');
            string lastDate = lastDatePart.Trim('\'');

            List<TotalADS> totalAPDs = new List<TotalADS>();

            try 
            {
                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();

                    //var listInv = await ListInv(dateListString, db);

                    //var listDataResult = await ListData(dateListString, db);

                    var joinDataInv = listDataResult.Join(
                         listInv,
                         x => x.Sku,
                         y => y.Sku,
                         (x, y) => new DataRows
                         {
                             Clubs = x.Clubs,
                             Sku = x.Sku,
                             Inventory = x.Inventory,
                             Sales = (x.Sales > 0) ? x.Sales : 0,
                             Date = x.Date
                         });

                    var groupedData = joinDataInv.GroupBy(item => new { item.Sku, item.Clubs, item.Date });

                    listData = groupedData.SelectMany(group => group).DistinctBy(item => new { item.Sku, item.Clubs, item.Date }).ToList();

                    //Filter sku and sum of sales
                    var groupedBy = listData.GroupBy(x => new { x.Sku, x.Clubs }).ToDictionary(
                                         group => group.Key,
                                         group => group.Sum(item => item.Sales)
                                     );

                    List<TotalDiv> divs = new List<TotalDiv>();

                    //Distinct of SKU
                    var filter = listData.Select(x => new
                    {
                        Sku = x.Sku,
                        Clubs = x.Clubs
                    }).Distinct().ToList();

                    foreach (var f in filter)
                    {
                        var checkSku = listData.Where(x => x.Sku == f.Sku && x.Clubs == f.Clubs && x.Sales == 0 && x.Inventory == 0);
                        var totalDiv = listData.Select(x => x.Date).Distinct().Count();

                        if (checkSku.Any())
                        {
                            foreach (var s in checkSku)
                            {
                                totalDiv -= 1;
                            }

                            divs.Add(new TotalDiv { sku = f.Sku, clubs = f.Clubs, total = totalDiv });
                        }
                        else
                        {
                            divs.Add(new TotalDiv { sku = f.Sku, clubs = f.Clubs, total = totalDiv });
                        }

                        decimal result = 0;

                        // Create a key with both Sku and Clubs
                        var key = new { Sku = f.Sku, Clubs = f.Clubs };

                        groupedBy.TryGetValue(key, out decimal totalSales);

                        //groupedBy.TryGetValue(f.Sku,f.Clubs, out decimal totalSales);


                        if (totalSales >= long.MinValue && totalSales <= long.MaxValue)
                        {
                            result = (long)totalSales;
                        }

                        decimal totalAPDDecimal = 0;

                        var search = divs.SingleOrDefault(x => x.sku == f.Sku && x.clubs == f.Clubs);

                        if (search != null)
                        {
                            //totalAPDDecimal = Math.Round(result / totalDiv, 2);
                            //Console.WriteLine(totalAPDDecimal);

                            if (totalDiv != 0)
                            {
                                totalAPDDecimal = Math.Round(result / totalDiv, 2);
                                Console.WriteLine(totalAPDDecimal);

                                groupedBy.TryGetValue(key, out decimal salesOut);
                                long totalAPD = Convert.ToInt64(totalAPDDecimal);
                                Console.WriteLine(totalAPD);

                                totalAPDs.Add(new TotalADS
                                {
                                    Divisor = totalDiv,
                                    Sales = salesOut,
                                    Ads = totalAPD,
                                    Date = lastDate,
                                    Sku = f.Sku,
                                    Clubs = f.Clubs,
                                    StartDate = lastDate,
                                    EndDate = firstDate
                                });
                            }
                        }
                    }

                    Console.WriteLine(totalAPDs);

                    //Bluk insert
                    using (var transaction = db.Con.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "tbl_totaladsperclubs";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Sku", typeof(string));
                            dataTable.Columns.Add("Clubs", typeof(string));
                            dataTable.Columns.Add("Sales", typeof(decimal));
                            dataTable.Columns.Add("Divisor", typeof(string));
                            dataTable.Columns.Add("Ads", typeof(decimal));
                            dataTable.Columns.Add("StartDate", typeof(string));
                            dataTable.Columns.Add("EndDate", typeof(string));

                            foreach (var rawData in totalAPDs)
                            {
                                var row = dataTable.NewRow();
                                row["Sku"] = rawData.Sku;
                                row["Clubs"] = rawData.Clubs;
                                row["Sales"] = rawData.Sales;
                                row["Divisor"] = rawData.Divisor;
                                row["Ads"] = rawData.Ads;
                                row["StartDate"] = rawData.StartDate;
                                row["EndDate"] = rawData.EndDate;
                                dataTable.Rows.Add(row);
                            }
                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        transaction.Commit();
                    }

                    DateTime endLogs = DateTime.Now;
                    Log.Add(new Logging
                    {
                        StartLog = startLogs,
                        EndLog = endLogs,
                        Action = "Total ADS",
                        Message = "Total Clubs Inserted : " + filter.Count + "",
                        Record_Date = lastDate
                    });

                    InsertLogs(Log);

                    return totalAPDs;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "Total Clubs : " + e.Message + "",
                    Record_Date = lastDate
                });

                InsertLogs(Log);

                return totalAPDs;
            }
        }

        //list Inventory 
        public async Task<List<Inventory>> ListInv(string dateListString, OledbCon db)
        {
            List<Inventory> list = new List<Inventory>();

            string query = "select * from tbl_inv where Date in (" + dateListString + ") ";
            //string query = "select * from tbl_inv where Date = '230911' ";

            using (SqlCommand cmd = new SqlCommand(query, db.Con))
            {
                cmd.CommandTimeout = 18000;
                // Implement of Pagination per Clubs
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Inventory Olde = new Inventory
                        {
                            Sku = reader["Sku"].ToString(),
                            Inv = Convert.ToDecimal(reader["Inventory"].ToString()),
                            Date = reader["Date"].ToString(),
                        };

                        Console.WriteLine(reader["Date"].ToString());
                        list.Add(Olde);
                    }
                }
            }

            return list.ToList();
        }
        //list Data
        public async Task<List<DataRows>> ListSales(string dateListString, OledbCon db)
        {
            List<DataRows> list = new List<DataRows>();

            var pageSize = 400000;

            // Get the total count of rows for your date filter
            var rowCount = await Countdata(dateListString, db);

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
                                Date = reader["Date"].ToString(),
                            };
                            Console.WriteLine(reader["Clubs"].ToString());
                            list.Add(Olde);
                        }
                    }
                }
            }

            return list;
        }

        //TotalCount of Data
        public async Task<int> Countdata(string dateListString, OledbCon db)
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

        //List of Date within 56 days 
        public List<string> DateCompute(string startDateStr)
        {
            List<string> listDate = new List<string>();

            // Get the current date
            DateTime currentDate = DateTime.ParseExact(startDateStr, "yyMMdd", CultureInfo.InvariantCulture);

            // Subtract one day
            DateTime previousDate = currentDate.AddDays(-1);

            DateTime endDate = previousDate;
            DateTime startDate = endDate.AddDays(-55);


            List<DateTime> datesInRange = GetDatesInRange(startDate, endDate);

            foreach (DateTime date in datesInRange)
            {
                Console.WriteLine(date.ToString("yyMMdd"));

                listDate.Add(date.ToString("yyMMdd"));
            }

            return listDate.ToList();
        }
        public List<DateTime> GetDatesInRange(DateTime startDate, DateTime endDate)
        {
            List<DateTime> datesInRange = new List<DateTime>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                datesInRange.Add(date);
            }

            return datesInRange;
        }

        //ListINVMST - List of All SKU Filter ISTYPE = ''01'' AND IDSCCD IN (''A'',''I'',''D'',''P'') AND IATRB1 IN (''L'',''I'',''LI'')
        //ISTYPE - Type of SKU
        //IDSCCD - Status of SKU
        //IATRB1 - Attribute of SKU
        public async Task<List<GeneralModel>> ListOfAllSKu(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            string query = "select * from Openquery([snr], 'SELECT INUMBR from MMJDALIB.INVMST WHERE ISTYPE = ''01'' AND IDSCCD IN (''A'',''I'',''D'',''P'') AND IATRB1 IN (''L'',''I'',''LI'')')";

            using (SqlCommand cmd = new SqlCommand(query, db.Con))
            {
                cmd.CommandTimeout = 18000;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        GeneralModel Olde = new GeneralModel
                        {
                            INUMBR = reader["INUMBR"].ToString()
                        };

                        list.Add(Olde);
                    }
                }
            }

            return list.ToList();
        }

        //ListCSHDET - List of Sales GroupBy SKu,,store,Date
        public async Task<List<GeneralModel>> ListOfSales(OledbCon db,string start, string end)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            string query = "select * from Openquery([snr], 'SELECT CSSKU, CSDATE, MAX(CSSTOR) CSSTOR, SUM(CSQTY) CSQTY from MMJDALIB.CSHDET where CSDATE BETWEEN ''" + start + "'' AND ''" + end + "'' GROUP BY CSSKU ,CSDATE ')";
            //string query = "select * from Openquery([snr], 'SELECT CSSKU, CSDATE, CSSTOR, SUM(CSQTY) CSQTY from MMJDALIB.CSHDET where CSDATE BETWEEN ''" + start + "'' AND ''" + end + "'' GROUP BY CSSKU, CSSTOR ,CSDATE ')";

            using (SqlCommand cmd = new SqlCommand(query, db.Con))
            {
                cmd.CommandTimeout = 18000;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        GeneralModel Olde = new GeneralModel
                        {
                            CSDATE = reader["CSDATE"].ToString(),
                            CSSTOR = reader["CSSTOR"].ToString(),
                            CSSKU = reader["CSSKU"].ToString(),
                            CSQTY = Convert.ToDecimal(reader["CSQTY"].ToString())
                        };

                        list.Add(Olde);
                    }
                }
            }

            return list.ToList();
        }

        //ListINVBAL - List of Inventory Groupby SKU
        public async Task<List<GeneralModel>> ListIventory(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

             string query = "select * from Openquery([snr], 'SELECT INUMBR ,Max(ISTORE) ISTORE , CASE WHEN SUM(IBHAND) < 0 THEN 0 ELSE SUM(IBHAND) END AS IBHAND from MMJDALIB.INVBAL GROUP BY INUMBR')";
            //string query = "select * from Openquery([snr], 'SELECT INUMBR ,ISTORE, CASE WHEN MAX(IBHAND) < 0 THEN 0 ELSE MAX(IBHAND) END AS IBHAND from MMJDALIB.INVBAL GROUP BY INUMBR ,ISTORE')";
            //string query = "select * from Openquery([snr], 'SELECT MST.INUMBR, MAX(BAL.ISTORE) ISTORE, SUM(BAL.IBHAND) IBHAND from MMJDALIB.INVMST as MST " +
            //    "INNER JOIN MMJDALIB.INVBAL as BAL on MST.INUMBR = BAL.INUMBR " +
            //    "WHERE MST.ISTYPE = ''01'' AND MST.IDSCCD IN (''A'',''I'',''D'',''P'') AND MST.IATRB1 IN (''L'',''I'',''LI'') " +
            //    "GROUP BY MST.INUMBR')";

            using (SqlCommand cmd = new SqlCommand(query, db.Con))
            {
                cmd.CommandTimeout = 18000;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        GeneralModel Olde = new GeneralModel
                        {
                            INUMBR2 = reader["INUMBR"].ToString(),
                            ISTORE = reader["ISTORE"].ToString(),
                            IBHAND = Convert.ToDecimal(reader["IBHAND"].ToString())
                        };

                        list.Add(Olde);
                    }
                }
            }

            return list.ToList();
        }

        //ListTBLSTR - List of ALL STORE with Filter STPOLL = ''Y'' AND STSDAT > 0
        //STPOLL - Identify Store Open
        // STSDAT - Date Open of Store
        public async Task<List<GeneralModel>> ListOfAllStore(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            string query = "select * from Openquery([snr], 'SELECT STRNUM from MMJDALIB.TBLSTR WHERE STPOLL = ''Y'' AND STSDAT > 0')";

            using (SqlCommand cmd = new SqlCommand(query, db.Con))
            {
                cmd.CommandTimeout = 18000;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        GeneralModel Olde = new GeneralModel
                        {
                            STRNUM = reader["STRNUM"].ToString()
                        };

                        list.Add(Olde);
                    }
                }
            }

            return list.ToList();
        }

    }
}
