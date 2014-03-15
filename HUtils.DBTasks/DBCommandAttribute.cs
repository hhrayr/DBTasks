using System;
using System.Data;

namespace HUtils.DBTasks
{
    /// <summary>
    /// Represents command type task method attribute
    /// </summary>
    public class DBCommandAttribute : Attribute
    {
        #region Private Fields

        private string _commandText;

        #endregion

        #region Virtual Methods
        
        /// <summary>
        /// Gets the command text
        /// </summary>
        /// <returns></returns>
        protected virtual string GetCommandText()
        {
            return _commandText;
        }

        #endregion

        #region Public Properties
        
        /// <summary>
        /// Gets/sets command type
        /// </summary>
        public CommandType CommandType { get; set; }

        /// <summary>
        /// Gets sets command text
        /// </summary>
        public string CommandText
        {
            get
            {
                return GetCommandText();
            }
            set
            {
                _commandText = value;
            }
        }

        #endregion
    }
}
