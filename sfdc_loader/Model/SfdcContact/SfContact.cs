using System;
using sfdc_loader.Model.MetaDataAttribute;


namespace Cfs.Salesforce.Model.SfdcContact
{

   
    public class SfContact 
    {

     
        [SfdcColumn("Cfs_Contact_Id__c")]
        public Decimal? ContactId { get; set; }


        
        [SfdcColumn("Ccan__c")]
        public Decimal? Ccan { get; set; }

       
        public String ContactName { get; set; }


       
        [SfdcColumn("Phone")]
        public String Phone { get; set; }

      
        [SfdcColumn("AccountId")]
        public String SfdcCustId { get; set; }

      
        [SfdcColumn("Id")]
        public String SfdcContactId { get; set; }

      
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
