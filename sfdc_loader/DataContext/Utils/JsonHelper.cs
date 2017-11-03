using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace sfdc_loader.DataContext.Utils
{
    public static class JsonHelper
    {
        public static TResponse PostJson<TRequest, TResponse>(Uri url, TRequest data)
        {
            var bytes = Encoding.Default.GetBytes(ToJson(data));

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                client.Headers.Add("User-Agent", "Salesforce Data Loader");
                client.UseDefaultCredentials = true;
                client.Credentials = CredentialCache.DefaultCredentials;

                //log request for debugging
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Uri: {0}\r\nData: {1}", url.ToString(), ToJson(data));
           //     OracleOrm.Utils.CfsLogging.LogInfo("Salesforce Data Loader - PostJson Info", sb.ToString());

                var response = client.UploadData(url, "POST", bytes);

                return FromJson<TResponse>(Encoding.Default.GetString(response));
            }
        }

        public static string ToJson<T>(T instance)
        {
            return JsonConvert.SerializeObject(instance);
        }

        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}