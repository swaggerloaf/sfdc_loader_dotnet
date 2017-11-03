

using System.Collections.Generic;
using OracleOrm.Models;

namespace Cfs.Salesforce.Model.SfdcRecordType
{
    public class SfRecordTypes : BaseModelList<SfRecordType>
    {
        public override IEnumerable<SfRecordType> List { get; set; }
    }
}
