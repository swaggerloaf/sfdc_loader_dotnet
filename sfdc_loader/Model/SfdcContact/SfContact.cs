using System;
using Cfs.Salesforce.Model.MetaDataAttribute;
using Oracle.DataAccess.Client;
using OracleOrm.Models;
using OracleOrm.Models.MetaDataAttributes;

namespace Cfs.Salesforce.Model.SfdcContact
{

    [DbTableDefinition("SF_CONTACT")]
    [DbProcDefinition(updateProcName: "CFS_SALESFORCE_LOADER.UPDATE_SF_CONTACT")]
    public class SfContact : BaseModel
    {

        [DbUpdateParam("inCONTACT_ID", OracleDbType.Decimal, 1)]
        [DbColumn("CONTACT_ID", isPrimaryKey: true)]
        [SfdcColumn("Cfs_Contact_Id__c")]
        public Decimal? ContactId { get; set; }


        [DbUpdateParam("inCCAN", OracleDbType.Decimal, 2)]
        [DbColumn("CCAN")]
        [SfdcColumn("Ccan__c")]
        public Decimal? Ccan { get; set; }

        [DbUpdateParam("inCONTACT_NAME", OracleDbType.Varchar2, 3, size: 30)]
        [DbColumn("CONTACT_NAME")]
        public String ContactName { get; set; }


        [DbUpdateParam("inPHONE", OracleDbType.Varchar2, 4, size: 30)]
        [DbColumn("PHONE")]
        [SfdcColumn("Phone")]
        public String Phone { get; set; }

        [DbUpdateParam("inSFDC_CUST_ID", OracleDbType.Varchar2, 5, size: 100)]
        [DbColumn("SFDC_CUST_ID")]
        [SfdcColumn("AccountId")]
        public String SfdcCustId { get; set; }

        [DbUpdateParam("inSFDC_CONTACT_ID", OracleDbType.Varchar2, 6, size: 100)]
        [DbColumn("SFDC_CONTACT_ID")]
        [SfdcColumn("Id")]
        public String SfdcContactId { get; set; }

        [DbUpdateParam("inNEED_SFDC_UPDATE", OracleDbType.Varchar2, 7, size: 1)]
        [DbColumn("NEED_SFDC_UPDATE")]
        public String NeedSfdcUpdate { get; set; }


        [DbColumn("ADD_DATE")]
        public DateTime? AddDate { get; set; }


        [DbColumn("ADD_USER")]
        public String AddUser { get; set; }


        [DbColumn("UPDATE_DATE")]
        public DateTime? UpdateDate { get; set; }

        [DbUpdateParam("inUPDATE_USER", OracleDbType.Varchar2, 11, size: 50)]
        [DbColumn("UPDATE_USER")]
        public String UpdateUser { get; set; }

    }


}
