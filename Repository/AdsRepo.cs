﻿using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ads.Repository
{
    public class AdsRepo : IAds
    {
        private readonly OpenQueryRepo openQuery = new OpenQueryRepo();
        private readonly DateConvertion dateConvertion = new DateConvertion();
        private readonly LogsRepo localQuery = new LogsRepo();
        private readonly DateComputeUtility dateCompute = new DateComputeUtility();
        public List<TotalADS> _totalAdsChain = new List<TotalADS>();
        public List<TotalADS> _totalAdsClubs = new List<TotalADS>();

        private readonly ISales _sales;
        private readonly IInvetory _invetory;
        private readonly IOpenQuery _openQuery;

        public AdsRepo(ISales sales, IInvetory invetory, IOpenQuery openQuery)
        {
            _sales = sales;
            _invetory = invetory;
            _openQuery = openQuery;
        }


        public async Task<List<TotalADS>> GetComputation(string stringDate)
        {
            List<TotalADS> returnlist = new List<TotalADS>();

            //Start Logs
            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            List<Sale> listData = new List<Sale>();

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

        public async Task<List<TotalADS>> GetTotalApdAsync(List<Inventory> listInventoryResult, List<Sale> listSalesResult, string dateListString)
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
                                      Inventory = y.Inv,
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
                        Record_Date = dateConvertion.ConvertStringDate(lastDate)
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
                    Message = "GetTotalApdAsync  : " + e.Message + "",
                    Record_Date = dateConvertion.ConvertStringDate(lastDate)
                });

                localQuery.InsertLogs(Log);

                return totalAPDs;
            }

        }

        public async Task<List<TotalADS>> GetTotalSkuAndClubsAsync(List<Inventory> listInventoryResult, List<Sale> listSalesResult, string dateListString)
        {
            //Start Logs
            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            var listData = new List<DataRows>();

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
                         Inventory = y.Inv,
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
                        Record_Date = dateConvertion.ConvertStringDate(lastDate)
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
                    Message = "GetTotalSkuAndClubsAsync : " + e.Message + "",
                    Record_Date = dateConvertion.ConvertStringDate(lastDate)
                });

                localQuery.InsertLogs(Log);

                return totalAPDs;
            }
        }

        public async Task ComputeAds()
        {
            _totalAdsChain = new List<TotalADS>();
            var Log = new List<Logging>();
            var listData = new List<Sale>();
            var tasks = new List<Task>();
            var tasksPerClubs = new List<Task>();
            var skus = new List<string>();

            //get ads first
            var currentDate = DateTime.Now.AddDays(-1);
            var AdsDate = DateTime.Now.AddDays(-2);
            var CurrentDateWithZeroTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 0, 0, 0, 0);
            var adsStartDate = new DateTime(AdsDate.Year, AdsDate.Month, AdsDate.Day, 0, 0, 0, 0);

            //get all skus for chain
            //get the ads chain for yesterday
            //var totalAdsYesterdayChain = await GetCountAdsChainByDate("'2023-05-15 00:00:00.000'");
            var totalAdsYesterdayChain = await GetCountAdsChainByDate($"'{adsStartDate:yyyy-MM-dd HH:mm:ss.fff}'");


            // Get the total count of rows for your date filter
            var pageSizeChain = (int)Math.Ceiling((double)totalAdsYesterdayChain / 5);
            var totalPagesChain = (int)Math.Ceiling((double)totalAdsYesterdayChain / pageSizeChain);

            for (int pageNumber = 0; pageNumber < totalPagesChain; pageNumber++)
            {
                int offset = pageSizeChain * pageNumber;

                tasks.Add(GetAverageSalesChainByDate($"{adsStartDate:yyyy-MM-dd HH:mm:ss.fff}", pageSizeChain, offset));
                //tasks.Add( GetAverageSalesChainByDate("2023-05-15 00:00:00.000", pageSizeChain, offset));
            }

            await Task.WhenAll(tasks);

            //get one sample from ads chain then get start and end date
            var adsDayZeor = _totalAdsChain[0].EndDate;
            var startDate = _totalAdsChain[0].StartDate;
            string format = "yyyy-MM-dd HH:mm:ss.fff";
            DateTime.TryParseExact(startDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDateOut);
            DateTime.TryParseExact(adsDayZeor, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDateOut);
            TimeSpan difference = startDateOut - endDateOut;
            var daysDifference = difference.Days;

            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                var sku = await _openQuery.ListOfAllSKu(db);
                skus = sku.Select(x => x.INUMBR).ToList();
            }

            //sales for chain
            var SalesToday = await _sales.GetSalesByDate(CurrentDateWithZeroTime); //to add
            var salesDayZero = await _sales.GetSalesByDate(endDateOut); // to subtract

            //sales for clubs
            var salesTodayWithoutNullClubs = SalesToday.Where(i => !i.Clubs.IsNullOrEmpty()).ToList();
            var salesDayZeroWithoutNullClubs = salesDayZero.Where(i => !i.Clubs.IsNullOrEmpty()).ToList();

            //getInventory today and dayzero
            var inventoryToday = await _invetory.GetInventoriesByDate(CurrentDateWithZeroTime);
            var inventoryDayZero = await _invetory.GetInventoriesByDate(endDateOut);

            var inventoryDayZeroWithoutNullClubs = inventoryDayZero.Where(i => !i.Clubs.IsNullOrEmpty()).ToList();
            var inventoryTodayWithoutNullClubs = inventoryToday.Where(c => !c.Clubs.IsNullOrEmpty()).ToList();

            //get ads per store
            var totalAdsDayYesterdayClubs = await GetCountAdsByDate($"'{adsStartDate:yyyy-MM-dd HH:mm:ss.fff}'");


            var skuDictionary = skus.Distinct().ToDictionary(x => x);
            var chainDictionary = _totalAdsChain.Distinct().ToDictionary(c => c.Sku, y => y);

            var salesTotalDictionaryToday = _sales.GetDictionayOfTotalSales(SalesToday);
            var salesTotalDictionaryDayZero = _sales.GetDictionayOfTotalSales(salesDayZero);

            var inventoryTotalDictionaryToday = _invetory.GetDictionayOfTotalInventory(inventoryToday);
            var inventoryTodayDictionaryDayZero = _invetory.GetDictionayOfTotalInventory(inventoryDayZero);

            var adsWithCurrentsales = new List<TotalADS>();

            var newEndDate = endDateOut.AddDays(1);
            var startDateInString = $"{CurrentDateWithZeroTime:yyyy-MM-dd HH:mm:ss.fff}";
            var endDateInString = $"{newEndDate:yyyy-MM-dd HH:mm:ss.fff}";


            foreach (var sku in skus)
            {
                if (chainDictionary.TryGetValue(sku, out var ads))
                {
                    var hasSales = salesTotalDictionaryDayZero.TryGetValue(sku, out var totalSalesOut);
                    var hasInventory = inventoryTodayDictionaryDayZero.TryGetValue(sku, out var totalInvOut);

                    var daysDifferenceOut = dateCompute.GetDifferenceInRange(ads.StartDate, ads.EndDate);

                    if (daysDifferenceOut == 56)
                    {
                        if (totalSalesOut > 0 || totalInvOut > 0)
                        {
                            ads.Sales -= totalSalesOut;
                            ads.Divisor--;
                            ads.EndDate = endDateInString;
                        }
                    }

                    adsWithCurrentsales.Add(ads);
                }
            }

            var adsWithCurrentsalesDictionary = adsWithCurrentsales.ToDictionary(x => x.Sku, y => y);

            adsWithCurrentsales = new List<TotalADS>();

            foreach (var sku in skus)
            {
                var hasSales = salesTotalDictionaryToday.TryGetValue(sku, out var totalSalesOut);
                var hasInventory = inventoryTotalDictionaryToday.TryGetValue(sku, out var totalInvOut);

                if (adsWithCurrentsalesDictionary.TryGetValue(sku, out var ads))
                {
                    if (totalSalesOut > 0 || totalInvOut > 0)
                    {
                        ads.Sales += totalSalesOut;
                        ads.Divisor++;
                        ads.Ads = Math.Round(ads.Sales / ads.Divisor, 2);
                    }
                    
                    ads.StartDate = startDateInString;

                    adsWithCurrentsales.Add(ads);
                }
                else
                {
                    var newAds = new TotalADS()
                    {
                        Divisor = 0,
                        Sales = 0,
                        Ads = 0,
                        Sku = sku,
                        StartDate = startDateInString,
                        EndDate = startDateInString
                    };

                    adsWithCurrentsales.Add(newAds);
                }
            }

            await SaveTotalAdsChain(adsWithCurrentsales, endDateInString);

            var pagesSizeClub = (int)Math.Ceiling((double)totalAdsDayYesterdayClubs / 5);
            var totalPagesClub = (int)Math.Ceiling((double)totalAdsDayYesterdayClubs / pagesSizeClub);

            for (int pageNumber = 0; pageNumber < totalPagesClub; pageNumber++)
            {
                int offset = pagesSizeClub * pageNumber;

                tasksPerClubs.Add(GetAverageSalesPerClubsByDate($"{CurrentDateWithZeroTime:yyyy-MM-dd HH:mm:ss.fff}", pagesSizeClub, offset));
            }

            await Task.WhenAll(tasksPerClubs);

            //create dictionary of perclubs with key clubs + sku
            var totalAdsClubDictionary = _totalAdsClubs.ToDictionary(x => new { x.Sku, x.Clubs });

            var salesTodayWithoutNullClubsDictionary = salesTodayWithoutNullClubs.GroupBy(x => new { x.Sku, x.Clubs }).ToDictionary(group => group.Key, group => group.Sum(y => y.Sales));
            var salesDayZeroWithoutNullClubsDictionary = salesDayZeroWithoutNullClubs.GroupBy(x => new { x.Sku, x.Clubs }).ToDictionary(group => group.Key, group => group.Sum(y => y.Sales));

            var inventoryDayZeroWithoutNullClubsDictionary = inventoryDayZeroWithoutNullClubs.ToDictionary(x => new { x.Sku, x.Clubs }, y => y.Inv);
            //var inventoryTodayWithoutNullClubsDictionary = inventoryTodayWithoutNullClubs.ToDictionary(x => new { x.Sku, x.Clubs }, y => y.Inv);

            var adsPerClubsWithCurrentsales = new List<TotalADS>();

            foreach (var inv in inventoryTodayWithoutNullClubs)
            {
                var hasAds = totalAdsClubDictionary.TryGetValue(new { inv.Sku, inv.Clubs }, out var adsOut);
                salesTodayWithoutNullClubsDictionary.TryGetValue(new { inv.Sku, inv.Clubs }, out var perClubSalesToday);

                if (hasAds)
                {
                    var daysDifferenceOut = dateCompute.GetDifferenceInRange(adsOut.StartDate, adsOut.EndDate);

                    if (daysDifferenceOut == 56)
                    {
                        salesDayZeroWithoutNullClubsDictionary.TryGetValue(new { inv.Sku, inv.Clubs }, out var perClubSalesDayZero);
                        inventoryDayZeroWithoutNullClubsDictionary.TryGetValue(new { inv.Sku, inv.Clubs }, out var perClubInvDayZero);

                        if (perClubSalesDayZero > 0 || perClubInvDayZero > 0)
                        {
                            adsOut.Sales -= perClubSalesDayZero;
                            adsOut.Divisor--;
                            adsOut.EndDate = endDateInString;
                        }
                    }

                    if (perClubSalesToday > 0 || inv.Inv > 0)
                    {
                        adsOut.Sales += perClubSalesToday;
                        adsOut.Divisor++;
                        adsOut.StartDate = startDateInString;
                        adsOut.Ads = Math.Round(adsOut.Sales / adsOut.Divisor, 2);
                    }

                    adsPerClubsWithCurrentsales.Add(adsOut);
                }
                else
                {
                    var newAds = new TotalADS()
                    {
                        Divisor = 0,
                        Sales = perClubSalesToday,
                        Ads = Math.Round(perClubSalesToday / 1, 2),
                        Sku = inv.Sku,
                        Clubs = inv.Clubs,
                        StartDate = startDateInString,
                        EndDate = startDateInString
                    };

                    if (inv.Inv > 0 || perClubSalesToday > 0)
                    {
                        newAds.Divisor = 1;
                    }

                    adsPerClubsWithCurrentsales.Add(newAds);
                }
            }

            await SaveAdsPerClubs(adsPerClubsWithCurrentsales, endDateInString);
        }

        private async Task SaveAdsPerClubs(List<TotalADS> adsPerClubs, string lastDate)
        {
            var Log = new List<Logging>();
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

                            foreach (var rawData in adsPerClubs)
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
                        Message = "Total Clubs Inserted : " + adsPerClubs.Count() + "",
                        Record_Date = dateConvertion.ConvertStringDate(lastDate)
                    });

                    localQuery.InsertLogs(Log);
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
                    Message = "SaveAdsPerClubs  : " + e.Message + "",
                    Record_Date = dateConvertion.ConvertStringDate(lastDate)
                });

                localQuery.InsertLogs(Log);
            }
        }

        private async Task SaveTotalAdsChain(List<TotalADS> totalAds, string lastDate)
        {
            var Log = new List<Logging>();
            var startLogs = DateTime.Now;

            try
            {
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

                            foreach (var rawData in totalAds)
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
                        Message = "Total Sku Inserted : " + totalAds.Count() + "",
                        Record_Date = dateConvertion.ConvertStringDate(lastDate)
                    });

                    localQuery.InsertLogs(Log);
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
                    Message = "SaveTotalAdsChain  : " + e.Message + "",
                    Record_Date = dateConvertion.ConvertStringDate(lastDate)
                });

                localQuery.InsertLogs(Log);
            }
        }

        private async Task<int> GetCountAdsChainByDate(string dateListString)
        {
            int totalCount = 0;

            string query = "select COUNT(*) as Count from tbl_totalAds where StartDate in (" + dateListString + ") ";
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            totalCount = Convert.ToInt32(reader["Count"].ToString());
                        }
                    }
                }
            }

            return totalCount;
        }

        private async Task<int> GetCountAdsByDate(string dateListString)
        {
            int totalCount = 0;

            string query = "select COUNT(*) as Count from tbl_totaladsperclubs where StartDate in (" + dateListString + ") ";
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

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
            }

            return totalCount;
        }

        public async Task GetAverageSalesChainByDate(string dateListString, int pageSize, int offset)
        {
            string strConn = "data source='199.84.0.201';Initial Catalog=ADS.UAT;User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            var con = new SqlConnection(strConn);

            var Log = new List<Logging>();
            DateTime startLogs = DateTime.Now;

            await Task.Run(() =>
            {
                try
                {
                    using (var command = new SqlCommand("_sp_GetTblDataSample3", con))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Offset", offset);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@dateListString", dateListString);
                        command.CommandTimeout = 18000;
                        var test1 = new List<TotalADS>();
                        con.Open();

                        // Open the connection and execute the command
                        SqlDataReader reader = command.ExecuteReader();

                        // Process the result set
                        while (reader.Read())
                        {
                            var startDate = reader["StartDate"].ToString();

                            var ads = new TotalADS
                            {
                                Sku = reader["Sku"].ToString(),
                                Sales = Convert.ToDecimal(reader["Sales"].ToString()),
                                Ads = Convert.ToDecimal(reader["Ads"].ToString()),
                                Divisor = Convert.ToInt32(reader["Divisor"].ToString()),
                                StartDate = startDate,
                                EndDate = reader["EndDate"].ToString()
                            };

                            test1.Add(ads);
                        }

                        _totalAdsChain.AddRange(test1);
                        reader.Close();
                        con.Close();
                    }
                }
                catch (Exception e)
                {
                    var test = e.Message;

                    DateTime endLogs = DateTime.Now;
                    Log.Add(new Logging
                    {
                        StartLog = startLogs,
                        EndLog = endLogs,
                        Action = "Error",
                        Message = "GetAverageSalesChainByDate  : " + e.Message + "",
                        Record_Date = dateConvertion.ConvertStringDate(dateListString)
                    });

                    localQuery.InsertLogs(Log);
                }

            });
        }

        public async Task GetAverageSalesPerClubsByDate(string dateListString, int pageSize, int offset)
        {
            string strConn = "data source='199.84.0.201';Initial Catalog=ADS.UAT;User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            var con = new SqlConnection(strConn);

            var Log = new List<Logging>();
            DateTime startLogs = DateTime.Now;

            try
            {
                await Task.Run(() =>
                {
                    using (var command = new SqlCommand("_sp_GetTblDataSample4", con))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Offset", offset);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@dateListString", dateListString);
                        command.CommandTimeout = 18000;
                        var totalAdsPerClubs = new List<TotalADS>();
                        con.Open();

                        // Open the connection and execute the command
                        SqlDataReader reader = command.ExecuteReader();

                        // Process the result set
                        while (reader.Read())
                        {
                            var startDate = reader.GetString("StartDate");

                            var ads = new TotalADS
                            {
                                Sku = reader["Sku"].ToString(),
                                Sales = Convert.ToDecimal(reader["Sales"].ToString()),
                                Clubs = reader["Clubs"].ToString(),
                                Ads = Convert.ToDecimal(reader["Ads"].ToString()),
                                Divisor = Convert.ToInt32(reader["Divisor"].ToString()),
                                StartDate = startDate,
                                EndDate = reader.GetString("EndDate")
                            };

                            totalAdsPerClubs.Add(ads);
                        }

                        _totalAdsClubs.AddRange(totalAdsPerClubs);
                        reader.Close();
                        con.Close();
                    }
                });
            }
            catch (Exception e)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "GetAverageSalesPerClubsByDate  : " + e.Message + "",
                    Record_Date = dateConvertion.ConvertStringDate(dateListString)
                });

                localQuery.InsertLogs(Log);
            }
        }
    }
}