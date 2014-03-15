using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HUtils.DBTasks
{
    /// <summary>
    /// Represents connection string name
    /// </summary>
    public class ConnectionStringNameAttribute : Attribute
    {
        public string ConnectionStringName { get; set; }

        public ConnectionStringNameAttribute()
        {
        }

        public ConnectionStringNameAttribute(string connectionStringName)
        {
            ConnectionStringName = connectionStringName;
        }
    }
}
