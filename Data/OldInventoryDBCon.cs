using Microsoft.Data.SqlClient;

namespace ads.Data
{
    public class OldInventoryDBCon : IDisposable
    {
        public SqlConnection Con;

        public OldInventoryDBCon()
        {
            string Catalog = "CatalogContext.Development";
            string strConn = "data source='199.84.0.158';Initial Catalog=" + Catalog + ";User Id=sa_avgsale;password=pass123!@#;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            //string strConn = "data source='199.84.1.203';Initial Catalog=" + Catalog + ";User Id=apps_wms_dev;password=P@55w0rd;Trusted_Connection=false;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            Con = new SqlConnection(strConn);
            //this.Con.Open();
        }

        public async Task OpenAsync()
        {
            await Con.OpenAsync();
        }
        public OldInventoryDBCon(string ConnectionString)
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
