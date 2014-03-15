using System;

namespace HUtils.DBTasks.DAL
{
    /// <summary>
    /// Represents DB connection exception
    /// </summary>
    public class DBException : Exception
    {
        private int _userExceptionCode = -1;

        public DBException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DBException(string message, Exception innerException, int userExceptionCode)
            : base(message, innerException)
        {
            _userExceptionCode = userExceptionCode;
        }

        /// <summary>
        /// Indicates if the exception has been thrown by user
        /// </summary>
        public bool IsUserException
        {
            get { return _userExceptionCode != -1; }
        }

        public int UserExceptionCode
        {
            get { return _userExceptionCode; }
        }
    }
}
