using Microsoft.Data.SqlClient;

namespace ads.Data
{
    public class OledbCon : IDisposable
    {
        public SqlConnection Con;

        public OledbCon()
        {
            string Catalog = "ADS.UAT";
            string strConn = "data source='199.84.0.201';Initial Catalog=" + Catalog + ";User Id=sa;password=@dm1n@8800;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            //string strConn = "data source='199.84.1.203';Initial Catalog=" + Catalog + ";User Id=apps_wms_dev;password=P@55w0rd;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            Con = new SqlConnection(strConn);
            //this.Con.Open();
        }

        public async Task OpenAsync()
        {
            await Con.OpenAsync();
        }
        public OledbCon(string ConnectionString)
        {
            Con = new SqlConnection(ConnectionString);
            //this.Con.Open();
        }
        public async Task OpenAsync(string ConnectionString)
        {
            Con = new SqlConnection(ConnectionString);
            await Con.OpenAsync();
        }

        public void Dispose()
        {
            Con.Close();
        }
    }

}
