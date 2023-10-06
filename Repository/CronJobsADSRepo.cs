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
        private readonly IInvetory _inventory;
        private readonly ISales _sales;
        private readonly IAds _ads;
        private readonly IOpenQuery _openQuery;

        private readonly LogsRepo localQuery = new LogsRepo();

        public CronJobsADSRepo(IInvetory invetory, ISales sales, IAds ads, IOpenQuery openQuery)
        {
            _inventory = invetory;
            _sales = sales;
            _ads = ads;
            _openQuery = openQuery;
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

                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync();
                    await _openQuery.ImportItems(db);
                    await _openQuery.ImportClubs(db);

                    var inventory = await _openQuery.ListIventory(db);
                    var skus = await _openQuery.ListOfAllSKu(db);
                    var sales = await _openQuery.ListOfSales(db, startDate, endDate);

                    await _inventory.GetInventoryAsync(startDate, startDate, skus, sales, inventory);
                    await _sales.GetSalesAsync(startDate, startDate, skus, sales, inventory);

                }

                await _ads.ComputeAds();

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
