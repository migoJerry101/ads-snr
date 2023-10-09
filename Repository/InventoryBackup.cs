using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.OleDb;

namespace ads.Repository
{
    public class InventoryBackup : IInventoryBackup
    {
        private readonly InventoryBackUpContex _inventoryBackUpContex;
        private readonly IItem _item;
        private readonly IOpenQuery _openQuery;
        private readonly ISales _sales;
        private readonly IInvetory _invetory;

        public InventoryBackup(InventoryBackUpContex inventoryBackUpContex, IItem item, IOpenQuery openQuery, ISales sales, IInvetory invetory)
        {
            _inventoryBackUpContex = inventoryBackUpContex;
            _item = item;
            _openQuery = openQuery;
            _sales = sales;
            _invetory = invetory;
        }
        public async Task ManualImport()
        {
            var listClubs = new List<string> { "201", "203", "204", "205", "206", "207", "208", "209", "210", "211", "212", "213", "214", "215", "216", "217", "218", "219", "220", "221", "222", "223", "224", "225", "226", "227" };
            var listTtotal = new List<GeneralModel>();
            var salesInGeneral = new List<GeneralModel>();
            var skuInClubs = new List<string>();
            var items = await _item.GetAllItemSku();
            var date = DateTime.Now.AddDays(-3);
            var dateYesterday = DateTime.Now.AddDays(-4);
            var dateWithZeroTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0);
            var dateYWithZeroTime = new DateTime(dateYesterday.Year, dateYesterday.Month, dateYesterday.Day, 0, 0, 0, 0);


            var dict = items.ToDictionary(x => x);
            //var sales = await _sales.GetSalesByDate(dateWithZeroTime);
            //var salesDict = sales.ToDictionary(x => $"{x.Sku}{x.Clubs}", y => y.Sales);
            //var salesInGeneralModel = sales.Select(x => new GeneralModel()
            //{
            //    CSSKU = x.Sku,
            //    CSDATE = $"'{dateWithZeroTime.ToString("yyMMdd")}'"
            //});

            using (OledbCon db = new OledbCon())
            {
                await db.Con.OpenAsync();
                salesInGeneral = await _openQuery.ListOfSales(db, $"{dateWithZeroTime.ToString("yyMMdd")}", $"{dateWithZeroTime.ToString("yyMMdd")}");
            }

            var inventoryToday = await _invetory.GetInventoriesByDate(dateYWithZeroTime);
            var testInv = _invetory.GetDictionayOfPerClubhlInventory(inventoryToday);

            listTtotal = new List<GeneralModel>();

            foreach (var club in listClubs)
            {
                using (OledbCon db = new OledbCon())
                {
                    await db.Con.OpenAsync();
                    skuInClubs = await _openQuery.ListIventorySkuPerClub(db, club);
                }

                string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\jbayoneta\Documents\1006\{club}_curpric.accdb";

                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM INVDPT";
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            var list = new List<GeneralModel>();
                            var skuPerClubDictionary = skuInClubs.ToDictionary(x => x);

                            while (reader.Read())
                            {
                                var sku = reader["INUMBR"].ToString();
                                var onHand = Convert.ToDecimal(reader["ONHAND"].ToString());
                                var hasValue = testInv.TryGetValue($"{sku}{club}", out var inventoryOut);
                                //var hasSales = salesDict.TryGetValue($"{sku}{club}", out var salesout);
                                var hasTest = skuPerClubDictionary.TryGetValue(sku, out var skuOut);

                                if ( hasValue)
                                {
                                    var inventory = new GeneralModel()
                                    {
                                        IBHAND = onHand > 0 ? onHand : 0,
                                        ISTORE = club,
                                        INUMBR2 = sku,
                                    };

                                    listTtotal.Add(inventory);
                                }
                                else
                                {
                                    if (hasTest)
                                    {
                                        var inventory = new GeneralModel()
                                        {
                                            IBHAND = onHand > 0 ? onHand : 0,
                                            ISTORE = club,
                                            INUMBR2 = sku,
                                        };

                                        listTtotal.Add(inventory);
                                    }
                                }
                            }
                        }
                    }
                }
            }


            var skuInGeneral = items.Select(x => new GeneralModel()
            {
                INUMBR = x,
            });
            await _invetory.GetInventoryAsync(date.ToString("yyMMdd"), date.ToString("yyMMdd"), skuInGeneral.ToList(), salesInGeneral, listTtotal);



            using (OledbCon db = new OledbCon())
            {
                if (db.Con.State == ConnectionState.Closed)
                {
                    db.Con.Open();
                }

                //using (var transaction = db.Con.BeginTransaction())
                //{
                //    using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                //    {
                //        bulkCopy.DestinationTableName = "tbl_inv";
                //        bulkCopy.BatchSize = 1000;

                //        var dataTable = new DataTable();
                //        dataTable.Columns.Add("Id", typeof(int));
                //        dataTable.Columns.Add("Date", typeof(DateTime));
                //        dataTable.Columns.Add("Sku", typeof(string));
                //        dataTable.Columns.Add("Inventory", typeof(decimal));
                //        dataTable.Columns.Add("Clubs", typeof(string));

                //        foreach (var rawData in listTtotal)
                //        {
                //            var row = dataTable.NewRow();
                //            row["Date"] = rawData.Date;
                //            row["Sku"] = rawData.Sku;
                //            row["Inventory"] = rawData.Inv;
                //            row["Clubs"] = rawData.Clubs;
                //            dataTable.Rows.Add(row);

                //        }
                //        await bulkCopy.WriteToServerAsync(dataTable);
                //    }

                //    //transaction.Commit();
                //}
            }


            var test = listTtotal;
        }
    }
}
