using System;
using System.Collections.Generic;
using System.Data;
using System.Data.CData.Salesforce;
using System.Reflection;
using System.Text;
using sfdc_loader.DataContext.Connections;
using sfdc_loader.DataContext.Utils;
using sfdc_loader.Model.SfdcRecordType;



namespace sfdc_loader.DataContext.SfdcContext
{
    public class SalesforceContext : ISalesforce
    {

        public TT GetRecordTypesFromSalesforce<TT, T>()
            where TT : BaseModelList<T>
            where T : BaseModel
        {
            try
            {
                String connectionString = SalesforceCxn.SalesforceConnectionString();
                string sql =
             String.Format("SELECT Id, Name,SobjectType FROM RecordType");

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand(sql, connection);
                    SalesforceDataReader rdr = cmd.ExecuteReader();

                    var sfRecordTypes = SfdcReflectionUtil.PopulateBaseModelListFromReader<TT, T>(rdr);
                    return sfRecordTypes;
                }

            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }


        }

        public T GetRecordType<T>(string recordTypeName, string sObjectType)
        {
            var recordType = Activator.CreateInstance<T>();
            try
            {
                String connectionString = SalesforceCxn.SalesforceConnectionString();
                string sql =
                    String.Format("SELECT Id, Name,SobjectType FROM RecordType where Name='{0}' and SobjectType = '{1}'",
                        recordTypeName, sObjectType);


                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {

                    SalesforceCommand cmd = new SalesforceCommand(sql, connection);

                    SalesforceDataReader rdr = cmd.ExecuteReader();
                    recordType = SfdcReflectionUtil.PopulateBaseModelFromReader<T>(rdr);
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }
            return recordType;
        }

        public List<T> GetAccountsFromSalesforce<T>(string recordTypeId)
        {
            List<T> accounts = new List<T>();

            String connectionString = SalesforceCxn.SalesforceConnectionString();
            using (SalesforceConnection connection = new SalesforceConnection(connectionString))
            {
                SalesforceCommand cmd = new SalesforceCommand("SELECT * FROM  Account WHERE RecordTypeId = " + recordTypeId, connection);

                SalesforceDataReader rdr = cmd.ExecuteReader();

                accounts = SfdcReflectionUtil.PopulateListFromReader<T>(rdr);
            }
            return accounts;
        }
        public T CreateJobForBatchInSalesforce<T>(string salesforceObjectName, string action, string concurrency)
        {

            var salesforceBulkJob = Activator.CreateInstance<T>();
            try
            {
                String connectionString = SalesforceCxn.SalesforceConnectionString();

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand("CreateJob", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SalesforceParameter("@ObjectName", salesforceObjectName)); //ex. "Account"
                    cmd.Parameters.Add(new SalesforceParameter("@Action", action)); // ex. "Insert"
                    cmd.Parameters.Add(new SalesforceParameter("@ConcurrencyMode", concurrency)); // ex. "Serial"
                    cmd.Parameters.Add(new SalesforceParameter("@ExternalIdColumn", ""));

                    SalesforceDataReader rdr = cmd.ExecuteReader();
                    salesforceBulkJob = SfdcReflectionUtil.PopulateBaseModelFromReader<T>(rdr);

                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;

            }
            return salesforceBulkJob;
        }
        public T SendBatchData<T>(string jobId, int numberOfRecords, string dataForBatch)
        {
            var salesforceBulkBatch = Activator.CreateInstance<T>();
            try
            {

                String connectionString = SalesforceCxn.SalesforceConnectionString();

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand("CreateBatch", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SalesforceParameter("@jobId", jobId));
                    cmd.Parameters.Add(new SalesforceParameter("@Aggregate", dataForBatch));

                    StringBuilder sb = new StringBuilder();

                    SalesforceDataReader rdr = cmd.ExecuteReader();
                    salesforceBulkBatch = SfdcReflectionUtil.PopulateBaseModelFromReader<T>(rdr);
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;

            }

            return salesforceBulkBatch;
        }
        public T GetBatchFromSalesforce<T>(string batchId, string jobId)
        {

            var salesforceBulkBatch = Activator.CreateInstance<T>();

            try
            {
                String connectionString = SalesforceCxn.SalesforceConnectionString();

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand("GetBatch", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SalesforceParameter("@jobId", jobId));
                    cmd.Parameters.Add(new SalesforceParameter("@BatchId", batchId));

                    SalesforceDataReader rdr = cmd.ExecuteReader();


                    salesforceBulkBatch = SfdcReflectionUtil.PopulateBaseModelFromReader<T>(rdr);
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }


            return salesforceBulkBatch;
        }

        public TT GetSalesforceBulkBatchResult<TT, T>(string jobId, string batchId)
            where TT : BaseModelList<T>
            where T : BaseModel
        {
            try
            {
                String connectionString = SalesforceCxn.SalesforceConnectionString();

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand("GetBatchResults", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SalesforceParameter("@JobId", jobId));
                    cmd.Parameters.Add(new SalesforceParameter("@BatchId", batchId));

                    SalesforceDataReader rdr = cmd.ExecuteReader();

                    var salesforceBulkBatchResults = SfdcReflectionUtil.PopulateBaseModelListFromReader<TT, T>(rdr);
                    return salesforceBulkBatchResults;
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }

        }

        public List<T> GetSalesforceRecords<T>(string sql)
        {
            List<T> recordList = new List<T>();

            try
            {

                String connectionString = SalesforceCxn.SalesforceConnectionString();

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {

                    SalesforceCommand cmd = new SalesforceCommand(sql, connection);

                    SalesforceDataReader rdr = cmd.ExecuteReader();

                    recordList = SfdcReflectionUtil.PopulateListFromReader<T>(rdr);

                }

            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }

            return recordList;
        }
        public T GetJobFromSalesforce<T>(string jobId)
        {
            var salesforceBulkJob = Activator.CreateInstance<T>();


            try
            {
                String connectionString = SalesforceCxn.SalesforceConnectionString();
                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand("GetJob", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SalesforceParameter("@jobId", jobId));

                    SalesforceDataReader rdr = cmd.ExecuteReader();
                    salesforceBulkJob = SfdcReflectionUtil.PopulateBaseModelFromReader<T>(rdr);
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }

            return salesforceBulkJob;
        }
        public T CloseJobInSalesforce<T>(string jobId)
        {
            var jobDetails = Activator.CreateInstance<T>();
            try
            {
                String connectionString = SalesforceCxn.SalesforceConnectionString();

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand("CloseJob", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SalesforceParameter("@jobId", jobId));

                    SalesforceDataReader rdr = cmd.ExecuteReader();
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }


            return jobDetails;
        }


        public List<T> GetAllLeaseAssignmentsToVirtual<T>(string recordTypeId)
        {

            List<T> vLeaseList = new List<T>();

            try
            {

                String connectionString = SalesforceCxn.SalesforceConnectionString();

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand("SELECT  Lease__c.Name, Lease__c.Id, Account.Name AS AccountName, Account.Id AS AccountId FROM Lease__c, Account WHERE Account.Id = Lease__c.Contract_Customer__c AND (Account.RecordTypeId = " + recordTypeId + ") AND (Lease__c.IsDeleted = 'False')", connection);

                    SalesforceDataReader rdr = cmd.ExecuteReader();

                    vLeaseList = SfdcReflectionUtil.PopulateListFromReader<T>(rdr);
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }

            return vLeaseList;
        }

        public List<SfVirtualParentChildAccount> GetVirtualAccountParentChildList(string customerRecordTypeId, string virtualRecordTypeId)
        {
            List<SfVirtualParentChildAccount> sfVirtualParentAcctList = new List<SfVirtualParentChildAccount>();

            try
            {

                String connectionString = SalesforceCxn.SalesforceConnectionString();
                var sql =
                    "SELECT Id, Name, ParentId FROM Account WHERE ParentId <> '' AND Account.IsDeleted = 'False' AND Account.RecordTypeId in('" + customerRecordTypeId + "', '" + virtualRecordTypeId + "') ORDER BY Name ";
                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand(sql, connection);

                    SalesforceDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {

                        SfVirtualParentChildAccount sfVirtualParentChildAcct = new SfVirtualParentChildAccount();
                        sfVirtualParentChildAcct.AccountName = rdr["Name"].ToString();
                        sfVirtualParentChildAcct.SfIdAccount = rdr["Id"].ToString();
                        sfVirtualParentChildAcct.SfIdParent = rdr["ParentId"].ToString();

                        sfVirtualParentChildAcct = GetParentInfo(sfVirtualParentChildAcct);

                        sfVirtualParentAcctList.Add(sfVirtualParentChildAcct);

                    }

                    return sfVirtualParentAcctList;
                }
            }
            catch (Exception ex)
            {

                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }

        }

        public static SfVirtualParentChildAccount GetParentInfo(SfVirtualParentChildAccount childAcct)
        {
            try
            {

                String connectionString = SalesforceCxn.SalesforceConnectionString();

                using (SalesforceConnection connection = new SalesforceConnection(connectionString))
                {
                    SalesforceCommand cmd = new SalesforceCommand("SELECT Account.Name, Account.CCAN__c , Record_type_Name__c FROM Account Where Id = " + childAcct.SfIdParent, connection);

                    SalesforceDataReader rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {

                        childAcct.ParentAccountName = rdr["Name"].ToString();
                        if (!string.IsNullOrEmpty(rdr["CCAN__c"].ToString()))
                        {
                            childAcct.Ccan = Convert.ToDecimal(rdr["CCAN__c"].ToString());
                        }
                        childAcct.ParentRecordType = rdr["Record_type_Name__c"].ToString();
                    }

                    return childAcct;
                }
            }
            catch (Exception ex)
            {
                CfsLogging.LogFatal("Error occured Salesforce Data Loader", String.Format(MethodBase.GetCurrentMethod().Name + " method failed in SalesforceContext class") + "\n" + ex);
                throw;
            }

        }

    }
}
