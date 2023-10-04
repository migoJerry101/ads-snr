using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Drawing.Printing;

namespace ads.Repository
{
    public class LogsRepo : ILogs
    {

        public List<Logging> SelectLastLogs()
        {
            List<Logging> logs = new List<Logging>();

            using (MsSqlCon db = new MsSqlCon())
            {
                string query = "  SELECT TOP 1 * FROM tbl_logs ORDER BY LogId DESC";

                using (SqlCommand cmd = new SqlCommand(query, db.Con))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Logging Olde = new Logging
                            {
                                StartLog = Convert.ToDateTime(reader["StartLogs"].ToString()),
                                EndLog = Convert.ToDateTime(reader["EndLogs"].ToString()),
                                Action = reader["Action"].ToString(),
                                Message = reader["Message"].ToString(),
                                Record_Date = Convert.ToDateTime(reader["Record_Date"].ToString()),
                            };

                            logs.Add(Olde);
                        }
                    }
                }
            }

            return logs;
        }

        //Insert Logs
        public void InsertLogs(List<Logging> logging)
        {
            using (MsSqlCon db = new MsSqlCon())
            {
                string query = "INSERT INTO tbl_logs (StartLogs,EndLogs,Action,Message,Record_Date) VALUES (@StartLogs,@EndLogs,@Action, @Message,@Record_Date)";

                using (SqlCommand command = new SqlCommand(query, db.Con))
                {
                    command.Parameters.Add("@StartLogs", SqlDbType.DateTime);
                    command.Parameters.Add("@EndLogs", SqlDbType.DateTime);
                    command.Parameters.Add("@Action", SqlDbType.VarChar);
                    command.Parameters.Add("@Message", SqlDbType.VarChar);
                    command.Parameters.Add("@Record_Date", SqlDbType.VarChar);

                    foreach (var rawData in logging)
                    {
                        command.Parameters["@StartLogs"].Value = rawData.StartLog;
                        command.Parameters["@EndLogs"].Value = rawData.EndLog;
                        command.Parameters["@Action"].Value = rawData.Action;
                        command.Parameters["@Message"].Value = rawData.Message;
                        command.Parameters["@Record_Date"].Value = rawData.Record_Date;

                        int result = command.ExecuteNonQuery();

                        if (result <= 0)
                        {
                            Console.WriteLine("Error inserting data into Database!");
                        }
                    }
                }
            }

        }

    }
}
