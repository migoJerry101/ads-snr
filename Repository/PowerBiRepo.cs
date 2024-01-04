using ads.Interface;
using ads.Models.Data;
using ads.Utility;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace ads.Repository
{
    public class PowerBiRepo : IPowerBiAds
    {
        private readonly ILogs _logger;
        private readonly ISales _sales;
        private readonly IInventory _inventory;
        private readonly IClub _club;
        private readonly IItem _item;
        private readonly IPowerBiAdsChain _powerBiAdsChain;
        private readonly IPowerBiAdsClub _powerBiAdsClub;

        public PowerBiRepo(
            ILogs logger,
            ISales sales,
            IInventory inventory,
            IClub club,
            IItem item,
            IPowerBiAdsChain powerBiAdsChain,
            IPowerBiAdsClub powerBiAdsClub)
        {
            _logger = logger;
            _sales = sales;
            _inventory = inventory;
            _club = club;
            _item = item;
            _powerBiAdsChain = powerBiAdsChain;
            _powerBiAdsClub = powerBiAdsClub;
        }

        public async Task ComputePowerBiAdsAsync(DateTime date)
        {
            var log = new List<Logging>();
            var startLogs = DateTime.Now;

            try
            {
                var skus = await _item.GetAllSkuWithDate();
                var skuDictionary = skus.Distinct().ToDictionary(x => x);
                var itemsToday = skus.Where(x => x.CreatedDate <= date);

                var currentDate = date.AddDays(-1);
                var AdsDate = date.AddDays(-2);
                var CurrentDateWithZeroTime = currentDate.Date;
                var adsStartDate = AdsDate.Date;

                //get Chain Ads for Pbi
                var adsChain = await _powerBiAdsChain.GetPowerBiAdsChainsByDateAsync(currentDate);

                var adsDayZeor = adsChain.Count > 0 ? adsChain[0].EndDate : CurrentDateWithZeroTime;

                //sales for chain
                var SalesToday = await _sales.GetSalesByDateEf(CurrentDateWithZeroTime); //to add
                var salesDayZero = await _sales.GetSalesByDateEf(adsDayZeor); // to subtract

                //sales for clubs
                var salesTodayWithoutNullClubs = SalesToday.Where(i => !i.Clubs.IsNullOrEmpty());
                var salesDayZeroWithoutNullClubs = salesDayZero.Where(i => !i.Clubs.IsNullOrEmpty());

                //getInventory today and dayzero
                var inventoryToday = await _inventory.GetInventoriesByDateEf(CurrentDateWithZeroTime);
                var inventoryDayZero = await _inventory.GetInventoriesByDateEf(adsDayZeor);

                var inventoryDayZeroWithoutNullClubs = inventoryDayZero.Where(i => !i.Clubs.IsNullOrEmpty());
                var inventoryTodayWithoutNullClubs = inventoryToday.Where(c => !c.Clubs.IsNullOrEmpty());

                var chainDictionary = adsChain.Distinct().ToDictionary(c => c.Sku, y => y);

                var salesTotalDictionaryToday = _sales.GetDictionayOfTotalSales(SalesToday);
                var salesTotalDictionaryDayZero = _sales.GetDictionayOfTotalSales(salesDayZero);

                var inventoryTotalDictionaryToday = _inventory.GetDictionaryOfTotalInventory(inventoryToday);
                var inventoryTodayDictionaryDayZero = _inventory.GetDictionaryOfTotalInventory(inventoryDayZero);

                var adsWithCurrentSales = new List<PowerBiAdsChain>();

                foreach (var item in itemsToday)
                {
                    if (chainDictionary.TryGetValue(item.Sku, out var ads))
                    {
                        var hasSales = salesTotalDictionaryDayZero.TryGetValue(item.Sku, out var totalSalesOut);
                        var hasInventory = inventoryTodayDictionaryDayZero.TryGetValue(item.Sku, out var totalInvOut);

                        var daysDifferenceOut = DateComputeUtility.GetDifferenceInRange(ads.StartDate, ads.EndDate);

                        if (ads.Divisor == 56)
                        {
                            var newEndDate = adsDayZeor.AddDays(1);

                            if (totalSalesOut > 0)
                            {
                                ads.Sales -= totalSalesOut;
                                ads.Divisor--;
                            }

                            ads.EndDate = newEndDate;
                        }

                        adsWithCurrentSales.Add(ads);
                    }
                }

                var adsWithCurrentSalesDictionary = adsWithCurrentSales.ToDictionary(x => x.Sku, y => y);

                foreach (var item in itemsToday)
                {
                    var hasSales = salesTotalDictionaryToday.TryGetValue(item.Sku, out var totalSalesOut);
                    var hasInventory = inventoryTotalDictionaryToday.TryGetValue(item.Sku, out var totalInvOut);

                    if (adsWithCurrentSalesDictionary.TryGetValue(item.Sku, out var ads))
                    {
                        if (totalSalesOut > 0)
                        {
                            if (totalSalesOut > 0)
                            {
                                if (ads.Divisor != 56) ads.Divisor++;

                                ads.Sales += totalSalesOut;
                                ads.Ads = ads.Divisor != 0 ? Math.Round(ads.Sales / ads.Divisor, 2) : 0;
                            }
                        }

                        ads.StartDate = currentDate;

                        adsWithCurrentSales.Add(ads);
                    }
                    else
                    {
                        var newAds = new PowerBiAdsChain()
                        {
                            Divisor = 0,
                            Ads = totalSalesOut != 0 ? Math.Round(totalSalesOut / 1, 2) : 0,
                            Sku = item.Sku,
                            StartDate = currentDate,
                            EndDate = currentDate
                        };

                        if (totalSalesOut > 0)
                        {
                            newAds.Divisor += 1;
                            newAds.Sales = totalSalesOut;
                            newAds.Ads += totalSalesOut != 0 ? Math.Round(totalSalesOut / 1, 2) : 0;
                        }
                        else
                        {
                            newAds.Sales = 0;
                            newAds.Ads = 0;
                        }

                        adsWithCurrentSales.Add(newAds);
                    }
                }

                //save new Ads
                var adsPerClubs = await _powerBiAdsClub.GetPowerBiAdsClubByDateAsync(currentDate);
                var totalAdsClubDictionary = adsPerClubs.ToDictionary(x => new { x.Sku, x.Clubs });

                var salesDayZeroWithoutNullClubsDictionary = salesDayZeroWithoutNullClubs
                    .GroupBy(x => new { x.Sku, x.Clubs })
                    .ToDictionary(group => group.Key, group => group.Sum(y => y.Sales));

                var inventoryDayZeroWithoutNullClubsDictionary = inventoryDayZeroWithoutNullClubs.ToDictionary(x => new { x.Sku, x.Clubs }, y => y.Inventory);

                foreach (var sale in salesTodayWithoutNullClubs)
                {
                    var hasAds = totalAdsClubDictionary.TryGetValue(new { sale.Sku, sale.Clubs }, out var adsOut);
                    inventoryDayZeroWithoutNullClubsDictionary.TryGetValue(new { sale.Sku, sale.Clubs }, out var perClubInvDayZero);
                    salesDayZeroWithoutNullClubsDictionary.TryGetValue(new { sale.Sku, sale.Clubs }, out var perClubSalesDatZero);

                    if (hasAds)
                    {
                        var daysDifferenceOut = DateComputeUtility.GetDifferenceInRange(adsOut.StartDate, adsOut.EndDate);
                        adsOut.StartDate = currentDate;
                        if (sale.Sales == 0 && perClubInvDayZero == 0) adsOut.OutOfStockDaysCount += 1;

                        if (adsOut.Divisor == 56)//checked
                        {
                            //subtraction of day zero sale from total
                            var newEndDate = adsDayZeor.AddDays(1);
                            adsOut.EndDate = newEndDate;
                            adsOut.Sales -= perClubSalesDatZero;


                        }
                        else
                        {
                            //add sales today and recalculate ads
                            //check if out of stock daycount

                            if (sale.Sales > 0)
                            {
                                adsOut.Divisor++;
                                adsOut.OutOfStockDaysCount = 0;
                                adsOut.Sales += sale.Sales;
                                adsOut.Ads = adsOut.Sales != 0 ? Math.Round(adsOut.Sales / 1, 2) : adsOut.Ads;
                            }



                        }
                    }
                }

            }
            catch (Exception error)
            {

                var endLogs = DateTime.Now;
                log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Tags for Ads Chain",
                    Message = $"Error: {error.Message}",
                    Record_Date = date.Date
                });

                _logger.InsertLogs(log);

                throw;
            }
        }
    }
}
