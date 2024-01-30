using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto.Price;
using ads.Utility;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;

namespace ads.Repository;

public class PriceRepo : IPrice
{
    private readonly AdsContext _adsContext;
    private readonly ILogs _logs;

    public PriceRepo(AdsContext adsContext, ILogs logs)
    {
        _adsContext = adsContext;
        _logs = logs;
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

            var fileName = $"CONDTX_{month}_{day}_{dateTime.Year}";
            var myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var completePath = $@"{myDocumentsPath}\\{folder}\\{fileName}.csv";

            //fetch all data using csvHelper
            var prices = CsvUtilityHelper.GetHistoricalListFromCsv<PriceCsvDto>(completePath).ToList();
            var importedPrices = new List<Price>();

            foreach (var priceDto in prices)
            {
                decimal.TryParse(priceDto.CSEXPR, out var val);

                var price = new Price()
                {
                    Club = priceDto.CSSTOR,
                    CreatedDate = dateTime.Date,
                    Sku = priceDto.CSSKU,
                    Value = val
                };

                importedPrices.Add(price);
            }

            await _adsContext.Prices.AddRangeAsync(importedPrices);
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
                Record_Date = dateTime
            });

            _logs.InsertLogs(logs);
            throw;
        }
        throw new NotImplementedException();
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
            var prices = await _adsContext.Prices.Where(x => x.CreatedDate == dateTime).ToListAsync();
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
        throw new NotImplementedException();
    }
}