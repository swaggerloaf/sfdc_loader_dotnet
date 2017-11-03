using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sfdc_loader.DataContext.SfdcContext
{
    public interface ISalesforce
    {
        T GetRecordType<T>(string recordTypeName, string sObjectType);
        T CreateJobForBatchInSalesforce<T>(string salesforceObjectName, string action, string concurrency);
        T SendBatchData<T>(string jobId, int numberOfRecords, string dataForBatch);
        T GetBatchFromSalesforce<T>(string batchId, string jobId);
      //  TT GetSalesforceBulkBatchResult<TT, T>(string jobId, string batchId)
      //      where TT : BaseModelList<T>
      //      where T : BaseModel;
        List<T> GetSalesforceRecords<T>(string sql);
        T GetJobFromSalesforce<T>(string jobId);
        T CloseJobInSalesforce<T>(string jobId);

     //   TT GetRecordTypesFromSalesforce<TT, T>()
     //       where TT : BaseModelList<T>
     //       where T : BaseModel;

        List<T> GetAccountsFromSalesforce<T>(string recordTypeId);

        List<T> GetAllLeaseAssignmentsToVirtual<T>(string recordTypeId);

        List<SfVirtualParentChildAccount> GetVirtualAccountParentChildList(string customerRecordTypeId, string virtualRecordTypeId);

    }
}
