using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Utility;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ads.Repository
{
    public class AdsRepo : IAds
    {
        private readonly OpenQueryRepo openQuery = new OpenQueryRepo();
        private readonly DateConvertion dateConvertion = new DateConvertion();
        private readonly LogsRepo localQuery = new LogsRepo();
        private readonly DateComputeUtility dateCompute = new DateComputeUtility();

        private readonly ISales _sales;
        private readonly IInvetory _invetory;


        public AdsRepo(ISales sales, IInvetory invetory)
        {
            _sales = sales;
            _invetory = invetory;
        }


        public async Task<List<TotalADS>> GetComputation(string stringDate)
        {
            List<TotalADS> returnlist = new List<TotalADS>();

            //Start Logs
            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            List<DataRows> listData = new List<DataRows>();

            DateTime currentDate = DateTime.Now;

            //var startDate = currentDate;
            //string startDate = "230913";


             DateTime startDate = Convert.ToDateTime(stringDate);
            //DateTime startDate = Convert.ToDateTime("2023-05-22 00:00:00.000");

            //Date Ranges of Computation of 56 days
            string dateListString = string.Join(",", dateCompute.DateCompute(startDate).Select(date => $"'{date}'"));
            dateListString = dateListString.TrimEnd(',');


            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                //list of Inventory within 56 days in Local DB
                var listInventoryResult = await _invetory.ListInv(dateListString, db);
                //list of Sales within 56 days in Local DB
                var listSalesResult = await _sales.ListSales(dateListString, db);

                //Per SKU
                await GetTotalApdAsync(listInventoryResult, listSalesResult, dateListString);
                ////Per Store
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
                //var listInv = await ListInv(dateListString, db);

                //var listDataResult = await ListData(dateListString, db);

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
                        else
                        {
                            //totalAPDDecimal = Math.Round(result / totalDiv, 2);
                            //Console.WriteLine(totalAPDDecimal);

                            ////groupedBy.TryGetValue(f.Sku.ToString(), out decimal salesOut);
                            //long totalAPD = Convert.ToInt64(totalAPDDecimal);
                            //Console.WriteLine(totalAPD);

                            totalAPDs.Add(new TotalADS
                            {
                                Divisor = totalDiv,
                                Sales = 0,
                                Ads = 0,
                                Date = lastDate,
                                Sku = f.Sku.ToString(),
                                StartDate = lastDate,
                                EndDate = firstDate
                            });

                        }
                    }
                }

                Console.WriteLine(totalAPDs);

                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();
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
                        Message = "Total Sku Inserted : " + totalAPDs.Count() + "",
                        Record_Date = lastDate
                    });

                    localQuery.InsertLogs(Log);

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

                localQuery.InsertLogs(Log);

                return totalAPDs;
            }

        }

        public async Task<List<TotalADS>> GetTotalSkuAndClubsAsync(List<Inventory> listInventoryResult, List<DataRows> listSalesResult, string dateListString)
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
                        else
                        {
                            totalAPDs.Add(new TotalADS
                            {
                                Divisor = totalDiv,
                                Sales = 0,
                                Ads = 0,
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

                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();


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
                        Message = "Total Clubs Inserted : " + totalAPDs.Count() + "",
                        Record_Date = lastDate
                    });

                    localQuery.InsertLogs(Log);

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

                localQuery.InsertLogs(Log);

                return totalAPDs;
            }
        }
    }
}
