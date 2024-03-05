using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto.Price;
using ads.Utility;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ads.Repository;

public class PriceRepo : IPrice
{
    private readonly AdsContext _adsContext;
    private readonly ILogs _logs;
    private readonly IInventory _inventory;
    private readonly IOpenQuery _openQuery;
    private readonly IConfiguration _configuration;

    public PriceRepo(AdsContext adsContext, ILogs logs, IInventory inventory, IOpenQuery openQuery, IConfiguration configuration)
    {
        _adsContext = adsContext;
        _logs = logs;
        _inventory = inventory;
        _openQuery = openQuery;
        _configuration = configuration;
    }

    public async Task GetHistoricalPriceFromCsv(DateTime dateTime)
    {
        var startLogs = DateTime.Now;
        var logs = new List<Logging>();
        const string folder = "price";

        try
        {
            var month = dateTime.ToString("MM");
            var day = dateTime.ToString("dd");

            var fileName = $"SKU_Prices_{month}_{day}_{dateTime.Year}";
            var myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var completePath = $@"{myDocumentsPath}\\{folder}\\{fileName}.csv";

            //fetch all data using csvHelper
            var prices = CsvUtilityHelper.GetHistoricalListFromCsv<PriceCsvDto>(completePath).ToList();
            var importedPrices = new List<Price>();

            foreach (var priceDto in prices)
            {
                var isTotalSales = decimal.TryParse(priceDto.Total_Sales, out var totalSales);
                var isTotalQtySold = decimal.TryParse(priceDto.Total_Quantity_Sold, out var totalQtySold);
                decimal value = 0;

                if (totalSales < 0 || totalQtySold < 0)
                {
                    value = 0;
                }

                if (totalSales > 0 && totalQtySold > 0)
                {
                    value = totalSales / totalQtySold;
                }

                var price = new Price()
                {
                    Club = priceDto.Store_Number.ToString(),
                    Date = priceDto.Transaction_Date.Date,
                    Sku = priceDto.SKU_Number.ToString(),
                    Value = value
                };

                importedPrices.Add(price);
            }

            await _adsContext.Prices.AddRangeAsync(importedPrices);
            await _adsContext.SaveChangesAsync();

            logs.Add(new Logging
            {
                StartLog = startLogs,
                EndLog = DateTime.Now,
                Action = "GetHistoricalPriceFromCsv",
                Message = $"inserted price: {importedPrices.Count()}",
                Record_Date = dateTime.Date
            });

            _logs.InsertLogs(logs);
        }
        catch (Exception error)
        {
            var endLogs = DateTime.Now;

            logs.Add(new Logging
            {
                StartLog = startLogs,
                EndLog = endLogs,
                Action = "GetHistoricalPriceFromCsv",
                Message = error.Message,
                Record_Date = endLogs.Date
            });

            _logs.InsertLogs(logs);
            throw;
        }
    }

    public async Task BatchCreatePrices(IEnumerable<Price> data)
    {
        var startLogs = DateTime.Now;
        var logs = new List<Logging>();

        try
        {
            await _adsContext.Prices.AddRangeAsync(data);
            await _adsContext.SaveChangesAsync();
        }
        catch (Exception error)
        {
            var endLogs = DateTime.Now;

            logs.Add(new Logging
            {
                StartLog = startLogs,
                EndLog = endLogs,
                Action = "GetHistoricalPriceFromCsv",
                Message = error.Message,
                Record_Date = startLogs.Date
            });

            _logs.InsertLogs(logs);
            throw;
        }
    }

    public async Task FetchSalesFromMmsByDateAsync()
    {
        var startLogs = DateTime.Now;
        var logs = new List<Logging>();
        var stringDate = startLogs.ToString("yyMMdd");

        try
        {
            var pricesDto = new List<Price>();

            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                var inventories = await _inventory.GetEFInventoriesByDate(startLogs.AddDays(-1).Date);
                var clubsDictionary = inventories
                    .Where(x => x.Clubs != string.Empty && x.Sku != string.Empty)
                    .ToDictionary(x => new { x.Clubs, x.Sku });

                var clubsWithInv = inventories.Where(x => x.Inventory > 0 && x.Clubs != string.Empty).Select(y => y.Clubs).Distinct().ToList();
                var commaSeparatedClubs = string.Join(",", clubsWithInv);

                var query = $@"SELECT * FROM OPENQUERY([snr],
                    'SELECT 
                        PISKU,
                        MAX(PRET) as CURRENTPRICE,
                        PSTR 
                     FROM MMJDALIB.PRC_PF5 
                     WHERE PDAT = {stringDate} and PRET != 0000000000.001 
                     AND PSTR IN ({commaSeparatedClubs})
                     GROUP BY PISKU,PSTR')";

                using SqlCommand cmd = new SqlCommand(query, db.Con);

                cmd.CommandTimeout = 18000;

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var Sku = reader["PISKU"].ToString();
                    var Clubs = reader["PSTR"].ToString();

                    if (clubsDictionary.TryGetValue(new { Clubs, Sku }, out var inv))
                    {
                        var currentPrice = reader["CURRENTPRICE"].ToString();
                        var isDecimal = decimal.TryParse(currentPrice, out var valueOut);

                        var price = new Price()
                        {
                            Sku = reader["PISKU"].ToString(),
                            Club = reader["PSTR"].ToString(),
                            Date = startLogs.Date,
                            Value = isDecimal ? valueOut : 0,
                        };

                        pricesDto.Add(price);
                    }
                }
            }

            var prices = pricesDto
                .GroupBy(y => new { y.Club, y.Sku })
                .Select(group => group.First())
                .ToList();

            await BulkCreatePrices(prices, startLogs.Date);
        }
        catch (Exception error)
        {
            var endLogs = DateTime.Now;

            logs.Add(new Logging
            {
                StartLog = startLogs,
                EndLog = endLogs,
                Action = "FetchSalesFromMmsByDateAsync",
                Message = error.Message,
                Record_Date = startLogs.Date
            });

            _logs.InsertLogs(logs);
        }
    }

    public async Task<List<Price>> GetPricesByDateAsync(DateTime dateTime)
    {
        var startLogs = DateTime.Now;
        var logs = new List<Logging>();

        try
        {
            var prices = await _adsContext.Prices.Where(x => x.Date == dateTime).ToListAsync();
            return prices;
        }
        catch (Exception error)
        {
            var endLogs = DateTime.Now;

            logs.Add(new Logging
            {
                StartLog = startLogs,
                EndLog = endLogs,
                Action = "GetPricesByDateAsync",
                Message = error.Message,
                Record_Date = startLogs.Date
            });

            _logs.InsertLogs(logs);
            throw;
        }
    }

    public async Task DeletePriceByDate(DateTime date)
    {
        DateTime startLogs = DateTime.Now;
        List<Logging> Log = new List<Logging>();

        try
        {
            var strConn = _configuration["ConnectionStrings:DatabaseConnection"];
            var con = new SqlConnection(strConn);

            using (var command = new SqlCommand("_sp_DeletePriceByDate", con))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@date", date);
                command.CommandTimeout = 18000;
                con.Open();

                var reader = await command.ExecuteReaderAsync();

                reader.Close();
                con.Close();
            }

            Log.Add(new Logging
            {
                StartLog = startLogs,
                EndLog = DateTime.Now,
                Action = "DeletePriceByDate",
                Message = $"Deleted Price with Date: {date}",
                Record_Date = date
            });

            _logs.InsertLogs(Log);
        }
        catch (Exception error)
        {
            Log.Add(new Logging
            {
                StartLog = startLogs,
                EndLog = DateTime.Now,
                Action = "DeletePriceByDate",
                Message = error.Message,
                Record_Date = date
            });

            _logs.InsertLogs(Log);
        }
    }

    private async Task BulkCreatePrices(List<Price> prices, DateTime date)
    {
        var logs = new List<Logging>();
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
                        bulkCopy.DestinationTableName = "tbl_Prices";
                        bulkCopy.BatchSize = 1000;

                        var dataTable = new DataTable();
                        dataTable.Columns.Add("Id", typeof(int));
                        dataTable.Columns.Add("Sku", typeof(string));
                        dataTable.Columns.Add("Club", typeof(decimal));
                        dataTable.Columns.Add("Value", typeof(int));
                        dataTable.Columns.Add("Date", typeof(DateTime));

                        foreach (var rawData in prices)
                        {
                            var row = dataTable.NewRow();
                            row["Sku"] = rawData.Sku;
                            row["Club"] = rawData.Club;
                            row["Value"] = rawData.Value;
                            row["Date"] = rawData.Date;

                            dataTable.Rows.Add(row);
                        }
                        await bulkCopy.WriteToServerAsync(dataTable);
                    }

                    await transaction.CommitAsync();
                }

                logs.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = DateTime.Now,
                    Action = "BulkCreatePrices",
                    Message = $"prices inserted : {prices.Count()}",
                    Record_Date = date
                });

                _logs.InsertLogs(logs);
            }
        }
        catch (Exception e)
        {
            DateTime endLogs = DateTime.Now;
            logs.Add(new Logging
            {
                StartLog = startLogs,
                EndLog = endLogs,
                Action = "BulkCreatePrices",
                Message = e.Message,
                Record_Date = date
            });

            _logs.InsertLogs(logs);
        }
    }
}