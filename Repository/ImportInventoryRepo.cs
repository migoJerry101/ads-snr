using ads.Data;
using ads.Interface;
using ads.Models.Data;
using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace ads.Repository
{
    public class ImportInventoryRepo : IImportInventory
    {
        private readonly ILogs _logs;

        public ImportInventoryRepo(IOpenQuery openQuery, ILogs logs)
        {
            _logs = logs;
        }

        public async Task<List<Inventory>> GetInventory(string start, string end)
        {

            List<Inventory> ListInventory = new List<Inventory>();
            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            try
            {
                var pageSize = 400000;

                // Get the total count of rows for your date filter
                var rowCount = await Countdata(start, end);

                // Calculate the total number of pages
                var totalPages = (int)Math.Ceiling((double)rowCount / pageSize);
                for (int pageNumber = 0; pageNumber < totalPages; pageNumber++)
                {
                    int offset = pageSize * pageNumber;

                    //OleDb Select Query Invetory
                    using (OldInventoryDBCon db = new OldInventoryDBCon())
                    {
                        await db.OpenAsync(); // Open the connection asynchronously

                        //string query = "select * from tbl_inv where Date in (" + dateListString + ") ";   
                        //string query = "select * from ConsolidatedHistoricalData where DateKey in (" + dateListString + ") " +
                        string query = "select * from ConsolidatedHistoricalData where DateKey BETWEEN '" + start + "' AND '" + end + "' " +
                           "ORDER BY DateKey OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY ";
                        //string query = "select * from tbl_inv where Date = '230911' ";

                        using (SqlCommand cmd = new SqlCommand(query, db.Con))
                        {
                            cmd.Parameters.AddWithValue("@Offset", offset);
                            cmd.Parameters.AddWithValue("@PageSize", pageSize);
                            //cmd.Parameters.AddWithValue("@Date", start);
                            cmd.CommandTimeout = 18000;

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    Inventory Olde = new Inventory
                                    {
                                        Clubs = reader["Store"].ToString(),
                                        Sku = reader["Sku"].ToString(),
                                        Inv = Convert.ToDecimal(reader["OnHand"].ToString()),
                                        Date = ConvertStringDate(reader["DateKey"].ToString()),
                                    };

                                    Console.WriteLine(reader["DateKey"].ToString());
                                    ListInventory.Add(Olde);
                                }
                            }
                        }

                    }

                }


                using (OledbCon db = new OledbCon())
                {
                    await db.OpenAsync(); // Open the connection asynchronously

                    //Bluk insert in tbl_Data table
                    using (var transaction = db.Con.BeginTransaction())
                    {
                        using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                        {
                            bulkCopy.DestinationTableName = "tbl_inv";
                            //bulkCopy.DestinationTableName = "tbl_inv_with_clubs";
                            bulkCopy.BatchSize = 1000;

                            var dataTable = new DataTable();
                            dataTable.Columns.Add("Id", typeof(int));
                            dataTable.Columns.Add("Date", typeof(DateTime));
                            dataTable.Columns.Add("Sku", typeof(string));
                            dataTable.Columns.Add("Inventory", typeof(decimal));
                            dataTable.Columns.Add("Clubs", typeof(string));

                            foreach (var rawData in ListInventory)
                            {
                                var row = dataTable.NewRow();
                                row["Date"] = rawData.Date;
                                row["Sku"] = rawData.Sku;
                                row["Inventory"] = rawData.Inv;
                                row["Clubs"] = rawData.Clubs;
                                dataTable.Rows.Add(row);

                            }
                            await bulkCopy.WriteToServerAsync(dataTable);
                        }

                        transaction.Commit();
                    }
                }

                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Inventory",
                    Message = "Total Inventory Inserted : " + ListInventory.Count + "",
                    Record_Date = start
                });

                _logs.InsertLogs(Log);



                return ListInventory.ToList();

            }
            catch (Exception e)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "Error Inserted : " + e.Message + "," + start + " ",
                    Record_Date = start
                });

                _logs.InsertLogs(Log);

                return ListInventory.ToList();
            }


        }

        //TotalCount of Data
        public async Task<int> Countdata(string start, string end)
        {
            int totalCount = 0;

            //string query = "select COUNT(*) as Count from ConsolidatedHistoricalData where DateKey in (" + dateListString + ") ";
            string query = "select COUNT(*) as Count from ConsolidatedHistoricalData where DateKey BETWEEN '" + start + "' AND '" + end + "' ";

            using (OldInventoryDBCon db = new OldInventoryDBCon())
            {
                await db.OpenAsync(); // Open the connection asynchronously

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            totalCount = (int)reader["Count"];
                        }
                    }
                }

                return totalCount;
            }

        }

        public DateTime ConvertStringDate(string? date)
        {
            // Input string in the format "yyMMdd"
            string dateString = date; // for September 20, 2023

            // Define the format you expect
            string format = "yyMMdd";

            // Specify the culture (optional)
            CultureInfo culture = CultureInfo.InvariantCulture;

            try
            {
                // Parse the string into a DateTime object
                DateTime dateTime = DateTime.ParseExact(dateString, format, culture);

                // Output the resulting DateTime
                Console.WriteLine("Parsed DateTime: " + dateTime.ToString("yyyy-MM-dd"));

                // Remove the time portion and keep only the date part
                DateTime dateOnly = dateTime.Date;

                return dateOnly;
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid date format.");

                return DateTime.Now;
            }
        }
    }
}
