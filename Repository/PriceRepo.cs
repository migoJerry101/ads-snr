using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto.Price;
using ads.Utility;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Vml;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;

namespace ads.Repository;

public class PriceRepo : IPrice
{
    private readonly AdsContext _adsContext;
    private readonly ILogs _logs;
    private readonly IInventory _inventory;

    public PriceRepo(AdsContext adsContext, ILogs logs, IInventory inventory)
    {
        _adsContext = adsContext;
        _logs = logs;
        _inventory = inventory;
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

    public async Task<List<Price>> FetchSalesFromMmsByDateAsync(DateTime dateTime)
    {
        var startLogs = DateTime.Now;
        var logs = new List<Logging>();

        try
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                const string query = "select * from Openquery([snr], 'SELECT INUMBR, IDESCR, IMCRDT from MMJDALIB.INVMST WHERE ISTYPE = ''01'' AND IDSCCD IN (''A'',''I'',''D'',''P'') AND IATRB1 IN (''L'',''I'',''LI'')')";

                using SqlCommand cmd = new SqlCommand(query, db.Con);

                cmd.CommandTimeout = 18000;

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {

                }
            }
            return await _adsContext.Prices.ToListAsync();
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
            throw;
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
}