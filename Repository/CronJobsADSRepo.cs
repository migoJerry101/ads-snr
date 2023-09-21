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
//Final Code
namespace ads.Repository
{
    public class CronJobsADSRepo : IJob
    {
        private readonly IInvetory _inventory;
        private readonly ISales _sales;
        private readonly IAds _ads;

        public CronJobsADSRepo(IInvetory invetory, ISales sales, IAds ads)
        {
            _inventory = invetory;
            _sales = sales;
            _ads = ads;
        }

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
                //string startDate = previousDate.ToString("yyMMdd");
                //string endDate = previousDate.ToString("yyMMdd");

                string startDate = "230919";
                string endDate = "230919";

                await _inventory.GetInventoryAsync(startDate, startDate);
                await _sales.GetSalesAsync(startDate, startDate);
                await _ads.GetComputation();

                //List<string> dates = new List<string>();
                //dates.Add("230708");
                //dates.Add("230709");
                //dates.Add("230710");
                //dates.Add("230711");
                //dates.Add("230712");
                //dates.Add("230713");
                //dates.Add("230714");

                //foreach (var item in dates)
                //{
                //    await GetInventoryAsync(item, item);
                //    await GetSalesAsync(item, item);
                //}

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            //return await Task.CompletedTask;
        }
    }
}
