using System.Data;
using System.Data.SqlClient;

namespace HUtils.DBTasks.DAL
{
    /// <summary>
    /// Represents SQL db connection
    /// </summary>
    public class SQLDBConnection : IDBConnection
    {
        #region Private Fields

        private SqlConnection _sqlConnection;                
        private int _commandTimeout = 30; // default command timeout
        private bool _isDisposed = false;

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets sql command by the given params and assign parameters
        /// </summary>
        /// <param name="paramCollection"></param>
        /// <param name="commandText"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        private SqlCommand GetCommand(DBParamCollection paramCollection, string commandText, CommandType commandType)
        {
            if (_sqlConnection == null)
            {
                throw new DBConnectionException("DBConnection is not initialized");
            }

            var command = _sqlConnection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;

            // adding the params
            foreach (var param in paramCollection)
            {
                var commandParam = new SqlParameter();
                commandParam.ParameterName = param.Name;
                commandParam.Direction = param.Direction;

                // setting the value
                if (param is StructuredDBParam)
                {
                    commandParam.SqlDbType = SqlDbType.Structured;
                    commandParam.Value = ((StructuredDBParam)param).ToDataTable();
                }
                else
                {
                    commandParam.Value = param.Value;
                }

                command.Parameters.Add(commandParam);
            }

            // setting the command timeout
            command.CommandTimeout = _commandTimeout;

            return command;
        }

        /// <summary>
        /// Sets output parameters' values of the given sql command to the given db param collection
        /// </summary>
        /// <param name="command"></param>
        /// <param name="paramCollection"></param>
        private static void SetOutputValues(SqlCommand command, DBParamCollection paramCollection)
        {
            foreach (SqlParameter commandParam in command.Parameters)
            {
                if (commandParam.Direction == ParameterDirection.Output
                    || commandParam.Direction == ParameterDirection.InputOutput
                    || commandParam.Direction == ParameterDirection.ReturnValue)
                {
                    paramCollection.SetParamValue(commandParam.ParameterName, commandParam.Value);
                }
            }
        }
        
        #endregion

        #region IDBConnection Implementation

        public void Init(string connectionString)
        {
            _sqlConnection = new SqlConnection(connectionString);
        }

        public void SetCommandTimeout(int value)
        {
            _commandTimeout = value;
        }

        public void Open()
        {
            if (_sqlConnection == null)
            {
                throw new DBConnectionException("DBConnection is not initialized");
            }

            if (_sqlConnection.State == ConnectionState.Closed)
            {
                _sqlConnection.Open();
            }
        }

        public void Close()
        {
            if (_sqlConnection == null)
            {
                throw new DBConnectionException("DBConnection is not initialized");
            }

            if (_sqlConnection.State == ConnectionState.Open)
            {
                _sqlConnection.Close();
            }
        }

        public int ExecuteNonQuery(DBParamCollection paramCollection, string commandText, CommandType commandType)
        {
            var command = GetCommand(paramCollection, commandText, commandType);
            try
            {
                var res = command.ExecuteNonQuery();
                SetOutputValues(command, paramCollection);
                return res;
            }
            catch (SqlException ex)
            {
                if (ex.Number >= 50000)
                {
                    throw new DBException(ex.Message, ex, ex.State);
                }

                throw new DBException(ex.Message, ex);
            }
        }

        public IDataReader ExecuteReader(DBParamCollection paramCollection, string commandText, CommandType commandType)
        {
            var command = GetCommand(paramCollection, commandText, commandType);
            try
            {
                var res = command.ExecuteReader();
                SetOutputValues(command, paramCollection);
                return res;
            }
            catch (SqlException ex)
            {
                if (ex.Number >= 50000)
                {
                    throw new DBException(ex.Message, ex, ex.State);
                }

                throw new DBException(ex.Message, ex);
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Close();
                _sqlConnection.Dispose();
                _isDisposed = true;
            }
        }

        #endregion
    }
}
