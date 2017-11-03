
using System.Collections.Generic;
using OracleOrm.Models;

namespace Cfs.Salesforce.Model.SfdcContact
{
    public class SfContacts : BaseModelList<SfContact>
    {
        public override IEnumerable<SfContact> List { get; set; }
    }
}
