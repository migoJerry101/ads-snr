using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ads.Repository
{
    public class OpenQueryRepo : IOpenQuery
    {

        //ListINVMST - List of All SKU Filter ISTYPE = ''01'' AND IDSCCD IN (''A'',''I'',''D'',''P'') AND IATRB1 IN (''L'',''I'',''LI'')
        //ISTYPE - Type of SKU
        //IDSCCD - Status of SKU
        //IATRB1 - Attribute of SKU
        public async Task<List<GeneralModel>> ListOfAllSKu(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

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

            return list.ToList();
        }

        //ListCSHDET - List of Sales GroupBy SKu,,store,Date
        public async Task<List<GeneralModel>> ListOfSales(OledbCon db, string start, string end)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            string query = "select * from Openquery([snr], 'SELECT CSSKU, CSDATE, MAX(CSSTOR) CSSTOR, SUM(CSQTY) CSQTY from MMJDALIB.CSHDET where CSDATE BETWEEN ''" + start + "'' AND ''" + end + "'' GROUP BY CSSKU ,CSDATE ')";
            //string query = "select * from Openquery([snr], 'SELECT CSSKU, CSDATE, CSSTOR, SUM(CSQTY) CSQTY from MMJDALIB.CSHDET where CSDATE BETWEEN ''" + start + "'' AND ''" + end + "'' GROUP BY CSSKU, CSSTOR ,CSDATE ')";

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

            return list.ToList();
        }

        //ListINVBAL - List of Inventory Groupby SKU
        public async Task<List<GeneralModel>> ListIventory(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

            string query = "select * from Openquery([snr], 'SELECT INUMBR ,Max(ISTORE) ISTORE , CASE WHEN SUM(IBHAND) < 0 THEN 0 ELSE SUM(IBHAND) END AS IBHAND from MMJDALIB.INVBAL GROUP BY INUMBR')";
            //string query = "select * from Openquery([snr], 'SELECT INUMBR ,ISTORE, CASE WHEN MAX(IBHAND) < 0 THEN 0 ELSE MAX(IBHAND) END AS IBHAND from MMJDALIB.INVBAL GROUP BY INUMBR ,ISTORE')";
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

            return list.ToList();
        }

        //ListTBLSTR - List of ALL STORE with Filter STPOLL = ''Y'' AND STSDAT > 0
        //STPOLL - Identify Store Open
        // STSDAT - Date Open of Store
        public async Task<List<GeneralModel>> ListOfAllStore(OledbCon db)
        {
            List<GeneralModel> list = new List<GeneralModel>();

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

            return list.ToList();
        }

    }
}
