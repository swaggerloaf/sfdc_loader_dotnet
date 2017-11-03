
namespace sfdc_loader.Model.MetaDataAttribute
{

    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public class SfdcColumn : System.Attribute
    {
        public string ColumnName { get; private set; }
        public bool DateDayToOrdinalConversion { get; private set; }
        public string ForeignKeyColumnName { get; set; }
        public bool IsPrimaryKey { get; set; }
        public SfdcColumn(string columnName, bool dateDayToOrinalConversion = false, bool isPrimaryKey = false, string foreignKeyColumnName = "")
        {
            this.ColumnName = columnName;
            this.DateDayToOrdinalConversion = dateDayToOrinalConversion;
            this.ForeignKeyColumnName = foreignKeyColumnName;
            this.IsPrimaryKey = isPrimaryKey;
        }
    }
}
