using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using HUtils.DBTasks.DAL;

namespace HUtils.DBTasks
{
    /// <summary>
    /// Base DB Task
    /// </summary>
    public abstract class BaseDBTask
    {
        #region Nested Types

        /// <summary>
        /// Represents command info
        /// </summary>
        private struct CommandInfo
        {
            public CommandType CommandType { get; set; }
            public string CommandText { get; set; }
        }

        #endregion

        #region Consts

        /// <summary>
        /// Represents symple types list
        /// </summary>
        private static readonly string[] SIMPLE_TYPES = { "String", "Byte", "Int16", "Int32", "Int64", "Decimal", "Double", "DateTime", "Boolean" };

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Gets DB connection
        /// </summary>
        /// <returns></returns>
        private static IDBConnection GetSqlConnection(Type taskType)
        {
            ConnectionStringElement cnnConfig = null;
            var connectionStringNameAttr = taskType.GetCustomAttributes(typeof(ConnectionStringNameAttribute), false).FirstOrDefault() as ConnectionStringNameAttribute;
            if (connectionStringNameAttr != null)
            {
                cnnConfig = ConnectionStringsConfiguration.Instance.ConnectionStrings.GetByName(connectionStringNameAttr.ConnectionStringName);                
            }
            else
            {
                cnnConfig = ConnectionStringsConfiguration.Instance.DefaultConnectionString;
            }

            if (cnnConfig == null)
            {
                throw new DBTaskConfigurationException("Connection string not found", null);
            }

            return DBConnection.GetDBConnection(cnnConfig.ProviderName, cnnConfig.ConnectionString);
        }

        /// <summary>
        /// Gets DB command info assigned to the given method
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private static CommandInfo GetDBCommandInfo(MethodBase method)
        {
            // default command type
            var commandType = CommandType.StoredProcedure;
            var commandText = String.Empty;
            
            // defining the command type
            DBCommandAttribute commandAttr = null;
            foreach (DBCommandAttribute attr in method.GetCustomAttributes(typeof(DBCommandAttribute), true))
            {
                commandAttr = attr;
                break;
            }

            if (commandAttr != null)
            {
                // custom command
                commandType = commandAttr.CommandType;
                commandText = commandAttr.CommandText;
            }
            else
            {
                // sp command

                // defining sp name
                var taskName = method.ReflectedType.Name;

                // removing "task" word of the end of task name
                if (taskName.EndsWith("task", StringComparison.InvariantCultureIgnoreCase))
                {
                    taskName = taskName.Remove(taskName.Length - 4);
                }
                                
                // getting method name
                var methodName = method.Name;
                                
                commandText = String.Concat(taskName, methodName);
            }

            if (String.IsNullOrEmpty(commandText))
            {
                throw new DBTaskException(String.Format("Comand info for {0}.{1} is invalid", method.ReflectedType.Name, method.Name), null);
            }

            return new CommandInfo { CommandType = commandType, CommandText = commandText };
        }

        /// <summary>
        /// Adds recursive the given parameters into the given SqlParameterCollection
        /// </summary>
        /// <param name="propInfo"></param>
        /// <param name="obj"></param>
        /// <param name="output"></param>
        private static void AddSqlCommandParams(PropertyInfo propInfo, object obj, DBParamCollection output)
        {
            // getting the param value & value type
            var val = propInfo.GetValue(obj, null);

            // checking Datetime
            if (val != null && val is DateTime && (DateTime)val == DateTime.MinValue)
            {
                val = null;
            }

            // defining mandatory attrinute
            var mndPropAttr = propInfo.GetCustomAttributes(typeof(DBContractMandatoryPropertyAttribute), true).FirstOrDefault();
            var isMandatoryParam = mndPropAttr != null && ((DBContractMandatoryPropertyAttribute)mndPropAttr).MandatoryParameter;

            // exclude null parameters if needed
            if (val != null || isMandatoryParam)
            {
                // exclude the props which have ExcludeFromParamList as "true" of DBContractAdvancedPropertyAttribute custom attribute
                var addPropAttr = propInfo.GetCustomAttributes(typeof(DBContractAdvancedPropertyAttribute), true).FirstOrDefault();
                if (addPropAttr == null || !((DBContractAdvancedPropertyAttribute)addPropAttr).ExcludeFromParamList)
                {
                    if (val == null)
                    {
                        output.Add(new SimpleDBParam(propInfo.Name, DBNull.Value));
                    }
                    else
                    {
                        // predefined types

                        // blob
                        var blobPropAttr = propInfo.GetCustomAttributes(typeof(DBBlobPropertyAttribute), true).FirstOrDefault();
                        if (blobPropAttr != null)
                        {
                            output.Add(new SimpleDBParam(propInfo.Name, val));
                        }

                        // runtime tipes
                        else
                        {
                            var valType = val.GetType();

                            // checking is enum
                            if (valType.IsEnum)
                            {
                                // just converting to int
                                output.Add(new SimpleDBParam(propInfo.Name, Convert.ToInt32(val)));
                            }
                            else
                            {
                                // other types

                                // simple types
                                if (SIMPLE_TYPES.Contains(valType.Name))
                                {
                                    output.Add(new SimpleDBParam(propInfo.Name, val));
                                }
                                // list types
                                else if (valType.IsArray || (valType.IsGenericType && valType.GetGenericTypeDefinition() != typeof(Nullable<>)))
                                {
                                    output.Add(new StructuredDBParam(propInfo.Name, (IEnumerable<object>)val));
                                }
                                // object types
                                else
                                {
                                    // calling this function recurcive

                                    // getting the props of the object
                                    var objProps = val.GetType().GetProperties();
                                    foreach (var childProp in objProps)
                                    {
                                        AddSqlCommandParams(childProp, val, output);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Builds DB Command parameter collection
        /// </summary>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <param name="pagingParams"></param>
        /// <param name="addTotalCountAsOutputParam"></param>
        /// <returns></returns>
        private static DBParamCollection GetDBParamCollection(MethodBase method, object parameters, DBPagingParams pagingParams, bool addTotalCountAsOutputParam)
        {
            var res = new DBParamCollection();
            
            // adding DB Parameters to the command parameters collection
            // based on the properties of the given parameters

            // getting the list of the properties of the given parameters if needed
            if (parameters != null)
            {
                var paramProperties = parameters.GetType().GetProperties();
                
                // adding the names and the values to the parameters collection
                foreach (var paramProp in paramProperties)
                {
                    AddSqlCommandParams(paramProp, parameters, res);
                }
            }

            // adding paging params if needed
            if (pagingParams != null)
            {
                // page index & size
                res.Add(new SimpleDBParam("PageIndex", pagingParams.PageIndex));
                res.Add(new SimpleDBParam("PageSize", pagingParams.PageSize));

                // sorting if needed
                if (!String.IsNullOrEmpty(pagingParams.SortColumn))
                {
                    res.Add(new SimpleDBParam("SortColumn", pagingParams.SortColumn));
                    res.Add(new SimpleDBParam("SortDirection", (byte)pagingParams.SortDirection));
                }

                // and total count as an output parameter if needed
                if (addTotalCountAsOutputParam)
                {
                    res.Add(new SimpleDBParam("TotalCount", ParameterDirection.Output, 0L));
                }
            }

            return res;
        }

        /// <summary>
        /// Build a text message for DB Exception based on the given sql command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="innerExeption"></param>
        /// <returns></returns>
        private static string GetExceptionMessage(string commandText, DBParamCollection paramCollection, Exception innerExeption)
        {
            var res = new StringBuilder();

            if (innerExeption != null)
            {
                res.Append(innerExeption.Message);
                res.Append(Environment.NewLine);
            }

            res.AppendFormat(@"An exception has been thrown while trying to execute ""{0}"" stored procedure with the following parameters:", commandText);
            res.Append(Environment.NewLine);

            foreach (var param in paramCollection)
            {
                if (param.Direction == ParameterDirection.Output)
                {
                    res.AppendFormat(@"({0}) ""{1}"": output param", param.ParamType, param.Name);
                }
                else
                {
                    res.AppendFormat(@"({0}) ""{1}"" = ""{2}""", param.ParamType, param.Name, param.Value != null ? param.Value.ToString() : "NULL");
                }
                res.Append(Environment.NewLine);
            }

            return res.ToString();
        }

        /// <summary>
        /// Assigns the given value to the given property
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="prop"></param>
        /// <param name="value"></param>
        private static void SetDBValue(object obj, PropertyInfo prop, object value)
        {
            if (prop.CanWrite)
            {
                Type enumPropType = null;
                if (prop.PropertyType.IsEnum)
                {
                    enumPropType = prop.PropertyType;
                }
                else
                {
                    var gTypeArgs = prop.PropertyType.GetGenericArguments();
                    if (gTypeArgs != null && gTypeArgs.Length > 0 && gTypeArgs[0].IsEnum)
                    {
                        enumPropType = gTypeArgs[0];
                    }
                }

                if (enumPropType != null)
                {
                    // enum prop

                    if (!(value is DBNull))
                    {
                        // db value is not null
                        prop.SetValue(obj, Enum.Parse(enumPropType, value.ToString()), null);
                    }
                    else
                    {
                        // db value is null
                        if (!prop.PropertyType.IsGenericType || prop.PropertyType.GetGenericTypeDefinition() != typeof(Nullable<>))
                        {
                            // the enum is not nullable
                            // setting default value which is 0
                            prop.SetValue(obj, Enum.Parse(enumPropType, "0"), null);
                        }
                    }
                }
                else
                {
                    // not enum
                    if (value is DBNull)
                    {
                        // null
                        prop.SetValue(obj, null, null);
                    }
                    else
                    {
                        // not null
                        prop.SetValue(obj, value, null);
                    }
                }
            }
        }

        /// <summary>
        /// Gets if the given datareader contains a value by the given name
        /// </summary>
        /// <param name="dataReader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool DataReaderHasValue(IDataReader dataReader, string name)
        {
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                if (dataReader.GetName(i).Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Executes the SP and returns its return value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static int GetReturnValue(MethodBase method, object parameters)
        {
            // creating sql connection and command            
            var commandInfo = GetDBCommandInfo(method);

            // adding return value
            var dbParams = GetDBParamCollection(method, parameters, null, false);
            dbParams.Add(new SimpleDBParam("returnVal", ParameterDirection.ReturnValue, 0));

            try
            {
                using (var connection = GetSqlConnection(method.DeclaringType))
                {
                    connection.Open();
                    connection.ExecuteNonQuery(dbParams, commandInfo.CommandText, commandInfo.CommandType);
                }
                return (int)dbParams.GetParamValue("returnVal");
            }
            catch (DBException ex)
            {
                if (ex.IsUserException)
                {
                    throw new DBTaskUserException(ex.Message, ex, ex.UserExceptionCode);
                }

                throw new DBTaskException(GetExceptionMessage(commandInfo.CommandText, dbParams, ex.InnerException), ex);
            }
            catch (Exception ex)
            {
                throw new DBTaskException(GetExceptionMessage(commandInfo.CommandText, dbParams, ex.InnerException), ex);
            }
        }
        
        /// <summary>
        /// Gets a list of the requested type objects from the DB
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <param name="pagingParams"></param>
        /// <param name="getTotalCountFromResultSet"></param>
        /// <returns></returns>
        private static IEnumerable<T> GetSingleListResult<T>(MethodBase method, object parameters, DBPagingParams pagingParams, bool getTotalCountFromResultSet)
        {
            // creating sql connection and command
            var commandInfo = GetDBCommandInfo(method);
            var dbParams = GetDBParamCollection(method, parameters, pagingParams, !getTotalCountFromResultSet);
            
            try
            {
                var res = new List<T>();

                using (var connection = GetSqlConnection(method.DeclaringType))
                {
                    connection.Open();

                    // running the command
                    using (var dataReader = connection.ExecuteReader(dbParams, commandInfo.CommandText, commandInfo.CommandType))
                    {
                        // getting returning type info
                        var resProperties = typeof(T).GetProperties();

                        // paging params routine
                        var isFirstPassed = false;

                        // running through the result set
                        while (dataReader.Read())
                        {
                            // creating an instance of the requested object
                            // and assigning its properties
                            var resObj = Activator.CreateInstance<T>();

                            foreach (var prop in resProperties)
                            {
                                if (DataReaderHasValue(dataReader, prop.Name))
                                {
                                    SetDBValue(resObj, prop, dataReader[prop.Name]);
                                }
                            }

                            res.Add(resObj);

                            // paging params routine
                            // setting paging params total count from result set if needed
                            if (pagingParams != null && getTotalCountFromResultSet && !isFirstPassed)
                            {
                                pagingParams.TotalCount = (long)dataReader["TotalCount"];
                                isFirstPassed = true;
                            }
                        }

                        // paging params routine
                        // setting paging params total count from output parameter if needed
                        if (pagingParams != null && !getTotalCountFromResultSet)
                        {
                            pagingParams.TotalCount = (long)dbParams.GetParamValue("TotalCount");
                        }
                    }
                }

                return res;
            }
            catch (DBException ex)
            {
                if (ex.IsUserException)
                {
                    throw new DBTaskUserException(ex.Message, ex, ex.UserExceptionCode);
                }

                throw new DBTaskException(GetExceptionMessage(commandInfo.CommandText, dbParams, ex.InnerException), ex);
            }
            catch (Exception ex)
            {
                throw new DBTaskException(GetExceptionMessage(commandInfo.CommandText, dbParams, ex.InnerException), ex);
            }
        }

        /// <summary>
        /// Gets an instance of af the given object with multiple list (not array) as properties in it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <param name="pagingParams"></param>
        /// <param name="getTotalCountFromResultSet"></param>
        /// <returns></returns>
        private static T GetMultipleListResult<T>(MethodBase method, object parameters, DBPagingParams pagingParams, bool getTotalCountFromResultSet)
        {
            // creating sql connection and command
            var commandInfo = GetDBCommandInfo(method);
            var dbParams = GetDBParamCollection(method, parameters, pagingParams, !getTotalCountFromResultSet);
            
            try
            {
                using (var connection = GetSqlConnection(method.DeclaringType))
                {
                    // getting the dataset from the DB
                    connection.Open();

                    using (var dataReader = connection.ExecuteReader(dbParams, commandInfo.CommandText, commandInfo.CommandType))
                    {
                        // paging params routine
                        // setting paging params total count from output parameter if needed
                        var totalCountAssigned = false;
                        if (pagingParams != null && !getTotalCountFromResultSet)
                        {
                            pagingParams.TotalCount = (int)dbParams.GetParamValue("TotalCount");
                            totalCountAssigned = true;
                        }

                        // getting returning type info            
                        var resObject = Activator.CreateInstance<T>();
                        var resType = typeof(T);
                        var resProperties = resType.GetProperties();

                        // begin of darareader resluts loop
                        var datareaderIndex = 0;
                        do
                        {
                            // defining array element type
                            var prop = resProperties[datareaderIndex];
                            var propType = prop.PropertyType;

                            var isListType = (propType.IsGenericType && propType.GetGenericTypeDefinition() != typeof(Nullable<>)) || propType.IsArray;
                            var isComplexType = false;

                            Type listElementType = null;
                            PropertyInfo[] listElementProps = null;
                            IList list = null;

                            // list types
                            if (isListType)
                            {
                                if (propType.IsArray)
                                {
                                    listElementType = propType.GetElementType();
                                }
                                else
                                {
                                    var elementTypes = propType.GetGenericArguments();
                                    if (elementTypes != null && elementTypes.Length > 0)
                                    {
                                        listElementType = elementTypes[0];
                                    }
                                }

                                // creating list of runtime type
                                var listType = typeof(List<>);
                                var combinedListType = listType.MakeGenericType(listElementType);
                                list = (IList)Activator.CreateInstance(combinedListType);
                                listElementProps = listElementType.GetProperties();
                            }
                            else
                            {
                                // other types i.e. simple / enum / complex
                                isComplexType = !propType.IsEnum && !SIMPLE_TYPES.Contains(propType.Name);
                            }

                            // paging params routine
                            var isFirstPassed = false;

                            // begin of resultset loop
                            while (dataReader.Read())
                            {
                                // list types
                                if (isListType)
                                {
                                    // creating runtime object
                                    var listElementObj = Activator.CreateInstance(listElementType);

                                    // assigning properties' values
                                    foreach (var resObjProp in listElementProps)
                                    {
                                        if (DataReaderHasValue(dataReader, resObjProp.Name))
                                        {
                                            SetDBValue(listElementObj, resObjProp, dataReader[resObjProp.Name]);
                                        }
                                    }

                                    // adding to the list
                                    list.Add(listElementObj);

                                    // paging params routine
                                    // setting paging params total count from result set if needed
                                    if (pagingParams != null && getTotalCountFromResultSet && !totalCountAssigned && !isFirstPassed)
                                    {
                                        pagingParams.TotalCount = (int)dataReader["TotalCount"];

                                        totalCountAssigned = true;
                                    }
                                }

                                // other types
                                // getting the first record from resultset
                                else
                                {
                                    // other types i.e. simple / enum
                                    if (!isComplexType)
                                    {
                                        SetDBValue(resObject, prop, dataReader[0]);
                                    }
                                    // object types
                                    else
                                    {
                                        // getting the properties
                                        var resObjProps = propType.GetProperties();

                                        // creating runtime object
                                        var resObj = Activator.CreateInstance(propType);

                                        // assigning properties' values
                                        foreach (var resObjProp in resObjProps)
                                        {
                                            if (DataReaderHasValue(dataReader, resObjProp.Name))
                                            {
                                                SetDBValue(resObj, resObjProp, dataReader[resObjProp.Name]);
                                            }
                                        }

                                        // setting object property
                                        prop.SetValue(resObject, resObj, null);
                                    }
                                    // break the resultset
                                    break;
                                }

                                isFirstPassed = true;
                            }
                            // end of resultset loop

                            if (isListType)
                            {
                                if (propType.IsArray)
                                {
                                    // converting to array
                                    var arr = Array.CreateInstance(listElementType, list.Count);
                                    list.CopyTo(arr, 0);
                                    prop.SetValue(resObject, arr, null);
                                }
                                else
                                {
                                    prop.SetValue(resObject, list, null);
                                }
                            }

                            datareaderIndex++;
                        }
                        while (dataReader.NextResult());
                        // end of darareader resluts loop

                        return resObject;
                    } // using the datareader
                }
            }
            catch (DBException ex)
            {
                if (ex.IsUserException)
                {
                    throw new DBTaskUserException(ex.Message, ex, ex.UserExceptionCode);
                }

                throw new DBTaskException(GetExceptionMessage(commandInfo.CommandText, dbParams, ex.InnerException), ex);
            }
            catch (Exception ex)
            {
                throw new DBTaskException(GetExceptionMessage(commandInfo.CommandText, dbParams, ex.InnerException), ex);
            }
        }

        #endregion

        #region Protected Static Methods

        /// <summary>
        /// Set the given type object to the DB
        /// </summary>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        protected static int GetReturnValue(object parameters)
        {
            return GetReturnValue(new StackFrame(1).GetMethod(), parameters);
        }

        /// <summary>
        /// Gets an instanse of the requisted type from the DB
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected static T GetSingleObjectResult<T>(object parameters)
        {
            return GetSingleListResult<T>(new StackFrame(1).GetMethod(), parameters, null, false).SingleOrDefault();
        }

        /// <summary>
        /// Gets a list of the requested type objects from the DB
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected static IEnumerable<T> GetSingleListResult<T>(object parameters)
        {
            return GetSingleListResult<T>(new StackFrame(1).GetMethod(), parameters, null, false);
        }

        /// <summary>
        /// Gets a list of the requested type objects from the DB
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <param name="pagingParams"></param>
        /// <returns></returns>
        protected static IEnumerable<T> GetSingleListResult<T>(object parameters, DBPagingParams pagingParams)
        {
            return GetSingleListResult<T>(new StackFrame(1).GetMethod(), parameters, pagingParams, false);
        }

        /// <summary>
        /// Gets a list of the requested type objects from the DB
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <param name="pagingParams"></param>
        /// <param name="getTotalCountFromResultSet"></param>
        /// <returns></returns>
        protected static IEnumerable<T> GetSingleListResult<T>(object parameters, DBPagingParams pagingParams, bool getTotalCountFromResultSet)
        {
            return GetSingleListResult<T>(new StackFrame(1).GetMethod(), parameters, pagingParams, getTotalCountFromResultSet);
        }

        /// <summary>
        /// Gets the multiple list of a result set of the requested type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected static T GetMultipleListResult<T>(object parameters)
        {
            return GetMultipleListResult<T>(new StackFrame(1).GetMethod(), parameters, null, false);
        }

        /// <summary>
        /// Gets the multiple list of a result set of the requested type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <param name="pagingParams"></param>
        /// <returns></returns>
        protected static T GetMultipleListResult<T>(object parameters, DBPagingParams pagingParams)
        {
            return GetMultipleListResult<T>(new StackFrame(1).GetMethod(), parameters, pagingParams, false);
        }

        /// <summary>
        /// Gets the multiple list of a result set of the requested type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
        /// <param name="pagingParams"></param>
        /// <param name="getTotalCountFromResultSet"></param>
        /// <returns></returns>
        protected static T GetMultipleListResult<T>(object parameters, DBPagingParams pagingParams, bool getTotalCountFromResultSet)
        {
            return GetMultipleListResult<T>(new StackFrame(1).GetMethod(), parameters, pagingParams, getTotalCountFromResultSet);
        }

        #endregion
    }
}
