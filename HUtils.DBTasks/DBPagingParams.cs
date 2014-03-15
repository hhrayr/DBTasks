namespace HUtils.DBTasks
{
    /// <summary>
    /// Represents the paging params
    /// </summary>
    public class DBPagingParams
    {
        #region Nested Types

        /// <summary>
        /// Represents the set of sort directions
        /// </summary>
        public enum SortColumnDirection : byte
        {
            Ascending = 0,
            Descending = 1
        }

        #endregion

        /// <summary>
        /// Gets or sets the 0 - based page index
        /// </summary>
        public int PageIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the page size
        /// </summary>
        public int PageSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the sort column name (default if empty)
        /// </summary>
        public string SortColumn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the sort direction (aplicable id SortColumn is not empty)
        /// </summary>
        public SortColumnDirection SortDirection
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the total count
        /// </summary>
        public long TotalCount
        {
            get;
            set;
        }
    }
}
