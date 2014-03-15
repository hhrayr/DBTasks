using System;

namespace HUtils.DBTasks.DAL
{
    /// <summary>
    /// Represents DB Connection functionality
    /// </summary>
    public static class DBConnection
    {
        /// <summary>
        /// Gets the db connection by the given connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IDBConnection GetDBConnection(string providerName, string connectionString)
        {
            var providerType = Type.GetType(providerName);
            if (providerType.GetInterface((typeof(IDBConnection)).FullName) != null)
            {
                var connection = (IDBConnection)Activator.CreateInstance(providerType);

                // calling init method
                connection.Init(connectionString);

                return connection;
            }

            throw new DBConnectionException("DB connection not found");
        }
    }

    public class DBConnectionException : Exception
    {
        public DBConnectionException(string message)
            : base(message)
        {
        }
    }
}
