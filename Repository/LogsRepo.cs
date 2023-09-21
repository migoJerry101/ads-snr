using ads.Data;
using ads.Interface;
using ads.Models.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ads.Repository
{
    public class LogsRepo : ILogs
    {
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
