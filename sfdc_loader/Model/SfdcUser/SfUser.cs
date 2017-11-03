using System;
using System.Data;
using Cfs.Salesforce.Model.MetaDataAttribute;
using Oracle.DataAccess.Client;
using OracleOrm.Models;
using OracleOrm.Models.MetaDataAttributes;

namespace Cfs.Salesforce.Model.SfdcUser
{
    [DbTableDefinition("SF_USER")]
    [DbProcDefinition("CFS_SALESFORCE_LOADER.INSERT_SF_USER"
  )]
    public class SfUser : BaseModel
    {
        [DbInsertParam("outUSER_ID", OracleDbType.Decimal, 1, ParameterDirection.Output)]
        [DbColumn("USER_ID", isPrimaryKey: true)]
        public Decimal? UserId { get; set; }

        [DbInsertParam("inSFDC_USER_ID", OracleDbType.Varchar2, 2, size: 100)]
        [DbColumn("SFDC_USER_ID")]
        [SfdcColumn("ID")]
        public String SfdcUserId { get; set; }

        [DbInsertParam("inUSER_NAME", OracleDbType.Varchar2, 3, size: 80)]
        [DbColumn("USER_NAME")]
        [SfdcColumn("Username")]
        public String UserName { get; set; }

        [DbInsertParam("inLAST_NAME", OracleDbType.Varchar2, 4, size: 80)]
        [DbColumn("LAST_NAME")]
        [SfdcColumn("LastName")]
        public String LastName { get; set; }

        [DbInsertParam("inFIRST_NAME", OracleDbType.Varchar2, 5, size: 40)]
        [DbColumn("FIRST_NAME")]
        [SfdcColumn("FirstName")]
        public String FirstName { get; set; }

        [DbInsertParam("inFULL_NAME", OracleDbType.Varchar2, 6, size: 121)]
        [DbColumn("FULL_NAME")]
        [SfdcColumn("Name")]
        public String FullName { get; set; }

        [DbInsertParam("inCOMPANY_NAME", OracleDbType.Varchar2, 7, size: 80)]
        [DbColumn("COMPANY_NAME")]
        [SfdcColumn("CompanyName")]
        public String CompanyName { get; set; }

        [DbInsertParam("inDIVISION", OracleDbType.Varchar2, 8, size: 80)]
        [DbColumn("DIVISION")]
        [SfdcColumn("Division")]
        public String Division { get; set; }

        [DbInsertParam("inDEPARTMENT", OracleDbType.Varchar2, 9, size: 80)]
        [DbColumn("DEPARTMENT")]
        [SfdcColumn("Department")]
        public String Department { get; set; }

        [DbInsertParam("inTITLE", OracleDbType.Varchar2, 10, size: 80)]
        [DbColumn("TITLE")]
        [SfdcColumn("Title")]
        public String Title { get; set; }

        [DbInsertParam("inSTREET", OracleDbType.Varchar2, 11, size: 255)]
        [DbColumn("STREET")]
        [SfdcColumn("Street")]
        public String Street { get; set; }

        [DbInsertParam("inCITY", OracleDbType.Varchar2, 12, size: 40)]
        [DbColumn("CITY")]
        [SfdcColumn("City")]
        public String City { get; set; }

        [DbInsertParam("inSTATE", OracleDbType.Varchar2, 13, size: 80)]
        [DbColumn("STATE")]
        [SfdcColumn("State")]
        public String State { get; set; }

        [DbInsertParam("inPOSTAL_CODE", OracleDbType.Varchar2, 14, size: 20)]
        [DbColumn("POSTAL_CODE")]
        [SfdcColumn("PostalCode")]
        public String PostalCode { get; set; }

        [DbInsertParam("inCOUNTRY", OracleDbType.Varchar2, 15, size: 80)]
        [DbColumn("COUNTRY")]
        [SfdcColumn("Country")]
        public String Country { get; set; }

        [DbInsertParam("inEMAIL", OracleDbType.Varchar2, 16, size: 128)]
        [DbColumn("EMAIL")]
        [SfdcColumn("Email")]
        public String Email { get; set; }

        [DbInsertParam("inPHONE", OracleDbType.Varchar2, 17, size: 40)]
        [DbColumn("PHONE")]
        [SfdcColumn("Phone")]
        public String Phone { get; set; }

        [DbInsertParam("inEMPLOYEE_NO", OracleDbType.Varchar2, 18, size: 20)]
        [DbColumn("EMPLOYEE_NO")]
        [SfdcColumn("EmployeeNumber")]
        public String EmployeeNo { get; set; }

        [DbInsertParam("inIS_ACTIVE", OracleDbType.Varchar2, 20, size: 10)]
        [DbColumn("IS_ACTIVE")]
        [SfdcColumn("IsActive")]
        public string IsActive { get; set; }

        [DbColumn("ADD_DATE")]
        public DateTime? AddDate { get; set; }

        [DbInsertParam("inADD_USER", OracleDbType.Varchar2, 22, size: 50)]
        [DbColumn("ADD_USER")]
        public String AddUser { get; set; }

        [DbColumn("UPDATE_DATE")]
        public DateTime? UpdateDate { get; set; }

        [DbColumn("UPDATE_USER")]
        public String UpdateUser { get; set; }

    }


}
