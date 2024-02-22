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
using System;
using Azure.Core.GeoJson;
using ads.Interface;
using ads.Utility;
//Final Code
namespace ads.Repository
{
    public class CronJobsADSRepo : IJob
    {
        private readonly IInventory _inventory;
        private readonly ISales _sales;
        private readonly IAds _ads;
        private readonly IOpenQuery _openQuery;
        private readonly IItem _item;
        private readonly IPrice _price;
        private readonly LogsRepo localQuery = new LogsRepo();
        private readonly ITotalAdsClub _totalAdsClub;

        public CronJobsADSRepo(IInventory invetory, ISales sales, IAds ads, IOpenQuery openQuery, IItem item, IPrice price, ITotalAdsClub totalAdsClub)
        {
            _inventory = invetory;
            _sales = sales;
            _ads = ads;
            _openQuery = openQuery;
            _item = item;
            _price = price;
            _totalAdsClub = totalAdsClub;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            string id = Guid.NewGuid().ToString();
            string message = "This job will be executed again at: " +
            context.NextFireTimeUtc.ToString();

            //Start Logs
            List<Logging> Log = new List<Logging>();

            //Star logs Date Time
            DateTime startLogs = DateTime.Now;

            // Get the current date
            DateTime currentDate = DateTime.Now;

            // Subtract one day
            DateTime previousDate = currentDate.AddDays(-1);

            ////////Actual Record or Final Setup
            string startDate = previousDate.ToString("yyMMdd");
            string endDate = previousDate.ToString("yyMMdd");

            try
            {
                var dateFormat = DateConvertion.ConvertStringDate(startDate);

                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();
                    await _openQuery.ImportItems(db);
                    await _openQuery.ImportClubs(db);

                    var itemList = await _item.GetAllSkuWithDate();
                    var items = itemList.Where(x => x.CreatedDate <= dateFormat);

                    var inventory = await _openQuery.ListIventory(db);
                    var sales = await _openQuery.ListOfSales(db, startDate, endDate);

                    await _inventory.GetInventoryAsync(startDate, startDate, items, sales, inventory);
                    await _sales.GetSalesAsync(startDate, startDate, items, sales, inventory);
                }

                await _ads.ComputeAds(currentDate.Date);

                var priceDateInString = currentDate.AddDays(-3);
                await _price.FetchSalesFromMmsByDateAsync();
                await _totalAdsClub.UpdateClubTotalAverageSales(previousDate.Date);
                //await _price.DeletePriceByDate(priceDateInString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

                Console.WriteLine(e.Message);
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "Execute CronJobs : " + e.Message + "",
                    Record_Date = DateConvertion.ConvertStringDate(startDate) 
                });

                localQuery.InsertLogs(Log);

                throw;
            }

            //return await Task.CompletedTask;
        }
    }
}
