using System;
using System.Configuration;
using System.Reflection;
using CFS.Crypto;
using Cfs.Salesforce.Model;
using OracleOrm.Utils;

namespace Cfs.Salesforce.DataContext.Connections
{
    public class DbCxn
    {

        public static string OracleSfdcConnectionString()
        {
            string connectionString = null;
            string connName = null;
            string dataSource = null;


            if (ConfigurationManager.AppSettings["OracleSfdcConnectionString"] != null)
            {
                connName = ConfigurationManager.AppSettings["OracleSfdcConnectionString"];
            }

            try
            {
                if (string.IsNullOrEmpty(SalesforceLoaderInfo.OracleSfdcConnectionString))
                {
                    var c = new ConnSvc.ConnectionsClient();
                    c.Endpoint.Address =
                        new System.ServiceModel.EndpointAddress(ConfigurationManager.AppSettings["ConnectionUrl"]);
                    var result = c.ConnectionInfo(connName);

                    var dSource = Encryption.Decrypt(result.DataSource);
                    dataSource = dSource.Split('|')[0];
                    string userName = Encryption.Decrypt(result.UserName);
                    string password = Encryption.Decrypt(result.Password);

                    connectionString = String.Format("Data Source={0};User Id={1};Password={2}", dataSource.ToUpper(),
                                                     userName, password);
                    SalesforceLoaderInfo.OracleSfdcConnectionString = connectionString;
                }
                else
                {
                    connectionString = SalesforceLoaderInfo.OracleSfdcConnectionString;
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", string.Format(MethodBase.GetCurrentMethod().Name + " method faied in DbCxn class \n" + ex));
                throw;
            }


            return connectionString;

        }
    }
}
