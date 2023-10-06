using ads.Data;
using ads.Interface;
using ads.Models.Data;
using ads.Utility;
using Microsoft.Data.SqlClient;
using System.Data;
using static System.Reflection.Metadata.BlobBuilder;

namespace ads.Repository
{
    public class OpenQueryRepo : IOpenQuery
    {
        //private readonly DateConvertion dateConvertion = new DateConvertion();

        private readonly ILogs _logs;

        //ListINVMST - List of All SKU Filter ISTYPE = ''01'' AND IDSCCD IN (''A'',''I'',''D'',''P'') AND IATRB1 IN (''L'',''I'',''LI'')
        //ISTYPE - Type of SKU
        //IDSCCD - Status of SKU
        //IATRB1 - Attribute of SKU
        public async Task<List<GeneralModel>> ListOfAllSKu(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            try 
            {
                string query = "select * from Openquery([snr], 'SELECT INUMBR from MMJDALIB.INVMST WHERE ISTYPE = ''01'' AND IDSCCD IN (''A'',''I'',''D'',''P'') AND IATRB1 IN (''L'',''I'',''LI'')')";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            GeneralModel Olde = new GeneralModel
                            {
                                INUMBR = reader["INUMBR"].ToString()
                            };

                            list.Add(Olde);
                        }
                    }
                }
            }
            catch (Exception e) 
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "ListOfAllSKu : " + e.Message + " ",
                    Record_Date = Convert.ToDateTime(startLogs.ToString("yyyy-MM-dd 00:00:00.000"))

                });

                _logs.InsertLogs(Log);
            }

            return list.ToList();
        }

        //ListCSHDET - List of Sales GroupBy SKu,,store,Date
        public async Task<List<GeneralModel>> ListOfSales(OledbCon db, string start, string end)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            try
            {
                //string query = "select * from Openquery([snr], 'SELECT CSSKU, CSDATE, MAX(CSSTOR) CSSTOR, SUM(CSQTY) CSQTY from MMJDALIB.CSHDET where CSDATE BETWEEN ''" + start + "'' AND ''" + end + "'' GROUP BY CSSKU ,CSDATE ')";
                string query = "select * from Openquery([snr], 'SELECT CSSKU, CSDATE, CSSTOR, CASE WHEN SUM(CSQTY) < 0 THEN 0 ELSE SUM(CSQTY) END AS CSQTY from MMJDALIB.CSHDET where CSDATE BETWEEN ''" + start + "'' AND ''" + end + "'' GROUP BY CSSKU, CSSTOR ,CSDATE ')";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            GeneralModel Olde = new GeneralModel
                            {
                                CSDATE = reader["CSDATE"].ToString(),
                                CSSTOR = reader["CSSTOR"].ToString(),
                                CSSKU = reader["CSSKU"].ToString(),
                                CSQTY = Convert.ToDecimal(reader["CSQTY"].ToString())
                            };

                            list.Add(Olde);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "ListOfSales : " + e.Message + " ",
                    Record_Date = Convert.ToDateTime(startLogs.ToString("yyyy-MM-dd 00:00:00.000"))

                });

                _logs.InsertLogs(Log);
            }

            return list.ToList();
        }

        //ListINVBAL - List of Inventory Groupby SKU
        public async Task<List<GeneralModel>> ListIventory(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            try
            {
                //string query = "select * from Openquery([snr], 'SELECT INUMBR ,Max(ISTORE) ISTORE , CASE WHEN SUM(IBHAND) < 0 THEN 0 ELSE SUM(IBHAND) END AS IBHAND from MMJDALIB.INVBAL GROUP BY INUMBR')";
                string query = "select * from Openquery([snr], 'SELECT INUMBR ,ISTORE, CASE WHEN MAX(IBHAND) < 0 THEN 0 ELSE MAX(IBHAND) END AS IBHAND from MMJDALIB.INVBAL GROUP BY INUMBR ,ISTORE')";
                //string query = "select * from Openquery([snr], 'SELECT MST.INUMBR, MAX(BAL.ISTORE) ISTORE, SUM(BAL.IBHAND) IBHAND from MMJDALIB.INVMST as MST " +
                //    "INNER JOIN MMJDALIB.INVBAL as BAL on MST.INUMBR = BAL.INUMBR " +
                //    "WHERE MST.ISTYPE = ''01'' AND MST.IDSCCD IN (''A'',''I'',''D'',''P'') AND MST.IATRB1 IN (''L'',''I'',''LI'') " +
                //    "GROUP BY MST.INUMBR')";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            GeneralModel Olde = new GeneralModel
                            {
                                INUMBR2 = reader["INUMBR"].ToString(),
                                ISTORE = reader["ISTORE"].ToString(),
                                IBHAND = Convert.ToDecimal(reader["IBHAND"].ToString())
                            };

                            list.Add(Olde);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "ListIventory : " + e.Message + " ",
                    Record_Date = Convert.ToDateTime(startLogs.ToString("yyyy-MM-dd 00:00:00.000"))

                });

                _logs.InsertLogs(Log);
            }

            return list.ToList();
        }

        //ListTBLSTR - List of ALL STORE with Filter STPOLL = ''Y'' AND STSDAT > 0
        //STPOLL - Identify Store Open
        // STSDAT - Date Open of Store
        public async Task<List<GeneralModel>> ListOfAllStore(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            List<Logging> Log = new List<Logging>();

            DateTime startLogs = DateTime.Now;

            try
            {
                string query = "select * from Openquery([snr], 'SELECT STRNUM from MMJDALIB.TBLSTR WHERE STPOLL = ''Y'' AND STSDAT > 0')";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            GeneralModel Olde = new GeneralModel
                            {
                                STRNUM = reader["STRNUM"].ToString()
                            };

                            list.Add(Olde);
                        }
                    }
                }
            }
            catch(Exception e) 
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "ListOfAllStore : " + e.Message + " ",
                    Record_Date = Convert.ToDateTime(startLogs.ToString("yyyy-MM-dd 00:00:00.000"))

                });
            }

            return list.ToList();
        }

        public async Task ImportClubs(OledbCon db)
        {
            var list = new List<Club>();
            var Log = new List<Logging>();
            DateTime startLogs = DateTime.Now;

            var clubsTableName = "tbl_Clubs";
            var truncateClubQuery = $"TRUNCATE TABLE {clubsTableName}";

            try
            {
                if (db.Con.State == ConnectionState.Closed)
                {
                    db.Con.Open();
                }

                using (var command = new SqlCommand(truncateClubQuery, db.Con))
                {
                    await command.ExecuteNonQueryAsync();
                }

                string query = "select * from Openquery([snr], 'SELECT STRNUM, STRNAM ,STSDAT from MMJDALIB.TBLSTR WHERE STPOLL = ''Y'' AND STSDAT > 0')";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var dateStart = reader["STSDAT"].ToString();
                            
                            if(dateStart.Length < 6)
                            {
                                dateStart = $"0{dateStart}";
                            }

                            var Olde = new Club
                            {
                                Number = Convert.ToInt32(reader["STRNUM"].ToString()),
                                Name = reader["STRNAM"].ToString(),
                                StartDate = DateConvertion.ConvertStringDate(dateStart),
                            };

                            list.Add(Olde);
                        }
                    }

                    if (list.Count > 0)
                    {
                        using (var transaction = db.Con.BeginTransaction())
                        {
                            using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                            {
                                bulkCopy.DestinationTableName = "tbl_Clubs";
                                bulkCopy.BatchSize = 1000;

                                var dataTable = new DataTable();
                                dataTable.Columns.Add("Id", typeof(int));
                                dataTable.Columns.Add("Number", typeof(int));
                                dataTable.Columns.Add("Name", typeof(string));
                                dataTable.Columns.Add("StartDate", typeof(DateTime));


                                foreach (var club in list)
                                {
                                    var row = dataTable.NewRow();
                                    row["Id"] = club.Id;
                                    row["Number"] = club.Number;
                                    row["Name"] = club.Name;
                                    row["StartDate"] = club.StartDate;

                                    dataTable.Rows.Add(row);
                                }

                                bulkCopy.WriteToServer(dataTable);
                            }

                            transaction.Commit();
                        }
                    }

                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "ListOfAllStore : " + e.Message + " ",
                    Record_Date = Convert.ToDateTime(startLogs.ToString("yyyy-MM-dd 00:00:00.000"))
                });
            }
        }

        public async Task ImportItems(OledbCon db)
        {
            var list = new List<Item>();
            var Log = new List<Logging>();
            var startLogs = DateTime.Now;

            var clubsTableName = "tbl_Items";
            var truncateClubQuery = $"TRUNCATE TABLE {clubsTableName}";

            try
            {
                if (db.Con.State == ConnectionState.Closed)
                {
                    db.Con.Open();
                }

                using (var command = new SqlCommand(truncateClubQuery, db.Con))
                {
                    await command.ExecuteNonQueryAsync();
                }

                string query = "select * from Openquery([snr], 'SELECT INUMBR,IDESCR from MMJDALIB.INVMST WHERE ISTYPE = ''01'' AND IDSCCD IN (''A'',''I'',''D'',''P'') AND IATRB1 IN (''L'',''I'',''LI'')')";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    cmd.CommandTimeout = 18000;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new Item
                            {
                                Sku = reader["INUMBR"].ToString(),
                                Name = reader["IDESCR"].ToString()
                            };

                            list.Add(item);
                        }
                    }

                    if (list.Count > 0)
                    {
                        using (var transaction = db.Con.BeginTransaction())
                        {
                            using (var bulkCopy = new SqlBulkCopy(db.Con, SqlBulkCopyOptions.Default, transaction))
                            {
                                bulkCopy.DestinationTableName = "tbl_Items";
                                bulkCopy.BatchSize = 1000;

                                var dataTable = new DataTable();
                                dataTable.Columns.Add("Id", typeof(int));
                                dataTable.Columns.Add("SKU", typeof(int));
                                dataTable.Columns.Add("Name", typeof(string));

                                foreach (var item in list)
                                {
                                    var row = dataTable.NewRow();
                                    row["Id"] = item.Id;
                                    row["Sku"] = item.Sku;
                                    row["Name"] = item.Name;

                                    dataTable.Rows.Add(row);
                                }

                                bulkCopy.WriteToServer(dataTable);
                            }

                            transaction.Commit();
                        }
                    }

                    db.Con.Close();
                }
            }
            catch (Exception e)
            {
                DateTime endLogs = DateTime.Now;
                Log.Add(new Logging
                {
                    StartLog = startLogs,
                    EndLog = endLogs,
                    Action = "Error",
                    Message = "ListOfAllSKu : " + e.Message + " ",
                    Record_Date = Convert.ToDateTime(startLogs.ToString("yyyy-MM-dd 00:00:00.000"))

                });

                _logs.InsertLogs(Log);
            }
        }
    }
}
