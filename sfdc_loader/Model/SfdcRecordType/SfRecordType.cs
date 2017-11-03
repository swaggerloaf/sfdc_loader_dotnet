

using System;
using System.Data;
using Cfs.Salesforce.Model.MetaDataAttribute;
using Oracle.DataAccess.Client;
using OracleOrm.Models;
using OracleOrm.Models.MetaDataAttributes;

namespace Cfs.Salesforce.Model.SfdcRecordType
{

    [DbTableDefinition("SF_RECORD_TYPES")]
    [DbProcDefinition("SF_VIRTUAL.SF_RECORD_TYPES_INS",
    "SF_VIRTUAL.SF_RECORD_TYPES_UPD",
    "SF_VIRTUAL.SF_RECORD_TYPES_DEL",
    "SF_VIRTUAL.SF_RECORD_TYPES_GET")]
    public class SfRecordType : BaseModel
    {

        [DbInsertParam("outSF_RECORD_TYPE_ID", OracleDbType.Varchar2, 1, ParameterDirection.Input, size: 20)]
        [DbColumn("SF_RECORD_TYPE_ID", isPrimaryKey: true)]
        [SfdcColumn("Id")]
        public String SfRecordTypeId { get; set; }

        [DbInsertParam("inSF_RECORD_TYPE_NAME", OracleDbType.Varchar2, 2, size: 50)]
        [DbColumn("SF_RECORD_TYPE_NAME")]
        [SfdcColumn("Name")]
        public String SfRecordTypeName { get; set; }

        [DbInsertParam("inSF_OBJECT_NAME", OracleDbType.Varchar2, 3, size: 50)]
        [DbColumn("SF_OBJECT_NAME")]
        [SfdcColumn("SobjectType")]
        public String SfObjectName { get; set; }


        [DbColumn("ADD_DATE")]
        public DateTime? AddDate { get; set; }

        [DbInsertParam("inADD_USER", OracleDbType.Varchar2, 5, size: 150)]
        [DbColumn("ADD_USER")]
        public String AddUser { get; set; }

    }
}




