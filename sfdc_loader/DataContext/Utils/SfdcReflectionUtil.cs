using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using sfdc_loader.Model.MetaDataAttribute;
using System.Linq.Dynamic;

namespace sfdc_loader.DataContext.Utils
{
    public class SfdcReflectionUtil
    {
        /// <summary>
        /// Populates properties of a class T (Model of type [BaseModel]) dynamically. (See the usage of "DBColumn" Attribute)
        /// Properties will be populated by looking DBColumn Attribute or by matching the name of property with DataReader's Column name.
        /// </summary>
        /// <typeparam name="T">Class which inherits [BaseModel]</typeparam>
        /// <param name="reader">Reader (IDataReader) Datasource</param>
        /// <param name="baseDataRequestParameter" />
        /// <param name="flagThrowExceptionIfAny">
        /// Flag to indicate if method should throw an exception on failure and stop working from that point or continue working.
        /// Default = FALSE
        /// </param>
        /// <returns></returns>
        public static List<T> PopulateListFromReader<T>(IDataReader reader, BaseQueryableRequestParameter baseDataRequestParameter = null, bool flagThrowExceptionIfAny = false)
        {
            //var _listType = typeof(List<>);
            //var _constructedListType = _listType.MakeGenericType(typeof(T));
            var _listCollectionOfAcutalInstanceType = new List<T>(); //(List<T>)Activator.CreateInstance(_constructedListType);
            DateTime _startTime = DateTime.Now;
            DateTime _intermediateProcessStartTime = DateTime.Now;

            //Populating List<T>
            try
            {
              //  if (typeof(T).BaseType != typeof(BaseModel) && typeof(T).IsSubclassOf(typeof(BaseModel)) == false)
              //      throw new Exception("Model Type is not of BaseModel Type.");
                int _rowCnt = 0;
                while (reader.Read())
                {
                    _rowCnt++;
                    T _actualInstance;
                    if (_rowCnt == 1) //only for 1st row
                        _actualInstance = CreateInstanceOfGivenModelType<T>(reader, baseDataRequestParameter, flagThrowExceptionIfAny);
                    else
                        _actualInstance = CreateInstanceOfGivenModelType<T>(reader, null, flagThrowExceptionIfAny);

                    //_listCollectionOfAcutalInstanceType.Add((T) Convert.ChangeType(_actualInstance,(typeof(T))));
                    _listCollectionOfAcutalInstanceType.Add(_actualInstance);
                }

             
               string.Format("Process Took [{1}] Msec.\r\nProcess Detail: PopulateListFromReader<{0}>", typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));

            }
            catch (Exception ex)
            {
                StringBuilder _sbError = new StringBuilder();
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception Severity: Critical"));
                _sbError.AppendLine(string.Format("Type: {0}", MethodBase.GetCurrentMethod().GetType().FullName));
                _sbError.AppendLine(string.Format("Method: {0}", MethodBase.GetCurrentMethod().Name));
                _sbError.AppendLine(string.Format("Exception Handling Message: Error occurred while creating List<{0}>", typeof(T)));
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception: {0}", ex));

                //Handle it later
                if (flagThrowExceptionIfAny)
                    throw;
                else
                  //  CfsLogging.LogFatal(string.Format("Error occurred while creating List<{0}> ", typeof(T)), _sbError.ToString());
            }
            finally
            {
                //#if DEBUG
                //CreateModelFile<T>(reader);
                //#endif
                reader.Close();
            }


            //Filtering Collection
            try
            {
                if (_listCollectionOfAcutalInstanceType.Count > 0 && baseDataRequestParameter != null)
                {
                    var _queryableList = _listCollectionOfAcutalInstanceType.AsQueryable();

                    //FILTER
                    if (_queryableList != null && _queryableList.Any() && baseDataRequestParameter.FilterOptions != null)
                    {
                        //Filter Numeric Range Options
                        StringBuilder _mainFilterOptionCondition = new StringBuilder();
                        if (baseDataRequestParameter.FilterOptions.FilterNumericRanges != null && baseDataRequestParameter.FilterOptions.FilterNumericRanges.Count > 0)
                        {
                            _intermediateProcessStartTime = DateTime.Now;
                            int _filterIndex = 0;
                            foreach (var _filter in baseDataRequestParameter.FilterOptions.FilterNumericRanges)
                            {
                                _filterIndex++;
                                //Throw Exception, if parameter is not of type NUMBER
                                if (IsNumber(_filter.RangeStart) == false || IsNumber(_filter.RangeEnd) == false)
                                    throw new Exception(string.Format("Filter Operation [FILTER NUMERIC RANGES] found invalid filter value range [{0} - {1}] for field [{2}] of type [{3}]", _filter.RangeStart, _filter.RangeEnd, _filter.Field, "Number"));

                                if (_filterIndex < baseDataRequestParameter.FilterOptions.FilterNumericRanges.Count)
                                    _mainFilterOptionCondition.AppendFormat("({0} >= {1} && {0} <= {2} ) && ", _filter.Field, _filter.RangeStart, _filter.RangeEnd);
                                else
                                    _mainFilterOptionCondition.AppendFormat("({0} >= {1} && {0} <= {2} )", _filter.Field, _filter.RangeStart, _filter.RangeEnd);
                            }

                            //Executing Main Contion 
                            //*** NOTE: Since there is an AND contion between filter options, There is no need to excute the contion at the end on whole list
                            if (_mainFilterOptionCondition.Length > 0)
                            {
                              //  CfsLogging.LogVerboseInfo("Filter Condition", string.Format("[FILTER NUMERIC RANGES - Condition]: {0}", _mainFilterOptionCondition));
                                _queryableList = _queryableList.Where(_mainFilterOptionCondition.ToString());
                            }

                            _mainFilterOptionCondition.Clear();

                          //  CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [FILTER NUMERIC RANGES - PopulateListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                                string.Format("Process Took [{1}] Msec.\r\nProcess Detail: PopulateListFromReader<{0}>", typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                        }

                        //Filter Date Range Options 
                        if (_queryableList != null && _queryableList.Any() && baseDataRequestParameter.FilterOptions.FilterDateRanges != null && baseDataRequestParameter.FilterOptions.FilterDateRanges.Count > 0)
                        {
                            _intermediateProcessStartTime = DateTime.Now;
                            int _filterIndex = 0;
                            foreach (var _filter in baseDataRequestParameter.FilterOptions.FilterDateRanges)
                            {
                                if (_filterIndex == 0 && _mainFilterOptionCondition.Length > 0)
                                    _mainFilterOptionCondition.Append(" && ");
                                _filterIndex++;

                                DateTime _tempVal;
                                //Throw Exception, if parameter is not of type DATETIME
                                if (DateTime.TryParse(_filter.RangeStart.ToString(), out _tempVal) == false ||
                                    DateTime.TryParse(_filter.RangeEnd.ToString(), out _tempVal) == false)
                                    throw new Exception(string.Format("Filter Operation [FILTER DATE RANGES] found invalid filter value range [{0} - {1}] for field [{2}] of type [{3}]", _filter.RangeStart, _filter.RangeEnd, _filter.Field, "System.DateTime"));

                                if (_filterIndex < baseDataRequestParameter.FilterOptions.FilterDateRanges.Count)
                                    _mainFilterOptionCondition.AppendFormat("({0} >= DateTime.parse(\"{1}\") && {0} <= DateTime.parse(\"{2}\") ) && ", _filter.Field, _filter.RangeStart, _filter.RangeEnd);
                                else
                                    _mainFilterOptionCondition.AppendFormat("({0} >= DateTime.parse(\"{1}\") && {0} <= DateTime.parse(\"{2}\") )", _filter.Field, _filter.RangeStart, _filter.RangeEnd);

                            }

                            //Executing Main Contion
                            if (_mainFilterOptionCondition.Length > 0)
                            {
                              //  CfsLogging.LogVerboseInfo("Filter Condition", string.Format("[FILTER DATE RANGES - Condition]: {0}", _mainFilterOptionCondition));
                                _queryableList = _queryableList.Where(_mainFilterOptionCondition.ToString());
                            }
                            _mainFilterOptionCondition.Clear();

                        //    CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [FILTER DATE RANGES - PopulateListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                                string.Format("Process Took [{1}] Msec.\r\nProcess Detail: PopulateListFromReader<{0}>", typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                        }

                        //Filter List Options
                        if (_queryableList != null && _queryableList.Any() && baseDataRequestParameter.FilterOptions.FilterLists != null && baseDataRequestParameter.FilterOptions.FilterLists.Count > 0)
                        {
                            _intermediateProcessStartTime = DateTime.Now;
                            int _filterIndex = 0;
                            foreach (var _filter in baseDataRequestParameter.FilterOptions.FilterLists)
                            {
                                if (_filterIndex == 0 && _mainFilterOptionCondition.Length > 0)
                                    _mainFilterOptionCondition.Append(" && ");

                                StringBuilder _fieldCondition = new StringBuilder();
                                _filterIndex++;
                                int _filterValueIndex = 0;
                                foreach (var _filterValue in _filter.FilterValues)
                                {
                                    _filterValueIndex++;
                                    if (_filter.FieldType == Type.GetType("System.String"))
                                    {
                                        if (_filterValueIndex < _filter.FilterValues.Count)
                                            _fieldCondition.AppendFormat("{0}.ToString().ToUpper() == \"{1}\" || ", _filter.Field, _filterValue.ToUpper());
                                        else
                                            _fieldCondition.AppendFormat("{0}.ToString().ToUpper() == \"{1}\" ", _filter.Field, _filterValue.ToUpper());
                                    }
                                    else if (_filter.FieldType == typeof(DateTime?) || _filter.FieldType == typeof(DateTime))
                                    {
                                        DateTime _tempVal;
                                        //Throw Exception, if parameter is not of type DATETIME
                                        if (DateTime.TryParse(_filterValue, out _tempVal) == false)
                                            throw new Exception(string.Format("Filter Operation [FILTER LISTS] found invalid filter value [{0}] for field [{1}] of type [{2}]", _filterValue, _filter.Field, _filter.FieldType));

                                        if (_filterValueIndex < _filter.FilterValues.Count)
                                            _fieldCondition.AppendFormat("{0} == DateTime.parse(\"{1}\") || ", _filter.Field, _filterValue);
                                        else
                                            _fieldCondition.AppendFormat("{0} == DateTime.parse(\"{1}\") ", _filter.Field, _filterValue);
                                    }
                                    else
                                    {
                                        if (_filterValueIndex < _filter.FilterValues.Count)
                                            _fieldCondition.AppendFormat("{0} == {1} || ", _filter.Field, _filterValue.ToUpper());
                                        else
                                            _fieldCondition.AppendFormat("{0} == {1} ", _filter.Field, _filterValue.ToUpper());
                                    }

                                }
                                if (_filterIndex < baseDataRequestParameter.FilterOptions.FilterLists.Count)
                                    _mainFilterOptionCondition.AppendFormat("({0}) && ", _fieldCondition);
                                else
                                    _mainFilterOptionCondition.AppendFormat("({0})", _fieldCondition);

                            }

                            //Executing Main Contion
                            if (_mainFilterOptionCondition.Length > 0)
                            {
                           //     CfsLogging.LogVerboseInfo("Filter Condition", string.Format("[FILTER LISTS - Condition]: {0}", _mainFilterOptionCondition));
                                _queryableList = _queryableList.Where(_mainFilterOptionCondition.ToString());
                          //      CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [FILTER LISTS - PopulateListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                                    string.Format("Process Took [{1}] Msec.\r\nProcess Detail: PopulateListFromReader<{0}>", typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                            }
                        }
                    }

                    //SORTING
                    if (_queryableList != null && _queryableList.Any() && baseDataRequestParameter.SortOptions != null)
                    {
                        _intermediateProcessStartTime = DateTime.Now;
                        _queryableList = _queryableList.OrderBy(string.Format("{0} {1}", baseDataRequestParameter.SortOptions.SortField, baseDataRequestParameter.SortOptions.SortDirection.ToString().ToUpper()));
                     //   CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [SORT OPTIONS - PopulateListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                            string.Format("Process Took [{1}] Msec.\r\nProcess Detail: PopulateListFromReader<{0}>", typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                    }

                    //PAGING
                    if (baseDataRequestParameter.PageOptions != null)
                    {
                        _intermediateProcessStartTime = DateTime.Now;
                        if (baseDataRequestParameter.PageOptions.Take > 0)
                        {
                            if (_queryableList != null && _queryableList.Any())
                            {
                                _queryableList = _queryableList.Skip(baseDataRequestParameter.PageOptions.Skip).Take(baseDataRequestParameter.PageOptions.Take);
                           //     CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [PAGE OPTIONS - PopulateListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                                    string.Format("Process Took [{1}] Msec.\r\nProcess Detail: PopulateListFromReader<{0}>", typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                            }
                        }
                    }

                    if (_queryableList != null)
                        _listCollectionOfAcutalInstanceType = _queryableList.ToList(); //.OfType<T>().ToList();
                    else
                        _listCollectionOfAcutalInstanceType = null;
                }

            }
            catch (Exception ex)
            {
                StringBuilder _sbError = new StringBuilder();
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception Severity: Critical"));
                _sbError.AppendLine(string.Format("Type: {0}", MethodBase.GetCurrentMethod().GetType().FullName));
                _sbError.AppendLine(string.Format("Method: {0}", MethodBase.GetCurrentMethod().Name));
                _sbError.AppendLine(string.Format("Exception Handling Message: Error occurred while filtering/sorting/paging IList<{0}>", typeof(T)));
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception: {0}", ex));

                //Handle it later
                if (flagThrowExceptionIfAny)
                    throw;
                else
                  //  CfsLogging.LogFatal(string.Format("Error occurred while filtering/sorting/paging IList<{0}> ", typeof(T)), _sbError.ToString());
            }
            finally
            {
              //  CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [PopulateListFromReader] Took [{0}] Msec.", (DateTime.Now - _startTime).TotalMilliseconds), string.Format("Process Took [{1}] Msec.\r\nProcess Detail: PopulateListFromReader<{0}>", typeof(T), (DateTime.Now - _startTime).TotalMilliseconds));
            }
            return _listCollectionOfAcutalInstanceType;
        }

        /// <summary>
        /// Populates properties of a class TT (Model of type [BaseModelList]) dynamically. (Also see the usage of "DBColumn" Attribute)
        /// Properties will be populated by looking DBColumn Attribute or by matching the name of property with DataReader's Column name.
        /// </summary>
        /// <typeparam name="TT">Class which inherits [BaseModelList]</typeparam>
        /// <typeparam name="T">Class which inherits [BaseModel]</typeparam>
        /// <param name="reader">Reader (IDataReader) DataSource</param>
        /// <param name="baseDataRequestParameter">Object of a class which is a sub class of [BaseRequestParameter] class.</param>
        /// <param name="flagThrowExceptionIfAny">
        /// Flag to indicate if method should throw an exception on failure and stop working from that point or continue working.
        /// Default = FALSE
        /// </param>
        /// <returns></returns>
        public static TT PopulateBaseModelListFromReader<TT, T>(IDataReader reader, BaseQueryableRequestParameter baseDataRequestParameter = null, bool flagThrowExceptionIfAny = false)
           // where TT : BaseModelList<T>
        {
            DateTime _startTime = DateTime.Now;
            DateTime _intermediateProcessStartTime = DateTime.Now;
            //var _listType = typeof(List<>);
            //var _constructedListType = _listType.MakeGenericType(typeof(T));
            var _listCollectionOfAcutalInstanceType = new List<T>(); //(IList)Activator.CreateInstance(_constructedListType);
            TT _actualInstanceOfBaseModelList = null;
            //Populating List<T>
            try
            {
              //  if (typeof(TT).BaseType != typeof(BaseModelList<T>))
             //       throw new Exception("Model Type is not of BaseModelist Type.");
             //   if (typeof(T).BaseType != typeof(BaseModel) && typeof(T).IsSubclassOf(typeof(BaseModel)) == false)
             //       throw new Exception("Model Type is not of BaseModel Type.");
                int _rowCnt = 0;
                while (reader.Read())
                {
                    _rowCnt++;
                    T _actualInstance;
                    if (_rowCnt == 1) //only for 1st row
                        _actualInstance = CreateInstanceOfGivenModelType<T>(reader, baseDataRequestParameter, flagThrowExceptionIfAny);
                    else
                        _actualInstance = CreateInstanceOfGivenModelType<T>(reader, null, flagThrowExceptionIfAny);

                    _listCollectionOfAcutalInstanceType.Add(_actualInstance);
                }
            //    CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [LOOP READER ROWS ({1}) - PopulateBaseModelListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds, _rowCnt),
                    string.Format("Process Took [{2}] Msec.\r\nProcess Detail: PopulateBaseModelListFromReader<{0},{1}>", typeof(TT), typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));

             //   _actualInstanceOfBaseModelList = Activator.CreateInstance(typeof(TT)) as TT;

                //Default before Paging & Filtering
                _actualInstanceOfBaseModelList.RecordCount = _rowCnt;
                if (_actualInstanceOfBaseModelList.RecordCount > 0)
                {
                    _actualInstanceOfBaseModelList.List = _listCollectionOfAcutalInstanceType; //.OfType<T>().ToList();
                    _actualInstanceOfBaseModelList.PageCount = 1;
                    _actualInstanceOfBaseModelList.PageSize = _rowCnt;
                    _actualInstanceOfBaseModelList.PageCurrent = 1;
                }

            }
            catch (Exception ex)
            {

                StringBuilder _sbError = new StringBuilder();
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception Severity: Critical"));
                _sbError.AppendLine(string.Format("Type: {0}", MethodBase.GetCurrentMethod().GetType().FullName));
                _sbError.AppendLine(string.Format("Method: {0}", MethodBase.GetCurrentMethod().Name));
                _sbError.AppendLine(string.Format("Exception Handling Message: Error occurred while creating list collection for {0} : IBaseModelList<{1}>", typeof(TT), typeof(T)));
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception: {0}", ex));

                //Handle it later
                if (flagThrowExceptionIfAny)
                    throw;
                else
              //      CfsLogging.LogFatal(string.Format("Error occurred while creating IList<{0}> ", typeof(T)), _sbError.ToString());
            }
            finally
            {
                //#if DEBUG
                //DataTable _readerSchemaTbl = reader.GetSchemaTable();
                ////_readerSchemaTbl.WriteXml(string.Format("c:\\temp\\{0}.xml",acutalInstanceType));
                //CreateModelFile<T>(reader);
                //#endif
                reader.Close();
            }

            //Execute below code only if baseDataRequestParameter != null
            //Filtering Collection
            try
            {
                if (_listCollectionOfAcutalInstanceType.Count > 0 && baseDataRequestParameter != null)
                {
                    var _queryableList = _listCollectionOfAcutalInstanceType.AsQueryable();

                    //FILTER
                    if (_queryableList != null && _queryableList.Any() && baseDataRequestParameter.FilterOptions != null)
                    {
                        //Filter Numeric Range Options
                        StringBuilder _mainFilterOptionCondition = new StringBuilder();
                        if (baseDataRequestParameter.FilterOptions.FilterNumericRanges != null && baseDataRequestParameter.FilterOptions.FilterNumericRanges.Count > 0)
                        {
                            _intermediateProcessStartTime = DateTime.Now;
                            int _filterIndex = 0;
                            foreach (var _filter in baseDataRequestParameter.FilterOptions.FilterNumericRanges)
                            {
                                _filterIndex++;
                                //Throw Exception, if parameter is not of type NUMBER
                                if (IsNumber(_filter.RangeStart) == false || IsNumber(_filter.RangeEnd) == false)
                                    throw new Exception(string.Format("Filter Operation [FILTER NUMERIC RANGES] found invalid filter value range [{0} - {1}] for field [{2}] of type [{3}]", _filter.RangeStart, _filter.RangeEnd, _filter.Field, "Number"));

                                if (_filterIndex < baseDataRequestParameter.FilterOptions.FilterNumericRanges.Count)
                                    _mainFilterOptionCondition.AppendFormat("({0} >= {1} && {0} <= {2} ) && ", _filter.Field, _filter.RangeStart, _filter.RangeEnd);
                                else
                                    _mainFilterOptionCondition.AppendFormat("({0} >= {1} && {0} <= {2} )", _filter.Field, _filter.RangeStart, _filter.RangeEnd);
                            }

                            //Executing Main Contion 
                            //*** NOTE: Since there is an AND contion between filter options, There is no need to excute the contion at the end on whole list
                            if (_mainFilterOptionCondition.Length > 0)
                            {
                  //              CfsLogging.LogVerboseInfo("Filter Condition", string.Format("[FILTER NUMERIC RANGES - Condition]: {0}", _mainFilterOptionCondition));
                                _queryableList = _queryableList.Where(_mainFilterOptionCondition.ToString());
                            }

                            _mainFilterOptionCondition.Clear();

                //            CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [FILTER NUMERIC RANGES - PopulateBaseModelListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                                string.Format("Process Took [{2}] Msec.\r\nProcess Detail: PopulateBaseModelListFromReader<{0},{1}>", typeof(TT), typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                        }

                        //Filter Date Range Options 
                        if (_queryableList != null && _queryableList.Any() && baseDataRequestParameter.FilterOptions.FilterDateRanges != null && baseDataRequestParameter.FilterOptions.FilterDateRanges.Count > 0)
                        {
                            _intermediateProcessStartTime = DateTime.Now;
                            int _filterIndex = 0;
                            foreach (var _filter in baseDataRequestParameter.FilterOptions.FilterDateRanges)
                            {
                                if (_filterIndex == 0 && _mainFilterOptionCondition.Length > 0)
                                    _mainFilterOptionCondition.Append(" && ");
                                _filterIndex++;

                                DateTime _tempVal;
                                //Throw Exception, if parameter is not of type DATETIME
                                if (DateTime.TryParse(_filter.RangeStart.ToString(), out _tempVal) == false ||
                                    DateTime.TryParse(_filter.RangeEnd.ToString(), out _tempVal) == false)
                                    throw new Exception(string.Format("Filter Operation [FILTER DATE RANGES] found invalid filter value range [{0} - {1}] for field [{2}] of type [{3}]", _filter.RangeStart, _filter.RangeEnd, _filter.Field, "System.DateTime"));

                                if (_filterIndex < baseDataRequestParameter.FilterOptions.FilterDateRanges.Count)
                                    _mainFilterOptionCondition.AppendFormat("({0} >= DateTime.parse(\"{1}\") && {0} <= DateTime.parse(\"{2}\") ) && ", _filter.Field, _filter.RangeStart, _filter.RangeEnd);
                                else
                                    _mainFilterOptionCondition.AppendFormat("({0} >= DateTime.parse(\"{1}\") && {0} <= DateTime.parse(\"{2}\") )", _filter.Field, _filter.RangeStart, _filter.RangeEnd);

                            }

                            //Executing Main Contion
                            if (_mainFilterOptionCondition.Length > 0)
                            {
                   //             CfsLogging.LogVerboseInfo("Filter Condition", string.Format("[FILTER DATE RANGES - Condition]: {0}", _mainFilterOptionCondition));
                                _queryableList = _queryableList.Where(_mainFilterOptionCondition.ToString());
                            }

                            _mainFilterOptionCondition.Clear();

                   //         CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [FILTER DATE RANGES - PopulateBaseModelListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                                string.Format("Process Took [{2}] Msec.\r\nProcess Detail: PopulateBaseModelListFromReader<{0},{1}>", typeof(TT), typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                        }

                        //Filter List Options
                        if (_queryableList != null && _queryableList.Any() && baseDataRequestParameter.FilterOptions.FilterLists != null && baseDataRequestParameter.FilterOptions.FilterLists.Count > 0)
                        {
                            _intermediateProcessStartTime = DateTime.Now;
                            int _filterIndex = 0;
                            foreach (var _filter in baseDataRequestParameter.FilterOptions.FilterLists)
                            {
                                if (_filterIndex == 0 && _mainFilterOptionCondition.Length > 0)
                                    _mainFilterOptionCondition.Append(" && ");

                                StringBuilder _fieldCondition = new StringBuilder();
                                _filterIndex++;
                                int _filterValueIndex = 0;
                                foreach (var _filterValue in _filter.FilterValues)
                                {
                                    _filterValueIndex++;
                                    if (_filter.FieldType == Type.GetType("System.String"))
                                    {
                                        if (_filterValueIndex < _filter.FilterValues.Count)
                                            _fieldCondition.AppendFormat("{0}.ToString().ToUpper() == \"{1}\" || ", _filter.Field, _filterValue.ToUpper());
                                        else
                                            _fieldCondition.AppendFormat("{0}.ToString().ToUpper() == \"{1}\" ", _filter.Field, _filterValue.ToUpper());
                                    }
                                    else if (_filter.FieldType == typeof(DateTime?) || _filter.FieldType == typeof(DateTime))
                                    {
                                        DateTime _tempVal;
                                        //Throw Exception, if parameter is not of type DATETIME
                                        if (DateTime.TryParse(_filterValue, out _tempVal) == false)
                                            throw new Exception(string.Format("Filter Operation [FILTER LISTS] found invalid filter value [{0}] for field [{1}] of type [{2}]", _filterValue, _filter.Field, _filter.FieldType));

                                        if (_filterValueIndex < _filter.FilterValues.Count)
                                            _fieldCondition.AppendFormat("{0} == DateTime.parse(\"{1}\") || ", _filter.Field, _filterValue);
                                        else
                                            _fieldCondition.AppendFormat("{0} == DateTime.parse(\"{1}\") ", _filter.Field, _filterValue);
                                    }
                                    else
                                    {
                                        if (_filterValueIndex < _filter.FilterValues.Count)
                                            _fieldCondition.AppendFormat("{0} == {1} || ", _filter.Field, _filterValue.ToUpper());
                                        else
                                            _fieldCondition.AppendFormat("{0} == {1} ", _filter.Field, _filterValue.ToUpper());
                                    }

                                }
                                if (_filterIndex < baseDataRequestParameter.FilterOptions.FilterLists.Count)
                                    _mainFilterOptionCondition.AppendFormat("({0}) && ", _fieldCondition);
                                else
                                    _mainFilterOptionCondition.AppendFormat("({0})", _fieldCondition);

                            }

                            //Executing Main Contion
                            if (_mainFilterOptionCondition.Length > 0)
                            {
                      //          CfsLogging.LogVerboseInfo("Filter Condition", string.Format("[FILTER LISTS - Condition]: {0}", _mainFilterOptionCondition));
                                _queryableList = _queryableList.Where(_mainFilterOptionCondition.ToString());
                    //            CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [FILTER LISTS - PopulateBaseModelListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                                    string.Format("Process Took [{2}] Msec.\r\nProcess Detail: PopulateBaseModelListFromReader<{0},{1}>", typeof(TT), typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                            }
                        }

                        //Reset Paging Model after the Filter
                        if (_queryableList != null)
                        {
                            _actualInstanceOfBaseModelList.RecordCount = _queryableList.Count();
                            _actualInstanceOfBaseModelList.PageCount = 1;
                            _actualInstanceOfBaseModelList.PageSize = _actualInstanceOfBaseModelList.RecordCount;
                            _actualInstanceOfBaseModelList.PageCurrent = 1;
                        //    CfsLogging.LogVerboseInfo(string.Format("Resetting Paging Model"), string.Format("Queryable List != [null] RecordCount = [{0}]", _actualInstanceOfBaseModelList.RecordCount));
                        }
                        else
                        {
                            _actualInstanceOfBaseModelList.RecordCount = 0;
                            _actualInstanceOfBaseModelList.PageCount = 0;
                            _actualInstanceOfBaseModelList.PageSize = _actualInstanceOfBaseModelList.RecordCount;
                            _actualInstanceOfBaseModelList.PageCurrent = 1;
                         //   CfsLogging.LogVerboseInfo(string.Format("Resetting Paging Model"), string.Format("Queryable List == [null] RecordCount = [{0}]", _actualInstanceOfBaseModelList.RecordCount));
                        }
                    }

                    //SORTING
                    if (_queryableList != null && _queryableList.Any() && baseDataRequestParameter.SortOptions != null)
                    {
                        _intermediateProcessStartTime = DateTime.Now;
                        _queryableList = _queryableList.OrderBy(string.Format("{0} {1}", baseDataRequestParameter.SortOptions.SortField, baseDataRequestParameter.SortOptions.SortDirection.ToString().ToUpper()));
                    //    CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [SORT OPTIONS - PopulateBaseModelListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                            string.Format("Process Took [{2}] Msec.\r\nProcess Detail: PopulateBaseModelListFromReader<{0},{1}>", typeof(TT), typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                    }

                    //PAGING
                    if (baseDataRequestParameter.PageOptions != null)
                    {
                        _intermediateProcessStartTime = DateTime.Now;
                        if (baseDataRequestParameter.PageOptions.Take > 0)
                        {
                            if (_queryableList != null && _queryableList.Any())
                                _queryableList = _queryableList.Skip(baseDataRequestParameter.PageOptions.Skip).Take(baseDataRequestParameter.PageOptions.Take);

                            if (_actualInstanceOfBaseModelList != null)
                            {
                                _actualInstanceOfBaseModelList.PageCount = (_actualInstanceOfBaseModelList.RecordCount / baseDataRequestParameter.PageOptions.Take);
                                if ((_actualInstanceOfBaseModelList.RecordCount % baseDataRequestParameter.PageOptions.Take) > 0)
                                    _actualInstanceOfBaseModelList.PageCount++;
                                _actualInstanceOfBaseModelList.PageSize = baseDataRequestParameter.PageOptions.Take;
                                _actualInstanceOfBaseModelList.PageCurrent = (baseDataRequestParameter.PageOptions.Skip / baseDataRequestParameter.PageOptions.Take) + 1;
                            }
                    //        CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [PAGE OPTIONS - PopulateBaseModelListFromReader] Took [{0}] Msec.", (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds),
                                string.Format("Process Took [{2}] Msec.\r\nProcess Detail: PopulateBaseModelListFromReader<{0},{1}>", typeof(TT), typeof(T), (DateTime.Now - _intermediateProcessStartTime).TotalMilliseconds));
                        }
                    }

                    if (_queryableList != null)
                        _actualInstanceOfBaseModelList.List = _queryableList; //.OfType<T>().ToList();
                    else
                        _actualInstanceOfBaseModelList.List = null;
                }

            }
            catch (Exception ex)
            {
                //_actualInstanceOfBaseModelList.List = null;
                StringBuilder _sbError = new StringBuilder();
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception Severity: Critical"));
                _sbError.AppendLine(string.Format("Type: {0}", MethodBase.GetCurrentMethod().GetType().FullName));
                _sbError.AppendLine(string.Format("Method: {0}", MethodBase.GetCurrentMethod().Name));
                _sbError.AppendLine(string.Format("Exception Handling Message: Error occurred while filtering/sorting/paging list collection for {0} : IBaseModelList<{1}>", typeof(TT), typeof(T)));
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception: {0}", ex));

                //Handle it later
                if (flagThrowExceptionIfAny)
                    throw;
                else
                    CfsLogging.LogFatal(string.Format("Error occurred while filtering/sorting/paging IList<{0}> ", typeof(T)), _sbError.ToString());
            }
            finally
            {
                CfsLogging.LogVerboseInfo(string.Format("Model Preparation Process [PopulateBaseModelListFromReader] Took [{0}] Msec.", (DateTime.Now - _startTime).TotalMilliseconds), string.Format("Process Took [{2}] Msec.\r\nProcess Detail: PopulateBaseModelListFromReader<{0},{1}>", typeof(TT), typeof(T), (DateTime.Now - _startTime).TotalMilliseconds));
            }

            return _actualInstanceOfBaseModelList;
        }

        /// <summary>
        /// Returns a single instance....
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="flagThrowExceptionIfAny"></param>
        /// <returns></returns>
        public static T PopulateBaseModelFromReader<T>(IDataReader reader, bool flagThrowExceptionIfAny = false)
        {
            var _listOfBaseModel = PopulateListFromReader<T>(reader, null, flagThrowExceptionIfAny);
            if (_listOfBaseModel != null && _listOfBaseModel.Any())
                return _listOfBaseModel.First();
            else
                return default(T);
        }

        private bool IsReaderColumnExists(IDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i) == columnName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] GetReaderAllColumns(IDataReader reader)
        {
            string[] _allColumnNames = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                _allColumnNames[i] = reader.GetName(i);
            }
            return _allColumnNames;
        }

        public static T CreateInstanceOfGivenModelType<T>(IDataReader reader, BaseQueryableRequestParameter baseDataRequestParameter = null, bool flagThrowExceptionIfAny = false)
        {
            object _actualInstance = null;
            try
            {
                string[] _allRaderColumnNames = GetReaderAllColumns(reader);
                _actualInstance = Activator.CreateInstance(typeof(T));
                if (_actualInstance.GetType().BaseType != typeof(BaseModel) && _actualInstance.GetType().IsSubclassOf(typeof(BaseModel)) == false)
                    throw new Exception("Model Type is not of BaseModel Type.");

                PropertyInfo[] _allSrcTypeProps = _actualInstance.GetType().GetProperties();
                foreach (PropertyInfo _srcPropertyInfo in _allSrcTypeProps)
                {

                    if (_srcPropertyInfo == null) continue;
                    if (_srcPropertyInfo.PropertyType.BaseType == typeof(BaseModel) || _srcPropertyInfo.PropertyType.IsSubclassOf(typeof(BaseModel)))
                    {
                        //Generic way to call current method recursively if current property type is of BaseModel type.
                        MethodInfo _currentMethodInfo = typeof(SfdcReflectionUtil).GetMethod("CreateInstanceOfGivenModelType");
                        MethodInfo _genericMethodInfoOfCurrentMethod = _currentMethodInfo.MakeGenericMethod(_srcPropertyInfo.PropertyType);
                        var instanceOfBaseModelTypeProperty = _genericMethodInfoOfCurrentMethod.Invoke(null, new object[] { reader, null, flagThrowExceptionIfAny }) as BaseModel;
                        //BaseModel instanceOfBaseModelTypeProperty = CreateInstanceOfGivenModelType(_srcPropertyInfo.PropertyType, reader, null, flagThrowExceptionIfAny);
                        _srcPropertyInfo.SetValue(_actualInstance, instanceOfBaseModelTypeProperty, null);
                        continue;
                    }

                    if (baseDataRequestParameter != null && baseDataRequestParameter.FilterOptions != null &&
                        baseDataRequestParameter.FilterOptions.FilterLists != null && baseDataRequestParameter.FilterOptions.FilterLists.Count > 0)
                    {
                        foreach (var _filter in baseDataRequestParameter.FilterOptions.FilterLists)
                        {
                            if (_filter.Field.ToUpper() == _srcPropertyInfo.Name.ToUpper())
                            {
                                _filter.FieldType = _srcPropertyInfo.PropertyType;
                            }

                        }
                    }



                    if (_srcPropertyInfo.PropertyType == Type.GetType("System.String") || _srcPropertyInfo.PropertyType == Type.GetType("System.Byte[]") ||
                        _srcPropertyInfo.PropertyType.IsValueType)
                    {
                        if (_srcPropertyInfo.CanRead == false || _srcPropertyInfo.CanWrite == false) continue;
                        try
                        {
                            object[] attributes = _srcPropertyInfo.GetCustomAttributes(typeof(SfdcColumn), false);


                            if (_srcPropertyInfo.PropertyType == Type.GetType("System.String"))
                            {
                                if (attributes.Any())
                                {

                                    var _sfdcColumnAttribute = (SfdcColumn)attributes.First();

                                    if (_allRaderColumnNames.Contains(_sfdcColumnAttribute.ColumnName, StringComparer.OrdinalIgnoreCase) == false)
                                        continue;

                                    if (_sfdcColumnAttribute.DateDayToOrdinalConversion)
                                    {
                                        _srcPropertyInfo.SetValue(_actualInstance,
                                            reader[_sfdcColumnAttribute.ColumnName] != DBNull.Value ? Util.AddDateDayOrdinal((int)reader[_sfdcColumnAttribute.ColumnName]) : null,
                                            null);
                                    }
                                    else
                                    {
                                        _srcPropertyInfo.SetValue(_actualInstance,
                                            reader[_sfdcColumnAttribute.ColumnName] != DBNull.Value ? reader[_sfdcColumnAttribute.ColumnName].ToString() : null,
                                            null);
                                    }
                                }
                                else
                                {
                                    if (_allRaderColumnNames.Contains(_srcPropertyInfo.Name, StringComparer.OrdinalIgnoreCase) == false)
                                        continue;

                                    _srcPropertyInfo.SetValue(_actualInstance,
                                        reader[_srcPropertyInfo.Name] != DBNull.Value ? reader[_srcPropertyInfo.Name].ToString() : null,
                                        null);
                                }
                            }
                            else if (_srcPropertyInfo.PropertyType.IsEnum)
                            {
                                if (attributes.Any())
                                {
                                    var _sfdcColumnAttribute = (SfdcColumn)attributes.First();

                                    if (_allRaderColumnNames.Contains(_sfdcColumnAttribute.ColumnName, StringComparer.OrdinalIgnoreCase) == false)
                                        continue;


                                    _srcPropertyInfo.SetValue(_actualInstance,
                                        reader[_sfdcColumnAttribute.ColumnName] != DBNull.Value ? Enum.Parse(_srcPropertyInfo.PropertyType, reader[_sfdcColumnAttribute.ColumnName].ToString().Replace(" ", ""), true) : null,
                                        null);
                                }
                                else
                                {
                                    if (_allRaderColumnNames.Contains(_srcPropertyInfo.Name, StringComparer.OrdinalIgnoreCase) == false)
                                        continue;

                                    _srcPropertyInfo.SetValue(_actualInstance,
                                        reader[_srcPropertyInfo.Name] != DBNull.Value ? Enum.Parse(_srcPropertyInfo.PropertyType, reader[_srcPropertyInfo.Name].ToString().Replace(" ", ""), true) : null,
                                        null);
                                }
                            }
                            else
                            {
                                if (attributes.Any())
                                {
                                    var _sfdcColumnAttribute = (SfdcColumn)attributes.First();

                                    if (_allRaderColumnNames.Contains(_sfdcColumnAttribute.ColumnName, StringComparer.OrdinalIgnoreCase) == false)
                                        continue;


                                    if (_sfdcColumnAttribute.DateDayToOrdinalConversion)
                                    {
                                        _srcPropertyInfo.SetValue(_actualInstance,
                                            reader[_sfdcColumnAttribute.ColumnName] != DBNull.Value ? Util.AddDateDayOrdinal((int)reader[_sfdcColumnAttribute.ColumnName]) : null,
                                            null);
                                    }
                                    else
                                    {
                                        if (_srcPropertyInfo.PropertyType != reader[_sfdcColumnAttribute.ColumnName].GetType())
                                        {

                                            //Get the underlying type property instead of the nullable generic
                                            var tProp = new NullableConverter(_srcPropertyInfo.PropertyType).UnderlyingType;

                                            _srcPropertyInfo.SetValue(_actualInstance,
                                        reader[_sfdcColumnAttribute.ColumnName] != DBNull.Value ? Convert.ChangeType(reader[_sfdcColumnAttribute.ColumnName], tProp) : null, null);


                                        }
                                        else
                                        {
                                            _srcPropertyInfo.SetValue(_actualInstance,
                                          reader[_sfdcColumnAttribute.ColumnName] != DBNull.Value ? reader[_sfdcColumnAttribute.ColumnName] : null,
                                          null);
                                        }

                                    }
                                }
                                else
                                {
                                    if (_allRaderColumnNames.Contains(_srcPropertyInfo.Name, StringComparer.OrdinalIgnoreCase) == false)
                                        continue;

                                    _srcPropertyInfo.SetValue(_actualInstance,
                                        reader[_srcPropertyInfo.Name] != DBNull.Value ? reader[_srcPropertyInfo.Name] : null,
                                        null);
                                }
                            }

                        }
                        catch (IndexOutOfRangeException ex1)
                        {
                            CfsLogging.LogVerboseInfo("Swallowed Exception", string.Format("Below exception is swallowed and not thrown intentionally\r\n{0}", ex1));
                            //Swallow this Exception since Property Name not found in Reader Columns, just ignore and continue to next one
                            CfsLogging.LogVerboseInfo(string.Format("Method: {0}", MethodBase.GetCurrentMethod().Name),
                                string.Format("Class: [{0}] Property: [{1}] not found in READER Columns while creating a reference of a given class type", typeof(T), _srcPropertyInfo.Name));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                StringBuilder _sbError = new StringBuilder();
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception Severity: Critical"));
                _sbError.AppendLine(string.Format("Type: {0}", MethodBase.GetCurrentMethod().GetType().FullName));
                _sbError.AppendLine(string.Format("Method: {0}", MethodBase.GetCurrentMethod().Name));
                _sbError.AppendLine(string.Format("Exception Handling Message: Error occurred while creating instance of type {0} from a given data-reader row.", typeof(T)));
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine(string.Format("Exception: {0}", ex));

                //Suppress the exception, don't blow the code so corrosponding property will be null and continue to next one
                if (flagThrowExceptionIfAny)
                    throw;
                CfsLogging.LogFatal(string.Format("Error occurred while creating [BaseModel] instance of type {0} from a given data-reader row.", typeof(T)), _sbError.ToString());
            }

            return (T)_actualInstance;
        }

        private static string CreateModelFile<T>(IDataReader reader, string oracleTableName = null, string[] primaryKeyColumns = null, T className = default(T))
        {
            DataTable _readerSchemaTbl = reader.GetSchemaTable();
            StringBuilder _modelCode = new StringBuilder();
            _modelCode.AppendLine(string.Format("using System;\r\n" +
                                                "using System.Data;\r\n" +
                                                "using Oracle.DataAccess.Client;\r\n" +
                                                "using OracleOrm.Models;\r\n" +
                                                "using OracleOrm.Models.MetaDataAttributes;\r\n"));

            _modelCode.AppendLine(string.Format("namespace App.Models.{0}", typeof(T) != typeof(string) ? typeof(T).Name : className.ToString()));
            _modelCode.AppendLine("{");

            _modelCode.AppendFormat("[DbTableDefinition(\"{0}\")]", string.IsNullOrEmpty(oracleTableName) ? "<TableName>" : oracleTableName.ToUpper());
            _modelCode.AppendLine();
            _modelCode.AppendFormat("[DbProcDefinition(\"{0}_DML.{0}_INS\",\r\n\"{0}_DML.{0}_UPD\",\r\n\"{0}_DML.{0}_DEL\",\r\n\"{0}_DML.{0}_GET\")]", string.IsNullOrEmpty(oracleTableName) ? "<TableName>" : oracleTableName.ToUpper());
            _modelCode.AppendLine();
            _modelCode.AppendFormat("public class {0} : BaseModel", typeof(T) != typeof(string) ? typeof(T).Name : className.ToString());
            _modelCode.AppendLine();
            _modelCode.AppendLine("{");
            _modelCode.AppendLine();

            if (_readerSchemaTbl != null && _readerSchemaTbl.Rows.Count > 0)
            {
                int i = 0;
                // Creates a TextInfo based on the "en-US" culture.
                TextInfo _ti = new CultureInfo("en-US", false).TextInfo;
                foreach (DataRow _rw in _readerSchemaTbl.Rows)
                {
                    bool isPrimaryKey = false;
                    int _columnOrder = ++i;
                    if (_columnOrder == 1)
                        _modelCode.Replace("<TableName>", _rw["BaseTableName"].ToString().ToUpper());
                    OracleDbType _columnParameterType = (OracleDbType)Int32.Parse(_rw["ProviderType"].ToString());

                    if (primaryKeyColumns != null && primaryKeyColumns.FirstOrDefault(x => x.ToUpper() == _rw["ColumnName"].ToString().ToUpper()) != null)
                        _modelCode.AppendFormat("[DbInsertParam(\"out{0}\",OracleDbType.{1},{2}{4}{3})]", _rw["ColumnName"].ToString().ToUpper(), _columnParameterType, _columnOrder, string.IsNullOrEmpty(_rw["DataType"].ToString()) == false && _rw["DataType"].ToString().Contains("String") ? ",size:" + _rw["ColumnSize"] : string.Empty, ",ParameterDirection.Output");
                    else
                        _modelCode.AppendFormat("[DbInsertParam(\"in{0}\",OracleDbType.{1},{2}{3})]", _rw["ColumnName"].ToString().ToUpper(), _columnParameterType, _columnOrder, string.IsNullOrEmpty(_rw["DataType"].ToString()) == false && _rw["DataType"].ToString().Contains("String") ? ",size:" + _rw["ColumnSize"] : string.Empty);

                    _modelCode.AppendLine();
                    _modelCode.AppendFormat("[DbUpdateParam(\"in{0}\",OracleDbType.{1},{2}{3})]", _rw["ColumnName"].ToString().ToUpper(), _columnParameterType, _columnOrder, string.IsNullOrEmpty(_rw["DataType"].ToString()) == false && _rw["DataType"].ToString().Contains("String") ? ",size:" + _rw["ColumnSize"] : string.Empty);
                    _modelCode.AppendLine();
                    _modelCode.AppendFormat("[DbGetParam(\"in{0}\",OracleDbType.{1},{2}{3})]", _rw["ColumnName"].ToString().ToUpper(), _columnParameterType, _columnOrder, string.IsNullOrEmpty(_rw["DataType"].ToString()) == false && _rw["DataType"].ToString().Contains("String") ? ",size:" + _rw["ColumnSize"] : string.Empty);
                    _modelCode.AppendLine();

                    if (primaryKeyColumns != null && primaryKeyColumns.FirstOrDefault(x => x.ToUpper() == _rw["ColumnName"].ToString().ToUpper()) != null)
                    {
                        isPrimaryKey = true;
                        _modelCode.AppendFormat("[DbGetByKeyParam(\"in{0}\",OracleDbType.{1},{2}{3})]", _rw["ColumnName"].ToString().ToUpper(), _columnParameterType, _columnOrder, string.IsNullOrEmpty(_rw["DataType"].ToString()) == false && _rw["DataType"].ToString().Contains("String") ? ",size:" + _rw["ColumnSize"] : string.Empty);
                        _modelCode.AppendLine();
                        _modelCode.AppendFormat("[DbDeleteParam(\"in{0}\",OracleDbType.{1},{2}{3})]", _rw["ColumnName"].ToString().ToUpper(), _columnParameterType, _columnOrder, string.IsNullOrEmpty(_rw["DataType"].ToString()) == false && _rw["DataType"].ToString().Contains("String") ? ",size:" + _rw["ColumnSize"] : string.Empty);
                        _modelCode.AppendLine();
                    }

                    if (isPrimaryKey)
                        _modelCode.AppendFormat("[SfdcColumn(\"{0}\", isPrimaryKey:true)]", _rw["ColumnName"].ToString().ToUpper());
                    else
                        _modelCode.AppendFormat("[SfdcColumn(\"{0}\")]", _rw["ColumnName"].ToString().ToUpper());

                    _modelCode.AppendLine();

                    if (_rw["DataType"].ToString() == "System.String" || _rw["DataType"].ToString() == "System.Byte[]")
                        _modelCode.AppendFormat("public {0} {1} {2} get; set; {3}", _rw["DataType"].ToString().Replace("System.", ""), _ti.ToTitleCase(_rw["ColumnName"].ToString().Replace("_", " ").ToLower()).Replace(" ", ""), "{", "}");
                    else
                        _modelCode.AppendFormat("public {0}? {1} {2} get; set; {3}", _rw["DataType"].ToString().Replace("System.", ""), _ti.ToTitleCase(_rw["ColumnName"].ToString().Replace("_", " ").ToLower()).Replace(" ", ""), "{", "}");
                    _modelCode.AppendLine();
                    _modelCode.AppendLine();
                }

            }
            _modelCode.AppendLine("}");
            _modelCode.AppendLine("}");
            if (Directory.Exists(@"C:\Temp") == false)
                Directory.CreateDirectory(@"C:\Temp\");
            using (StreamWriter _modelCodeFile = new StreamWriter(string.Format("c:\\temp\\{0}.cs", typeof(T) != typeof(string) ? typeof(T).Name : className.ToString()), false))
            {
                _modelCodeFile.Write(_modelCode.ToString());
            }
            return _modelCode.ToString();
        }

        private static string CreateModelFile<T>(IDataReader reader, T className = default(T))
        {
            DataTable _readerSchemaTbl = reader.GetSchemaTable();
            StringBuilder _modelCode = new StringBuilder();
            _modelCode.AppendLine(string.Format("using System;\r\n" +
                                                "using System.Data;\r\n" +
                                                "using Oracle.DataAccess.Client;\r\n" +
                                                "using OracleOrm.Models;\r\n" +
                                                "using OracleOrm.Models.MetaDataAttributes;\r\n"));

            _modelCode.AppendLine(string.Format("namespace App.Models.{0}", typeof(T) != typeof(string) ? typeof(T).Name : className.ToString()));
            _modelCode.AppendLine("{");
            _modelCode.AppendLine();
            _modelCode.AppendFormat("public class {0} : BaseModel", typeof(T) != typeof(string) ? typeof(T).Name : className.ToString());
            _modelCode.AppendLine();
            _modelCode.AppendLine("{");
            _modelCode.AppendLine();

            if (_readerSchemaTbl != null && _readerSchemaTbl.Rows.Count > 0)
            {
                int i = 0;
                // Creates a TextInfo based on the "en-US" culture.
                TextInfo _ti = new CultureInfo("en-US", false).TextInfo;
                foreach (DataRow _rw in _readerSchemaTbl.Rows)
                {

                    _modelCode.AppendFormat("[SfdcColumn(\"{0}\")]", _rw["ColumnName"].ToString().ToUpper());
                    _modelCode.AppendLine();
                    if (_rw["DataType"].ToString() == "System.String" || _rw["DataType"].ToString() == "System.Byte[]")
                        _modelCode.AppendFormat("public {0} {1} {2} get; set; {3}", _rw["DataType"].ToString().Replace("System.", ""), _ti.ToTitleCase(_rw["ColumnName"].ToString().Replace("_", " ").ToLower()).Replace(" ", ""), "{", "}");
                    else
                        _modelCode.AppendFormat("public {0}? {1} {2} get; set; {3}", _rw["DataType"].ToString().Replace("System.", ""), _ti.ToTitleCase(_rw["ColumnName"].ToString().Replace("_", " ").ToLower()).Replace(" ", ""), "{", "}");
                    _modelCode.AppendLine();
                    _modelCode.AppendLine();
                }

            }
            _modelCode.AppendLine("}");
            _modelCode.AppendLine("}");
            if (Directory.Exists(@"C:\Temp") == false)
                Directory.CreateDirectory(@"C:\Temp\");
            using (StreamWriter _modelCodeFile = new StreamWriter(string.Format("c:\\temp\\{0}.cs", typeof(T) != typeof(string) ? typeof(T).Name : className.ToString()), false))
            {
                _modelCodeFile.Write(_modelCode.ToString());
            }
            return _modelCode.ToString();
        }

        public static string CreateModelFile<T>(string oracleConnection, string oracleTableName = null, string[] primaryKeyColumns = null, T className = default(T))
        {
            //Only run in debug mode.....
#if DEBUG

            if (string.IsNullOrEmpty(oracleConnection))
                throw new Exception("Oracle Connection String is not provided, please provide valid Connection String !");

            if (string.IsNullOrEmpty(oracleTableName))
            {
                var baseModelType = typeof(T);
                //Get all Entity (T - Base Model) Level Attributes
                object[] attributesOfType = baseModelType.GetCustomAttributes(typeof(DbTableDefinition), true);

                if (!attributesOfType.Any())
                    throw new Exception(string.Format("[DbTableDefinition] attribute is missing for type of {0}", baseModelType));

                //Get _dbTableDefinition Attribute for Entity (T - Base Model)
                var _dbTableDefinitionAttribute = attributesOfType.First() as DbTableDefinition;
                if (_dbTableDefinitionAttribute == null || string.IsNullOrEmpty(_dbTableDefinitionAttribute.TableName))
                    throw new Exception(string.Format("[DbTableDefinition] attribute is missing TableName for type of {0}", baseModelType));

                oracleTableName = _dbTableDefinitionAttribute.TableName.ToUpper();
            }


            OracleConnection _conn = new OracleConnection(oracleConnection);
            try
            {
                //Just to get the structure of the table to populate underlying Model
                OracleCommand _cmd = new OracleCommand(string.Format("Select * from {0} Where 1 = 2", oracleTableName), _conn);
                _cmd.CommandType = CommandType.Text;
                _conn.Open();
                var _reader = _cmd.ExecuteReader(CommandBehavior.CloseConnection);

                if (primaryKeyColumns == null || !primaryKeyColumns.Any())
                {
                    primaryKeyColumns = GetIndicesForTable(oracleConnection, oracleTableName).ToArray();
                }
                //Generate Model File.....in C:\Temp\
                return CreateModelFile<T>(_reader, oracleTableName, primaryKeyColumns, className);
            }
            catch (Exception ex)
            {
                CfsLogging.LogToFlatFile("Model Generation Error", string.Format("Error occurred while generating model from provided table definition\r\n\r\nException:\r\n{0}", ex));
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }

#endif
            return string.Empty;
        }

        public static string CreateModelFileByQuery<T>(string oracleConnection, string sqlQuery, T className = default(T))
        {
            //Only run in debug mode.....
#if DEBUG

            if (string.IsNullOrEmpty(oracleConnection))
                throw new Exception("Oracle Connection String is not provided, please provide valid Connection String !");

            if (string.IsNullOrEmpty(sqlQuery))
                throw new Exception(string.Format("sqlQuery parameter is required for select query."));



            OracleConnection _conn = new OracleConnection(oracleConnection);
            try
            {
                //Just to get the structure of the table to populate underlying Model
                OracleCommand _cmd = new OracleCommand(string.Format(sqlQuery), _conn);
                _cmd.CommandType = CommandType.Text;
                _conn.Open();
                var _reader = _cmd.ExecuteReader(CommandBehavior.CloseConnection);

                //Generate Model File.....in C:\Temp\
                return CreateModelFile<T>(_reader, className);
            }
            catch (Exception ex)
            {
                CfsLogging.LogToFlatFile("Model Generation Error", string.Format("Error occurred while generating model from provided sql query: [{1}]\r\n\r\nException:\r\n{0}", ex, sqlQuery));
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }

#endif
            return string.Empty;
        }


        /// <summary>
        /// Gets the indices of the given table.
        /// </summary>
        private static List<string> GetIndicesForTable(string oracleConnectionString, string oracleTableName)
        {
            List<string> indices = new List<string>();

            using (OracleConnection oracleConnection = new OracleConnection(oracleConnectionString))
            {
                oracleConnection.Open();

                // Get the primary key for the table
                string tablesPrimaryKey = GetTablesPrimaryKey(oracleConnectionString, oracleTableName);

                // Only query the table if it has a primary key
                if (!String.IsNullOrEmpty(tablesPrimaryKey))
                {
                    // Get the table's schema's info based on the primary key index
                    string[] restrictions = new string[] { null, tablesPrimaryKey, null, oracleTableName };
                    DataTable indexTable = oracleConnection.GetSchema("IndexColumns", restrictions);

                    foreach (DataRow row in indexTable.Rows)
                    {
                        // Get each column that is within the index
                        indices.Add(row["COLUMN_NAME"].ToString());
                    }
                }
            }

            return indices;
        }

        /// <summary>
        /// Gets the primary key for the table.
        /// </summary>
        /// <returns>Primary key</returns>
        private static string GetTablesPrimaryKey(string oracleConnectionString, string oracleTableName)
        {
            using (OracleConnection oracleConnection = new OracleConnection(oracleConnectionString))
            {
                oracleConnection.Open();

                OracleCommand oracleCommand = new OracleCommand();
                oracleCommand.Connection = oracleConnection;
                oracleCommand.CommandText = "SELECT A.CONSTRAINT_NAME FROM ALL_CONS_COLUMNS A JOIN ALL_CONSTRAINTS C ON A.CONSTRAINT_NAME = C.CONSTRAINT_NAME WHERE C.TABLE_NAME = '" + oracleTableName +
                                            "' AND C.CONSTRAINT_TYPE = 'P'";
                OracleDataReader oracleDataReader = oracleCommand.ExecuteReader();

                if (oracleDataReader.Read())
                {
                    return oracleDataReader["CONSTRAINT_NAME"].ToString();
                }
                else
                {
                    // No primary key on table
                    return null;
                }
            };
        }

        /// <summary>
        /// Returns string containing all public properties of a type with values.
        /// </summary>
        /// <param name="typeInstance"></param>
        /// <returns></returns>
        public static string InstanceToString<T>(T typeInstance)
        {
            try
            {
                StringBuilder _instanceString = new StringBuilder();
                _instanceString.AppendLine("\r\n");
                PropertyInfo[] _allSrcTypeProps = typeInstance.GetType().GetProperties();
                foreach (var _srcPropertyInfo in _allSrcTypeProps)
                {
                    if (_srcPropertyInfo == null) continue;
                    if (_srcPropertyInfo.PropertyType == Type.GetType("System.String") || _srcPropertyInfo.PropertyType == Type.GetType("System.Byte[]") ||
                        _srcPropertyInfo.PropertyType.IsValueType)
                    {
                        if (_srcPropertyInfo.CanRead == false) continue;
                        object _srcPropValue = _srcPropertyInfo.GetValue(typeInstance, null);
                        if (_srcPropValue != null)
                        {
                            if (_srcPropValue.ToString().Trim().Length == 0)
                                _instanceString.AppendLine(string.Format("{0}.{1} = {2}", typeInstance.GetType().Name, _srcPropertyInfo.Name, "[BLANK]"));
                            else
                                _instanceString.AppendLine(string.Format("{0}.{1} = {2}", typeInstance.GetType().Name, _srcPropertyInfo.Name, _srcPropValue));
                        }
                        else
                        {
                            _instanceString.AppendLine(string.Format("{0}.{1} = [NULL]", typeInstance.GetType().Name, _srcPropertyInfo.Name));
                        }
                    }
                }
                _instanceString.AppendLine("\r\n");
                return _instanceString.ToString();
            }
            catch (Exception)
            {

                throw;
            }

        }

        public static bool IsNumber<T>(T value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }

        /// <summary>
        /// Copies all String/ValueType matching name Properties from source instance to target instance of any given type
        /// </summary>
        /// <param name="srcTypeInstance"></param>
        /// <param name="targetTypeInstance"></param>
        public static void CopySrcInstancePropsToTargetInstanceProps<TS, TT>(TS srcTypeInstance, TT targetTypeInstance)
        {
            try
            {
                PropertyInfo[] _allSrcTypeProps = srcTypeInstance.GetType().GetProperties();
                foreach (var _srcPropertyInfo in _allSrcTypeProps)
                {
                    if (_srcPropertyInfo == null) continue;
                    if (_srcPropertyInfo.PropertyType == Type.GetType("System.String") || _srcPropertyInfo.PropertyType == Type.GetType("System.Byte[]") ||
                        _srcPropertyInfo.PropertyType.IsValueType)
                    {
                        if (_srcPropertyInfo.CanRead == false) continue;
                        object _srcPropValue = _srcPropertyInfo.GetValue(srcTypeInstance, null);
                        if (_srcPropValue == null) continue;
                        PropertyInfo _targetPropInfo = targetTypeInstance.GetType().GetProperty(_srcPropertyInfo.Name);
                        if (_targetPropInfo != null && _targetPropInfo.CanWrite)
                            if (_targetPropInfo.PropertyType == Type.GetType("System.String"))
                                _targetPropInfo.SetValue(targetTypeInstance, _srcPropValue.ToString(), null);
                            else
                                _targetPropInfo.SetValue(targetTypeInstance, _srcPropValue, null);
                    }
                }
            }
            catch (Exception ex)
            {

                StringBuilder _sbError = new StringBuilder();
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine("Type: " + MethodBase.GetCurrentMethod().GetType());
                _sbError.AppendLine("Method: " + MethodBase.GetCurrentMethod().Name);
                _sbError.AppendLine("Error Type: Critical-Reflection");
                _sbError.AppendLine("Message: Error occurred while coping properties from source instance type to target instance type.");
                _sbError.AppendLine();
                _sbError.AppendLine("Exception: ");
                _sbError.AppendLine(ex.ToString());
                //CfsLogging.LogFatal(ex.Message,_sbError.ToString());
            }

        }

        /// <summary>
        /// Gets the value of a given property of an object 
        /// </summary>
        /// <param name="source">the source object</param>
        /// <param name="propertyName">the name of the property</param>
        /// <returns></returns>
        public static object GetPropertyValue(object source, string propertyName)
        {
            if (source == null)
            {
                return null;
            }
            object obj = source;

            try
            {


                // Split property name to parts (propertyName could be hierarchical, like obj.subobj.subobj.property
                string[] propertyNameParts = propertyName.Split('.');

                foreach (string propertyNamePart in propertyNameParts)
                {
                    if (obj == null) return null;

                    // propertyNamePart could contain reference to specific 
                    // element (by index) inside a collection
                    if (!propertyNamePart.Contains("["))
                    {
                        PropertyInfo pi = obj.GetType().GetProperty(propertyNamePart);
                        if (pi == null)
                        {
                            return null;

                        }
                        obj = pi.GetValue(obj, null);
                    }
                    else
                    {   // propertyNamePart is areference to specific element 
                        // (by index) inside a collection
                        //   get collection name and element index
                        int indexStart = propertyNamePart.IndexOf("[") + 1;
                        string collectionPropertyName = propertyNamePart.Substring(0, indexStart - 1);
                        int collectionElementIndex = Int32.Parse(propertyNamePart.Substring(indexStart, propertyNamePart.Length - indexStart - 1));
                        //   get collection object
                        PropertyInfo pi = obj.GetType().GetProperty(collectionPropertyName);
                        if (pi == null) return null;
                        object unknownCollection = pi.GetValue(obj, null);
                        System.Collections.IList collectionAsList = unknownCollection as System.Collections.IList;
                        if (collectionAsList != null)
                        {
                            obj = collectionAsList[collectionElementIndex];
                        }
                        else if (unknownCollection.GetType().IsArray)
                        {
                            object[] collectionAsArray = unknownCollection as Array[];
                            obj = collectionAsArray[collectionElementIndex];
                        }
                        else
                        {
                            return null;
                        }

                    }
                }
            }
            catch (Exception ex)
            {

                StringBuilder _sbError = new StringBuilder();
                _sbError.AppendLine();
                _sbError.AppendLine();
                _sbError.AppendLine("Type: " + MethodBase.GetCurrentMethod().GetType());
                _sbError.AppendLine("Method: " + MethodBase.GetCurrentMethod().Name);
                _sbError.AppendLine("Error Type: Critical-Reflection");
                _sbError.AppendLine("Message: Error occurred while retrieving property value for " + propertyName);
                _sbError.AppendLine();
                _sbError.AppendLine("Exception: ");
                _sbError.AppendLine(ex.Message.ToString());
            }

            return obj;
        }

        public static DateTime RetrieveAssemblyBuiltTimestamp()
        {
            //return  File.GetCreationTime(Assembly.GetExecutingAssembly().Location);

            //This was a hack found on internet - http://stackoverflow.com/questions/1600962/displaying-the-build-date

            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream s = null;

            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
