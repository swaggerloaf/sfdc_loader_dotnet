using System;
using System.Configuration;
using System.Reflection;
using CFS.Crypto;
using Cfs.Salesforce.Model;
using OracleOrm.Utils;

namespace Cfs.Salesforce.DataContext.Connections
{
    public class SalesforceCxn
    {
        public static string SalesforceConnectionString()
        {
            string connectionString = null;
            string connName = null;
            string dataSource = null;


            if (ConfigurationManager.AppSettings["SalesforceConnectionString"] != null)
            {
                connName = ConfigurationManager.AppSettings["SalesforceConnectionString"];
            }

            try
            {
                if (string.IsNullOrEmpty(SalesforceLoaderInfo.SalesforceConnectionString))
                {
                    var c = new ConnSvc.ConnectionsClient();
                    c.Endpoint.Address =
                        new System.ServiceModel.EndpointAddress(ConfigurationManager.AppSettings["ConnectionUrl"]);
                    var result = c.ConnectionInfo(connName);

                    string connUser = String.Empty;
                    string connPassword = String.Empty;
                    string connSecurityToken = String.Empty;
                    string connUseSandbox = String.Empty;
                    string connOffline = String.Empty;

                    foreach (var item in result.ExtendedProperties)
                    {

                        switch (item.Key.ToLower())
                        {
                            case "user":
                                connUser = Encryption.Decrypt(item.Value);
                                break;
                            case "password":
                                connPassword = Encryption.Decrypt(item.Value);
                                break;
                            case "security token":
                                connSecurityToken = Encryption.Decrypt(item.Value);
                                break;
                            case "use sandbox":
                                connUseSandbox = Encryption.Decrypt(item.Value);
                                break;
                            case "offline":
                                connOffline = Encryption.Decrypt(item.Value);
                                break;
                        }

                    }

                    connectionString = String.Format("User={0};Password={1};Security Token={2};Offline={3}; Use Sandbox={4}", connUser, connPassword, connSecurityToken, connOffline, connUseSandbox);


                    SalesforceLoaderInfo.SalesforceConnectionString = connectionString;
                }
                else
                {
                    connectionString = SalesforceLoaderInfo.SalesforceConnectionString;
                }
                return connectionString;
            }

            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", string.Format(MethodBase.GetCurrentMethod().Name + " method faied in SalesforceCxn class \n" + ex));
                throw;
            }

        }


    }
}
