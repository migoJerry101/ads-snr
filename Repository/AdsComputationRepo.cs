using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using System.Globalization;
using ads.Data;
using System.Data;
using System.Runtime.InteropServices;
using System.Linq;

namespace ads.Repository
{
    public class AdsComputationRepo : IAdsComputation
    {
        public async Task<List<TotalAPD>> GetTotalApdAsync()
        {
            List<DataRows> listData = new List<DataRows>();

            DateTime currentDate = DateTime.Now;

            string startDate = currentDate.ToString("yyMMdd");

            string dateListString = string.Join(",", DateCompute(startDate).Select(date => $"'{date}'"));
            dateListString = dateListString.TrimEnd(',');
            string[] dateParts = dateListString.Split(',');
            string fistDatePart = dateParts.FirstOrDefault();
            string lastDatePart = dateParts.LastOrDefault();
            string firstDate = fistDatePart.Trim('\'');
            string lastDate = lastDatePart.Trim('\'');

            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

                Console.WriteLine(lastDate);

                string dataQuery = "select MAX(data.Clubs) Clubs, MAX(data.Sku) Sku, " +
                    "CASE WHEN SUM(inv.Inventory) < 0 THEN 0 ELSE SUM(inv.Inventory) END AS Inventory, " +
                    "SUM(CASE WHEN data.Sales > 0 THEN data.Sales ELSE 0 END) AS Sales, " +
                    "MAX(data.Date) Date " +
                    "from tbl_data as data " +
                    "inner join tbl_inv as inv on data.Sku = inv.Sku " +
                    //"where data.Sku in ('94090', '67351', '133625', '137494') and data.Date in (" + dateListString + ") " +
                    "where data.Date in (" + dateListString + ") " +
                    //"where data.Sales != 0 and inv.Inventory != 0 and data.Date in (" + dateListString + ") " +
                    "group by data.Sku, data.Date";
                //"fetch first 10 rows only')";

                using (SqlCommand cmd = new SqlCommand(dataQuery, db.Con))
                {
                    // Increase the command timeout value (5hrs) to a higher value
                    cmd.CommandTimeout = 18000;
                    //cmd.Parameters.AddWithValue("@Date", dateListString);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            DataRows Olde = new DataRows
                            {
                                Clubs = reader["Clubs"].ToString(),
                                Sku = reader["Sku"].ToString(),
                                Inventory = Convert.ToDecimal(reader["Inventory"].ToString()),
                                Sales = Convert.ToDecimal(reader["Sales"].ToString()),
                                Date = reader["Date"].ToString(),
                            };

                            listData.Add(Olde);
                        }
                    }
                }
                Console.WriteLine(listData);
            }

            List<TotalAPD> totalAPDs = new List<TotalAPD>();
            //Filter sku and sum of sales
            var groupedBy = listData.GroupBy(x => x.Sku).ToDictionary(y => y.Key, y => y.Sum(z => {
                
                if (z.Inventory > 0)
                {
                    return z.Sales;
                }

                return 0;
            }));

            //Filter Date for get 56 days
            var totalDiv = listData.Select(x => x.Date).Distinct().Count();

            foreach (var dataSku in listData)
            {
                var checkSku = listData.Where(x => x.Sku == dataSku.Sku && x.Sales == 0 && x.Inventory == 0);

                if (checkSku.Any())
                {
                    foreach (var s in checkSku)
                    {
                        totalDiv -= 1;
                    }
                }
            }

            //Distinct of SKU
            var filter = listData.Select(x => new {
                Sku = x.Sku,
            }).Distinct().ToList();

            foreach (var f in filter)
            {
                decimal result = 0;

                groupedBy.TryGetValue(f.Sku.ToString(), out decimal totalSales);

                if (totalSales >= long.MinValue && totalSales <= long.MaxValue)
                {
                    result = (long)totalSales;
                }

                var totalAPDDecimal = Math.Round(result / totalDiv, 2);
                Console.WriteLine(totalAPDDecimal);

                if (totalDiv != 0)
                {
                    groupedBy.TryGetValue(f.Sku.ToString(), out decimal salesOut);
                    long totalAPD = Convert.ToInt64(totalAPDDecimal);
                    Console.WriteLine(totalAPD);

                    totalAPDs.Add(new TotalAPD
                    {
                        Divisor = totalDiv,
                        Sales = salesOut,
                       
                        Ads = totalAPD,
                        Date = lastDate,
                        Sku = f.Sku.ToString(),
                        StartDate = lastDate,
                        EndDate = firstDate
                    });
                }
            }

            Console.WriteLine(totalAPDs);

            //Bluk insert
            using (OledbCon db = new OledbCon())
            {
                await db.OpenAsync();

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
                        dataTable.Columns.Add("Divisor", typeof(string));
                        dataTable.Columns.Add("Date", typeof(string));
                        dataTable.Columns.Add("Ads", typeof(decimal));
                        dataTable.Columns.Add("StartDate", typeof(string));
                        dataTable.Columns.Add("EndDate", typeof(string));

                        foreach (var rawData in totalAPDs)
                        {
                            var row = dataTable.NewRow();
                            row["Sku"] = rawData.Sku;
                            row["Sales"] = rawData.Sales;
                            row["Divisor"] = rawData.Divisor;
                            row["Date"] = rawData.Date;
                            row["Ads"] = rawData.Ads;
                            row["StartDate"] = rawData.StartDate;
                            row["EndDate"] = rawData.EndDate;
                            dataTable.Rows.Add(row);
                        }
                        await bulkCopy.WriteToServerAsync(dataTable);
                    }

                    transaction.Commit();
                }
            }

            return totalAPDs;
        }

        public List<string> DateCompute(string startDateStr)
        {
            List<string> listDate = new List<string>();

            // Get the current date
            DateTime currentDate = DateTime.ParseExact(startDateStr, "yyMMdd", CultureInfo.InvariantCulture);

            // Subtract one day
            DateTime previousDate = currentDate.AddDays(-1);

            DateTime endDate = previousDate;
            DateTime startDate = endDate.AddDays(-55);


            List<DateTime> datesInRange = GetDatesInRange(startDate, endDate);

            foreach (DateTime date in datesInRange)
            {
                Console.WriteLine(date.ToString("yyMMdd"));

                listDate.Add(date.ToString("yyMMdd"));
            }

            return listDate.ToList();
        }

        public List<DateTime> GetDatesInRange(DateTime startDate, DateTime endDate)
        {
            List<DateTime> datesInRange = new List<DateTime>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                datesInRange.Add(date);
            }

            return datesInRange;
        }

    }
}
