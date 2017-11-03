

using System;
using System.Data;
using sfdc_loader.Model.MetaDataAttribute;
namespace sfdc_loader.Model.SfdcRecordType
{

 
    public class SfRecordType 
    {

      
        [SfdcColumn("Id")]
        public String SfRecordTypeId { get; set; }

       
        [SfdcColumn("Name")]
        public String SfRecordTypeName { get; set; }

       
        [SfdcColumn("SobjectType")]
        public String SfObjectName { get; set; }


       
        public DateTime? AddDate { get; set; }

        
        public String AddUser { get; set; }

    }
}




