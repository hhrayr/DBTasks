using System;

namespace HUtils.DBTasks
{
    /// <summary>
    /// Represents DB Task Exception thrown by the system
    /// </summary>
    public class DBTaskException : Exception
    {
        public DBTaskException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Represents DB Task Exception thrown by the user
    /// </summary>
    public class DBTaskUserException : DBTaskException
    {
        public int ErrorCode { get; private set; }

        public DBTaskUserException(string message, Exception innerException, int errorCode)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
