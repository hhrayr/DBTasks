using System;
using System.Data;

namespace HUtils.DBTasks.DAL
{
    /// <summary>
    /// Represents DB connection implementation contract
    /// </summary>
    public interface IDBConnection : IDisposable
    {
        /// <summary>
        /// Inits the connection by the given connection string
        /// </summary>
        /// <param name="connectionString"></param>
        void Init(string connectionString);

        /// <summary>
        /// Sets commad execute timeout
        /// </summary>
        /// <param name="value"></param>
        void SetCommandTimeout(int value);

        /// <summary>
        /// Opens connection
        /// </summary>
        void Open();

        /// <summary>
        /// Closes connection
        /// </summary>
        void Close();

        /// <summary>
        /// Executes non reader command
        /// </summary>
        /// <param name="paramCollection"></param>
        /// <param name="commandText"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        int ExecuteNonQuery(DBParamCollection paramCollection, string commandText, CommandType commandType);

        /// <summary>
        /// Executes datareader command
        /// </summary>
        /// <param name="paramCollection"></param>
        /// <param name="commandText"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        IDataReader ExecuteReader(DBParamCollection paramCollection, string commandText, CommandType commandType);
    }
}
