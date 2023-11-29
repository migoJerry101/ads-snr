using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Models.Dto.ItemsDto;
using ads.Utility;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

namespace ads.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowOrigin")]
    public class ImportController : ControllerBase
    {
        private readonly IInventory _inventory;
        private readonly ISales _sales;
        private readonly IAds _ads;
        private readonly IOpenQuery _openQuery;
        private readonly IInventoryBackup _inventoryBackup;
        private readonly IItem _item;
        private readonly ITotalAdsClub _totalAdsClub;
        private readonly ITotalAdsChain _totalAdsChain;

        public ImportController(
            IInventory inventory,
            ISales sales,
            IAds ads,
            IOpenQuery openQuery,
            IInventoryBackup inventoryBackup,
            IItem item,
            ITotalAdsClub totalAdsClub,
            ITotalAdsChain totalAdsChain)
        {
            _inventory = inventory;
            _sales = sales;
            _ads = ads;
            _openQuery = openQuery;
            _inventoryBackup = inventoryBackup;
            _item = item;
            _totalAdsClub = totalAdsClub;
            _totalAdsChain = totalAdsChain;
        }

        [HttpPost]
        [Route("ComputeAds")]
        public async Task<IActionResult> ComputeAds()
        {
            DateTime currentDate = DateTime.Now;
            DateTime previousDate = currentDate.Date;

            await _ads.ComputeAds(previousDate);

            return Ok();
        }

        [HttpPost]
        [Route("RecomputeAds")]
        public async Task<IActionResult> RecomputeAds(List<string> dates)
        {
            foreach (var date in dates)
            {
                //delete chain and clubs ads
                var dateFormat = DateConvertion.ConvertStringDate(date);
                var startDateInString = $"{dateFormat:yyyy-MM-dd HH:mm:ss.fff}";
                //convert date to string
                await _totalAdsChain.DeleteAdsChainAsync(startDateInString);
                await _totalAdsClub.DeleteAdsClubsAsync(startDateInString);

                var dateToCompute = dateFormat.AddDays(1).Date;
                await _ads.ComputeAds(dateToCompute);
            } 
            
            return Ok();
        }

        [HttpPost]
        [Route("GetInventory")]
        public async Task<IActionResult> GetInventory(string dates)
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                //list of Inventory within 56 days in Local DB
                var listInventoryResult = await _inventory.ListInv(dates, db);

                return Ok(new { listInventoryResult });
            }
        }

        [HttpPost]
        [Route("GetSales")]
        public async Task<IActionResult> GetSales(string dates)
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                var listOfSales = await _sales.ListSales(dates, db);

                return Ok(new { listOfSales });
            }
        }

        [HttpPost]
        [Route("GetSalesOpenQuery")]
        public async Task<IActionResult> GetSalesOpenQuery(string dates)
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                var listOfSales = await _openQuery.ListOfSales(db, dates, dates);

                return Ok(new { listOfSales });
            }
        }

        [HttpPost]
        [Route("ReImportSalesExcludingInventory")]
        public async Task<IActionResult> ReImportSalesExcludingInventory(List<string> dates)
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();
                await _openQuery.ImportItems(db);
                var items = await _item.GetAllSkuWithDate();

                foreach (var date in dates)
                {
                    var dateFormat = DateConvertion.ConvertStringDate(date);
                    var itemsSku = items.Where(x => x.CreatedDate <= dateFormat);
                    var itemsDictionary = itemsSku.Where(x => x.CreatedDate <= dateFormat).ToDictionary(y => y.Sku, y => y);
                    //listOfSales is sales from mms
                    var listOfSales = await _openQuery.ListOfSales(db, date, date);

                    var reImportedSales = listOfSales.Select(x => new Sale()
                    {
                        Date = dateFormat,
                        Sales = x.CSQTY,
                        Sku = x.CSSKU,
                        Clubs = x.CSSTOR
                    });

                    var uniqueSales = reImportedSales
                        .GroupBy(s => new { s.Clubs, s.Sku, s.Date })
                        .Select(g => new Sale()
                        {
                            Clubs = g.Key.Clubs,
                            Sku = g.Key.Sku,
                            Date = g.Key.Date,
                            Sales = g.Sum(s => s.Sales)
                        })
                        .ToList();

                    var inventories = await _inventory.GetEFInventoriesByDate(dateFormat);
                    var invetiriesInDate = inventories
                        .Where(x => itemsDictionary.TryGetValue(x.Sku, out var greneralDto))
                        .Select(y => new GeneralModel()
                        {
                            INUMBR2 = y.Sku,
                            ISTORE = y.Clubs,
                            IBHAND = y.Inventory
                        }).ToList();

                    _sales.DeleteSalesByDate(dateFormat);
                    await _sales.GetSalesAsync(date, date, itemsSku, listOfSales, invetiriesInDate);
                }
            }

            return Ok();
        }

        [HttpPost]
        [Route("ReImportSales")]
        public async Task<IActionResult> ReImportSales(List<string> dates)
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();
                await _openQuery.ImportItems(db);
                var items = await _item.GetAllSkuWithDate();
                
                foreach (var date in dates)
                {
                    var dateFormat = DateConvertion.ConvertStringDate(date);
                    var itemsSku = items.Where(x => x.CreatedDate <= dateFormat);
                    var itemsDictionary = itemsSku.Where(x => x.CreatedDate <= dateFormat).ToDictionary(y => y.Sku, y => y);
                    //listOfSales is sales from mms
                    var listOfSales = await _openQuery.ListOfSales(db, date, date);
                    
                    var reImportedSales = listOfSales.Select(x => new Sale()
                    {
                        Date = dateFormat,
                        Sales = x.CSQTY,
                        Sku = x.CSSKU,
                        Clubs = x.CSSTOR
                    });

                    var uniqueSales = reImportedSales
                        .GroupBy(s => new { s.Clubs, s.Sku, s.Date })
                        .Select(g => new Sale()
                        {
                            Clubs = g.Key.Clubs,
                            Sku = g.Key.Sku,
                            Date = g.Key.Date,
                            Sales = g.Sum(s => s.Sales)
                        })
                        .ToList();

                    var sales = await _sales.GetSalesByDateEf(dateFormat);
                    var updatedSales = _sales.GetAdjustedSalesValue(sales, uniqueSales);

                    //updates inventory using Updated sales
                    await _inventory.BatchUpdateInventoryBysales(updatedSales);

                    var inventories = await _inventory.GetEFInventoriesByDate(dateFormat);
                    var invetiriesInDate = inventories
                        .Where(x => itemsDictionary.TryGetValue(x.Sku, out var greneralDto))
                        .Select(y => new GeneralModel()
                        {
                            INUMBR2 = y.Sku,
                            ISTORE = y.Clubs,
                            IBHAND = y.Inventory
                        }).ToList();

                    _sales.DeleteSalesByDate(dateFormat);
                    await _sales.GetSalesAsync(date, date, itemsSku, listOfSales, invetiriesInDate);
                }
            }

            return Ok();
        }


        [HttpPost]
        [Route("Computation")]
        public async Task<IActionResult> GetComputation(string start)
        {
            var computation = await _ads.GetComputation(start);

            return Ok(computation);
        }

        [HttpPost]
        [Route("ImportClubs")]
        public async Task<IActionResult> ImportClubs()
        {
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                await _openQuery.ImportItems(db);

                return Ok();
            }
        }

        [HttpPost]
        [Route("ImportInventoryBackUp")]
        public async Task<IActionResult> ImportInventoryBackUp()
        {
            await _inventoryBackup.ManualImport();

            return Ok();
        }

        [HttpPost]
        [Route("ImportExcelFile")]
        public async Task<IActionResult> ImportExcelFile()
        {
            //string filePath = @"C:\Users\jbayoneta\Desktop\10-06-2023-Inventory.xlsx";
            string filePath = @"C:\Users\jbayoneta\Desktop\test.csv";
            var listClubs = new List<string> { "201", "203", "204", "205", "206", "207", "208", "209", "210", "211", "212", "213", "214", "215", "216", "217", "218", "219", "220", "221", "222", "223", "224", "225", "226", "227" };


            try
            {
                var excelData = new List<Dictionary<string, string>>();

                using (var spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
                {
                    var workbookPart = spreadsheetDocument.WorkbookPart;
                    var worksheetPart = workbookPart.WorksheetParts.First();
                    var worksheet = worksheetPart.Worksheet;
                    var sharedStringTablePart = workbookPart.SharedStringTablePart;

                    var rows = worksheet.Descendants<Row>().ToList();
                    var headerRow = rows.First();
                    var columnHeaders = headerRow.Elements<Cell>()
                        .Select(x => GetCellValue(x, sharedStringTablePart))
                        .ToList();

                    foreach (var row in rows.Skip(1)) // Skip the header row
                    {
                        var rowData = new Dictionary<string, string>();
                        var cells = row.Elements<Cell>().ToList();

                        for (int i = 0; i < cells.Count; i++)
                        {
                            string cellValue = GetCellValue(cells[i], sharedStringTablePart);
                            string columnHeader = columnHeaders[i];
                            rowData[columnHeader] = cellValue;
                        }

                        excelData.Add(rowData);
                    }

                    // Now, excelData contains the content of the Excel file.
                    // You can process the data or return it as needed. 

                    var inventories = new List<Inv>();
                    var skuInClubs = new List<string>();
                    var skuInClubsDic = new Dictionary<string, Dictionary<string, string>>();

                    foreach (var club in listClubs)
                    {
                        using (OledbCon db = new OledbCon())
                        {
                            await db.Con.OpenAsync();
                            skuInClubs = await _openQuery.ListIventorySkuPerClub(db, club);
                            var test = skuInClubs.ToDictionary(x => x);
                            skuInClubsDic[club] = test;
                        }
                    }

                    foreach (var data in excelData)
                    {
                        data.TryGetValue("SKU", out var skuOut);

                        foreach (var club in listClubs)
                        {
                            data.TryGetValue($"{club}_INV_QTY", out var invOut);
                            skuInClubsDic.TryGetValue(club, out var test);


                            if (test.TryGetValue(skuOut, out var xxx))
                            {
                                var invInDecimal = Convert.ToDecimal(invOut);

                                var inventory = new Inv()
                                {
                                    Sku = skuOut,
                                    Date = DateTime.Now.AddDays(-4),
                                    Clubs = club,
                                    Inventory = invInDecimal > 0 ? invInDecimal : 0,
                                };

                                inventories.Add(inventory);
                            }
                            else
                            {
                                var inventory = new Inv()
                                {
                                    Sku = skuOut,
                                    Date = DateTime.Now.AddDays(-4),
                                    Clubs = club,
                                    Inventory = 0,
                                };

                                inventories.Add(inventory);
                            }
                        }
                    }

                    return Ok(inventories);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error reading Excel file: {ex.Message}");
            }
        }

        private string GetCellValue(Cell cell, SharedStringTablePart sharedStringTablePart)
        {
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (int.TryParse(cell.InnerText, out int sharedStringIndex))
                {
                    var sharedStringItem = sharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(sharedStringIndex);

                    return sharedStringItem.Text.Text;
                }
            }

            return cell.InnerText;
        }

        [HttpPost]
        [Route("ImportExcelFilePerDay")]
        public async Task<IActionResult> ImportExcelFilePerDay()
        {
            string filePath = @"C:\Users\jbayoneta\Desktop\10-07-2023-ADS.csv";
            var masterDictionary = new Dictionary<string, Dictionary<string, string>>();
            var listClubs = new List<string> { "201", "203", "204", "205", "206", "207", "208", "209", "210", "211", "212", "213", "214", "215", "216", "217", "218", "219", "220", "221", "222", "223", "224", "225", "226", "227" };

            var dateYesterday = DateTime.Now.AddDays(-6);
            var dateYWithZeroTime = new DateTime(dateYesterday.Year, dateYesterday.Month, dateYesterday.Day, 0, 0, 0, 0);

            var inventoryToday = await _inventory.GetInventoriesByDate(dateYWithZeroTime);
            var testInv = _inventory.GetDictionayOfPerClubhlInventory(inventoryToday);

            using (OledbCon db = new OledbCon())
            {


                foreach (var club in listClubs)
                {
                    await db.Con.OpenAsync();
                    var skuClubs = await _openQuery.ListIventorySkuPerClub(db, club);
                    var skuClubsDictionary = skuClubs.ToDictionary(x => x);
                    masterDictionary[club] = skuClubsDictionary;
                    db.Con.Close();
                }
            }

            try
            {
                var invList = new List<Inv>();
                var items = await _item.GetAllItemSku();

                using (StreamReader reader = new StreamReader(filePath))
                {
                    var date = DateTime.Now.AddDays(-4);
                    var dateZeroTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0);

                    var count = 0;
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] cells = line.Split(','); // Assuming CSV uses a comma as the delimiter
                        var sku = cells[2];
                        var club = cells[3];
                        var inv = cells[4].Contains("0E") ? "0" : cells[4];
                        var isInClub = false;

                        if (count != 0)
                        {

                            if (masterDictionary.TryGetValue(club, out var subDic))
                            {
                                isInClub = subDic.TryGetValue(sku, out var skuOut);
                            }
                        }

                        //var hasValue = testInv.TryGetValue($"{sku}{club}", out var inventoryOut);

                        if (count != 0 && listClubs.Contains(club) && isInClub )
                        {
                            var invModel = new Inv()
                            {
                                Date = dateZeroTime,
                                Sku = sku,
                                Clubs = club,
                                Inventory = Convert.ToDecimal(inv),
                            };

                            invList.Add(invModel);
                        }
                        count++;

                        Console.WriteLine();
                    }
                }

                var invDic = invList.GroupBy(x => new { x.Sku, x.Clubs }).ToDictionary(
                    x => x.Key,
                    y => new Inv()
                    {
                        Inventory = y.Sum(item => item.Inventory),
                        Clubs = y.First().Clubs,
                        Sku = y.First().Sku,
                        Date = y.First().Date,
                    });

                var finalList = invDic.Values.ToList();

                //using (OledbCon db = new OledbCon())
                //{
                //    await db.Con.OpenAsync();

                //    using (var transaction = db.Con.BeginTransaction())
                //    {
                //        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                //        {
                //            bulkCopy.DestinationTableName = "tbl_inv";
                //            bulkCopy.BatchSize = 1000;

                //            var dataTable = new DataTable();
                //            dataTable.Columns.Add("Id", typeof(int));
                //            dataTable.Columns.Add("Date", typeof(DateTime));
                //            dataTable.Columns.Add("Sku", typeof(string));
                //            dataTable.Columns.Add("Inventory", typeof(decimal));
                //            dataTable.Columns.Add("Clubs", typeof(string));

                //            foreach (var rawData in finalList)
                //            {
                //                var row = dataTable.NewRow();
                //                row["Date"] = rawData.Date;
                //                row["Sku"] = rawData.Sku;
                //                row["Inventory"] = rawData.Inv;
                //                row["Clubs"] = rawData.Clubs;
                //                dataTable.Rows.Add(row);

                //            }
                //            await bulkCopy.WriteToServerAsync(dataTable);
                //        }

                //        transaction.Commit();
                //    }
                //}

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error reading Excel file: {ex.Message}");
            }
        }

    }
}
