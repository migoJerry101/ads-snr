using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto.Price;
using ads.Models.Dto.Sale;
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
using System.Text.RegularExpressions;

namespace ads.Repository
{
    public class AdsRepo : IAds
    {
        /*        private readonly OpenQueryRepo openQuery = new OpenQueryRepo();*/
        private readonly LogsRepo localQuery = new LogsRepo();
        public List<TotalAdsChain> _totalAdsChain = new List<TotalAdsChain>();
        public List<TotalAdsClub> _totalAdsClubs = new List<TotalAdsClub>();

        private readonly ISales _sales;
        private readonly IInventory _invetory;
        private readonly IOpenQuery _openQuery;
        private readonly IClub _club;
        private readonly ITotalAdsClub _totalAdsClubRepo;
        private readonly ITotalAdsChain _totalAdsChainRepo;
        private readonly IItem _item;
        private readonly IPrice _price;

        public AdsRepo(
            ISales sales,
            IInventory invetory,
            IOpenQuery openQuery,
            IClub club,
            ITotalAdsClub totalAdsClubRepo,
            ITotalAdsChain totalAdsChainRepo,
            IItem item,
            IPrice price)
        {
            _sales = sales;
            _invetory = invetory;
            _openQuery = openQuery;
            _club = club;
            _totalAdsClubRepo = totalAdsClubRepo;
            _totalAdsChainRepo = totalAdsChainRepo;
            _item = item;
            _price = price;
        }


        public async Task<List<TotalAdsChain>> GetComputation(string stringDate)
        {
            List<TotalAdsChain> returnlist = new List<TotalAdsChain>();

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
            string dateListString = string.Join(",", DateComputeUtility.DateCompute(startDate).Select(date => $"'{date}'"));
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

        public async Task<List<TotalAdsChain>> GetTotalApdAsync(List<Inv> listInventoryResult, List<Sale> listSalesResult, string dateListString)
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

            List<TotalAdsChain> totalAPDs = new List<TotalAdsChain>();

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
                                      Inventory = y.Inventory,
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

                            totalAPDs.Add(new TotalAdsChain
                            {
                                Divisor = totalDiv,
                                Sales = salesOut,
                                Ads = totalAPD,
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

                            totalAPDs.Add(new TotalAdsChain
                            {
                                Divisor = totalDiv,
                                Sales = 0,
                                Ads = 0,
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
                        Record_Date = DateConvertion.ConvertStringDate(lastDate)
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
                    Record_Date = DateConvertion.ConvertStringDate(lastDate)
                });

                localQuery.InsertLogs(Log);

                return totalAPDs;
            }

        }

        public async Task<List<TotalAdsClub>> GetTotalSkuAndClubsAsync(List<Inv> listInventoryResult, List<Sale> listSalesResult, string dateListString)
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

            List<TotalAdsClub> totalAPDs = new List<TotalAdsClub>();

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
                         Inventory = y.Inventory,
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

                            totalAPDs.Add(new TotalAdsClub
                            {
                                Divisor = totalDiv,
                                Sales = salesOut,
                                Ads = totalAPD,
                                Sku = f.Sku,
                                Clubs = f.Clubs,
                                StartDate = lastDate,
                                EndDate = firstDate
                            });
                        }
                        else
                        {
                            totalAPDs.Add(new TotalAdsClub
                            {
                                Divisor = totalDiv,
                                Sales = 0,
                                Ads = 0,
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
                        Record_Date = DateConvertion.ConvertStringDate(lastDate)
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
                    Record_Date = DateConvertion.ConvertStringDate(lastDate)
                });

                localQuery.InsertLogs(Log);

                return totalAPDs;
            }
        }

        public async Task ComputeAds(DateTime date)
        {
            _totalAdsChain = new List<TotalAdsChain>();
            var Log = new List<Logging>();
            var listData = new List<Sale>();
            var tasks = new List<Task>();
            var tasksPerClubs = new List<Task>();
            var skus = await _item.GetAllSkuWithDate();
            var itemsToday = skus.Where(x => x.CreatedDate <= date.AddDays(-1).Date);
            var itemDictionary = itemsToday.ToDictionary(x => x.Sku, y => y);

            //get ads first
            var currentDate = date.AddDays(-1);
            var AdsDate = date.AddDays(-2);
            var CurrentDateWithZeroTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 0, 0, 0, 0);
            var adsStartDate = new DateTime(AdsDate.Year, AdsDate.Month, AdsDate.Day, 0, 0, 0, 0);
            string format = "yyyy-MM-dd HH:mm:ss.fff";

            var price = await _price.GetPricesByDateAsync(CurrentDateWithZeroTime);
            var priceDictionary = price.ToDictionary(x => new PriceKey() { Club = x.Club, Sku = x.Sku }, x => x.Value);

            var adsChain = await _totalAdsChainRepo.GetTotalAdsChainByDate($"{adsStartDate:yyyy-MM-dd HH:mm:ss.fff}");
            var adsDayZeorChain = adsChain.Count > 0 ? adsChain[0].EndDate : $"{CurrentDateWithZeroTime:yyyy-MM-dd HH:mm:ss.fff}";
            var numberOfDayZeroChain = DateComputeUtility.GetDifferenceInRange($"{adsStartDate:yyyy-MM-dd HH:mm:ss.fff}", adsDayZeorChain) - 56;
            var numberOfDayZeroChainFinal = numberOfDayZeroChain >= 0 ? numberOfDayZeroChain : 0;

            //get list of day zeros for Chain
            DateTime.TryParseExact(adsDayZeorChain, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDateOutTotal);
            var listOfDayZeroChain = DateComputeUtility.GetDatesAndDaysAfter(endDateOutTotal, numberOfDayZeroChainFinal);

            //get ads clubs to get the longest range of date
            var adsPerClubs = await _totalAdsClubRepo.GetTotalAdsClubsByDate($"{adsStartDate:yyyy-MM-dd HH:mm:ss.fff}");
            var adsDayZeorClubs = adsPerClubs.Count > 0 ? adsPerClubs[0].EndDate : $"{CurrentDateWithZeroTime:yyyy-MM-dd HH:mm:ss.fff}";
            var numberOfDayZeroClubs = DateComputeUtility.GetDifferenceInRange($"{adsStartDate:yyyy-MM-dd HH:mm:ss.fff}", adsDayZeorClubs)-56;
            var numberOfDayZeroFinal = numberOfDayZeroClubs >= 0 ? numberOfDayZeroClubs : 0;

            DateTime.TryParseExact(adsDayZeorClubs, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDateOut);
            var listOfDayZero = DateComputeUtility.GetDatesAndDaysAfter(endDateOut, numberOfDayZeroFinal);

            listOfDayZero.AddRange(listOfDayZeroChain);

            var listOfDayZeroBoth = listOfDayZero.Distinct().ToList();

            var adsChainWithCompleteDivisor = adsChain.Where(x =>
            {
                DateTime.TryParseExact(x.EndDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime adsItemEndDate);
                var isMember = x.Divisor == 56 && listOfDayZero.Contains(adsItemEndDate);

                return isMember;
            });

            var groupedAdsChain = adsChainWithCompleteDivisor
                .GroupBy(x => x.EndDate)
                .ToDictionary(
                    group => group.Key,
                    group =>  group.Select(item => item.Sku).ToList()
             );

            var adsPerClubsWithCompleteDivisor = adsPerClubs.Where(x =>
            {
                DateTime.TryParseExact(x.EndDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime adsItemEndDate);
                var isMember = x.Divisor == 56 && listOfDayZero.Contains(adsItemEndDate);

                return isMember;
            });

            var dictionaryListOfSkuClubs = adsPerClubsWithCompleteDivisor
                .GroupBy(x => x.EndDate)
                .ToDictionary(
                    group => group.Key,
                    group =>  group.Select(item => item.Sku).ToList()
                );

            var mergedDictionarySku= dictionaryListOfSkuClubs
                .Concat(groupedAdsChain)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(
                    grp => grp.Key,
                    grp => grp.SelectMany(kvp => kvp.Value).Distinct().ToList()
                );
            //sales for chain
            var salesToday = await _sales.GetSalesByDateEf(CurrentDateWithZeroTime); //to add

            //sales and inventory for day zero chain
            var salesZeroChain = await _sales.GetSalesWithFilteredSku(mergedDictionarySku, listOfDayZeroBoth);
            var inventoryZeorChain = await _invetory.GetInventoriesWithFilteredSku(mergedDictionarySku, listOfDayZeroBoth);

            //sales for clubs
            var salesTodayWithoutNullClubs = salesToday.Where(i => !i.Clubs.IsNullOrEmpty());
            var salesDayZeroWithoutNullClubs = salesZeroChain.Where(i => !i.Clubs.IsNullOrEmpty());

            //getInventory today and dayzero
            var inventoryToday = await _invetory.GetInventoriesByDateEf(CurrentDateWithZeroTime);

            var inventoryDayZeroWithoutNullClubs = inventoryZeorChain.Where(i => !i.Clubs.IsNullOrEmpty());
            var inventoryTodayWithoutNullClubs = inventoryToday.Where(c => !c.Clubs.IsNullOrEmpty());

            var skuDictionary = skus.Distinct().ToDictionary(x => x);
            var chainDictionary = adsChain.Distinct().ToDictionary(c => c.Sku, y => y);

            var salesTotalDictionaryToday = _sales.GetDictionayOfTotalSales(salesToday);
            var salesTotalDictionaryDayZero = _sales.GetDictionayOfTotalSalesWithSalesKey(salesZeroChain);

            var inventoryTotalDictionaryToday = _invetory.GetDictionayOfTotalInventory(inventoryToday);
            var inventoryTodayDictionaryDayZero = _invetory.GetDictionayOfTotalInventory(inventoryZeorChain);

            var adsWithCurrentSales = new List<TotalAdsChain>();

            var startDateInString = $"{CurrentDateWithZeroTime:yyyy-MM-dd HH:mm:ss.fff}";
            //var endDateInString = $"{endDateOut:yyyy-MM-dd HH:mm:ss.fff}";

            foreach (var item in itemsToday)
            {
                if (chainDictionary.TryGetValue(item.Sku, out var ads))
                {
                    DateTime.TryParseExact(ads.EndDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime adsItemEndDate);
                    var dateAsKey = DateConvertion.ConvertStringDate(adsItemEndDate.ToString("yyMMdd"));

                    var dayZeroKey = new SalesKey()
                    {
                        Sku = item.Sku,
                        Date = dateAsKey
                    };

                    var todayKey = new SalesKey()
                    {
                        Sku = item.Sku,
                        Date = CurrentDateWithZeroTime
                    };

                    var hasSales = salesTotalDictionaryDayZero.TryGetValue(dayZeroKey, out var totalSalesOut);
                    var hasInventory = inventoryTodayDictionaryDayZero.TryGetValue(dayZeroKey, out var totalInvOut);

                    var hasInventoryToday = inventoryTotalDictionaryToday.TryGetValue(todayKey, out var totalInvOutToday);

                    var daysDifferenceOut = DateComputeUtility.GetDifferenceInRange(ads.StartDate, ads.EndDate);

                    if (daysDifferenceOut >= 56 & totalInvOutToday > 0 & ads.Divisor == 56)
                    {
                        var newEndDate = adsItemEndDate.AddDays(1);
                        var endDateInStringNew = $"{newEndDate:yyyy-MM-dd HH:mm:ss.fff}";

                        if (totalInvOut > 0 & totalSalesOut >= 0)
                        {
                            ads.Sales -= totalSalesOut;
                            ads.Divisor--;
                        }

                        ads.EndDate = endDateInStringNew;
                    }

                    adsWithCurrentSales.Add(ads);
                }
            }

            var adsWithCurrentsalesDictionary = adsWithCurrentSales.ToDictionary(x => x.Sku, y => y);
            adsWithCurrentSales = new List<TotalAdsChain>();

            foreach (var item in itemsToday)
            {
                var todayKey = new SalesKey()
                {
                    Sku = item.Sku,
                    Date = CurrentDateWithZeroTime
                };

                var hasSales = salesTotalDictionaryToday.TryGetValue(item.Sku, out var totalSalesOut);
                var hasInventory = inventoryTotalDictionaryToday.TryGetValue(todayKey, out var totalInvOut);
                var isItemToday = itemDictionary.TryGetValue(item.Sku, out var itemOut);


                if (adsWithCurrentsalesDictionary.TryGetValue(item.Sku, out var ads))
                {
                    var daysDifferenceOut = DateComputeUtility.GetDifferenceInRange(ads.StartDate, ads.EndDate);

                    if (totalInvOut > 0 & totalSalesOut >= 0)
                    {
                        if (ads.Divisor != 56) ads.Divisor++;

                        ads.Sales += totalSalesOut;
                        ads.Ads = ads.Divisor != 0 ? Math.Round(ads.Sales / ads.Divisor, 2) : 0;
                    }

                    ads.StartDate = startDateInString;
                    adsWithCurrentSales.Add(ads);
                }
                else
                {
                    if (totalInvOut > 0 & totalSalesOut > 0 & isItemToday)
                    {
                        var newAds = new TotalAdsChain()
                        {
                            Divisor = 0,
                            Sales = totalSalesOut,
                            Ads = Math.Round(totalSalesOut / 1, 2),
                            Sku = item.Sku,
                            StartDate = startDateInString,
                            EndDate = startDateInString
                        };

                        if (totalInvOut > 0 & totalSalesOut >= 0)
                        {
                            newAds.Divisor = 1;
                        }

                        adsWithCurrentSales.Add(newAds);
                    }
                }
            }

            var recordDate = currentDate.Date;
            await SaveTotalAdsChain(adsWithCurrentSales, recordDate);

            var totalAdsClubDictionary = adsPerClubs.ToDictionary(x => new { x.Sku, x.Clubs });

            //prices per clubs

            var salesTodayWithoutNullClubsDictionary = salesTodayWithoutNullClubs
                .GroupBy(x => new { x.Sku, x.Clubs })
                .ToDictionary(group => group.Key, group => group.Sum(y => y.Sales));

            var salesDayZeroWithoutNullClubsDictionary = salesDayZeroWithoutNullClubs
                .GroupBy(x => 
                    new SalesClubKey() 
                    { 
                        Sku = x.Sku,
                        Club = x.Clubs,
                        Date = x.Date 
                    })
                .ToDictionary(group => 
                    group.Key, group =>
                    group.Sum(y => y.Sales));

            var inventoryDayZeroWithoutNullClubsDictionary = inventoryDayZeroWithoutNullClubs
                .GroupBy(x => 
                    new SalesClubKey()
                    { 
                        Sku = x.Sku,
                        Club = x.Clubs,
                        Date = x.Date
                    })
                .ToDictionary(group =>
                    group.Key,
                    group => group.Sum(i => i.Inventory));

            var adsPerClubsWithCurrentsales = new List<TotalAdsClub>();
            var clubsDictionary = await _club.GetClubsDictionary();

            foreach (var inv in inventoryTodayWithoutNullClubs)
            {
                var hasAds = totalAdsClubDictionary.TryGetValue(new { inv.Sku, inv.Clubs }, out var adsOut);
                var isAclub = clubsDictionary.TryGetValue(Convert.ToInt32(inv.Clubs), out var StartDate);

                var priceKey = new PriceKey()
                {
                    Sku = hasAds ? adsOut.Sku : inv.Sku,
                    Club = hasAds ? adsOut.Clubs : inv.Clubs
                };

                var hasPrice = priceDictionary.TryGetValue(priceKey, out var priceOut);

                salesTodayWithoutNullClubsDictionary.TryGetValue(new { inv.Sku, inv.Clubs }, out var perClubSalesToday);

                if (hasAds & isAclub)
                {
                    DateTime.TryParseExact(adsOut.EndDate, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime adsClubEndDate);
                    var dateAsKey = DateConvertion.ConvertStringDate(adsClubEndDate.ToString("yyMMdd"));

                    var salesClubKey = new SalesClubKey()
                    {
                        Sku = inv.Sku,
                        Date = dateAsKey,
                        Club = inv.Clubs
                    };

                    var daysDifferenceOut = DateComputeUtility.GetDifferenceInRange(adsOut.StartDate, adsOut.EndDate);

                    if (daysDifferenceOut >= 56 & inv.Inventory > 0 & adsOut.Divisor == 56)
                    {
                        salesDayZeroWithoutNullClubsDictionary.TryGetValue(salesClubKey, out var perClubSalesDayZero);
                        inventoryDayZeroWithoutNullClubsDictionary.TryGetValue(salesClubKey, out var perClubInvDayZero);

                        var newEndDate = adsClubEndDate.AddDays(1);
                        var endDateInStringNew = $"{newEndDate:yyyy-MM-dd HH:mm:ss.fff}";

                        if (perClubInvDayZero > 0 & perClubSalesDayZero >= 0)
                        {
                            adsOut.Sales -= perClubSalesDayZero;
                            adsOut.Divisor--;
                        }

                        adsOut.EndDate = endDateInStringNew;
                    }

                    if (inv.Inventory > 0 & perClubSalesToday >= 0)
                    {
                        if (adsOut.Divisor < 56) adsOut.Divisor++;

                        adsOut.Sales += perClubSalesToday;
                        adsOut.Ads = adsOut.Divisor != 0 ? Math.Round(adsOut.Sales / adsOut.Divisor, 2) : 0;
                    }

                    adsOut.OverallSales = hasPrice ? priceOut * perClubSalesToday : 0;
                    adsOut.StartDate = startDateInString;
                    adsPerClubsWithCurrentsales.Add(adsOut);
                }
                else
                {
                    var isItemToday = itemDictionary.TryGetValue(inv.Sku, out var itemOut);
                    var currentDateCheck = DateTime.Now;

                    if (inv.Inventory > 0 & perClubSalesToday > 0 & isItemToday)
                    {
                        var newAds = new TotalAdsClub()
                        {
                            Divisor = 0,
                            Sales = perClubSalesToday,
                            Ads = perClubSalesToday != 0 ? Math.Round(perClubSalesToday / 1, 2) : 0,
                            Sku = inv.Sku,
                            Clubs = inv.Clubs,
                            StartDate = startDateInString,
                            EndDate = startDateInString,
                            OverallSales = hasPrice ? priceOut * perClubSalesToday : 0
                        };

                        if (inv.Inventory > 0 & perClubSalesToday >= 0)
                        {
                            newAds.Divisor = 1;
                        }

                        if (currentDateCheck > StartDate & isItemToday)
                        {
                            adsPerClubsWithCurrentsales.Add(newAds);
                        }
                    }
                }
            }

            await SaveAdsPerClubs(adsPerClubsWithCurrentsales, recordDate);
        }

        private async Task SaveAdsPerClubs(List<TotalAdsClub> adsPerClubs, DateTime lastDate)
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
                            dataTable.Columns.Add("Divisor", typeof(int));
                            dataTable.Columns.Add("Ads", typeof(decimal));
                            dataTable.Columns.Add("StartDate", typeof(string));
                            dataTable.Columns.Add("EndDate", typeof(string));
                            dataTable.Columns.Add("OverallSales", typeof(decimal));

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
                                row["OverallSales"] = rawData.OverallSales != null ? rawData.OverallSales : 0;
                                dataTable.Rows.Add(row);
                            }
                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        await transaction.CommitAsync();
                    }

                    DateTime endLogs = DateTime.Now;
                    Log.Add(new Logging
                    {
                        StartLog = startLogs,
                        EndLog = endLogs,
                        Action = "Total ADS",
                        Message = "Total Clubs Inserted : " + adsPerClubs.Count() + "",
                        Record_Date = lastDate
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
                    Record_Date = lastDate
                });

                localQuery.InsertLogs(Log);
            }
        }

        private async Task SaveTotalAdsChain(List<TotalAdsChain> totalAds, DateTime lastDate)
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
                            dataTable.Columns.Add("Divisor", typeof(int));
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

                        await transaction.CommitAsync();
                    }

                    DateTime endLogs = DateTime.Now;
                    Log.Add(new Logging
                    {
                        StartLog = startLogs,
                        EndLog = endLogs,
                        Action = "Total ADS",
                        Message = "Total Sku Inserted : " + totalAds.Count() + "",
                        Record_Date = lastDate
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
                    Record_Date = lastDate
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
                        var test1 = new List<TotalAdsChain>();
                        con.Open();

                        // Open the connection and execute the command
                        SqlDataReader reader = command.ExecuteReader();

                        // Process the result set
                        while (reader.Read())
                        {
                            var startDate = reader["StartDate"].ToString();

                            var ads = new TotalAdsChain
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
                        Record_Date = DateConvertion.ConvertStringDate(dateListString)
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

            await Task.Run(() =>
            {
                try
                {
                    using (var command = new SqlCommand("_sp_GetTblDataSample4", con))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Offset", offset);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@dateListString", dateListString);
                        command.CommandTimeout = 18000;
                        var totalAdsPerClubs = new List<TotalAdsClub>();
                        con.Open();

                        // Open the connection and execute the command
                        SqlDataReader reader = command.ExecuteReader();

                        // Process the result set
                        while (reader.Read())
                        {
                            var startDate = reader.GetString("StartDate");

                            var ads = new TotalAdsClub
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

                    };
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
                        Record_Date = DateConvertion.ConvertStringDate(dateListString)
                    });

                    localQuery.InsertLogs(Log);
                }

            });
        }
    }
}
