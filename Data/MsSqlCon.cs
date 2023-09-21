using Microsoft.Data.SqlClient;

namespace ads.Data
{
    public class MsSqlCon : IDisposable
    {
        public SqlConnection Con;
        /// <summary>
        /// / ON PREM FINAL SIGNATURE
        /// </summary>
        public MsSqlCon()
        {
            string Catalog = "ADS.UAT";
            //string strConn = "data source='199.84.0.201';Initial Catalog=" + Catalog + ";User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            string strConn = "data source='199.84.1.203';Initial Catalog=" + Catalog + ";User Id=apps_wms_dev;password=P@55w0rd;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";

            Con = new SqlConnection(strConn);
            this.Con.Open();
        }

        public MsSqlCon(string ConnectionString)
        {
            Con = new SqlConnection(ConnectionString);
            this.Con.Open();
        }

        public void Dispose()
        {
            Con.Close();
        }
    }
}
