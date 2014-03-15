using System;
using System.Configuration;

namespace HUtils.DBTasks
{
    public class ConnectionStringsConfiguration : ConfigurationSection
    {
        #region Private Fileds

        private static ConnectionStringsConfiguration _instanse = ConfigurationManager.GetSection("connectionStringsConfiguration") as ConnectionStringsConfiguration;

        #endregion

        #region Properties

        [ConfigurationProperty("connectionStrings")]
        public ConnectionStringsCollection ConnectionStrings
        {
            get { return this["connectionStrings"] as ConnectionStringsCollection; }
        }

        [ConfigurationProperty("defaultConnectionStringName", IsRequired = true)]
        public string DefaultConnectionStringName
        {
            get { return this["defaultConnectionStringName"] as string; }
        }

        public ConnectionStringElement DefaultConnectionString
        {
            get { return ConnectionStrings.GetByName(DefaultConnectionStringName); }
        }

        public static ConnectionStringsConfiguration Instance
        {
            get { return _instanse; }
        }

        #endregion
    }

    public class ConnectionStringElement : ConfigurationElement
    {
        #region Properties

        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return this["name"] as string; }
        }

        [ConfigurationProperty("connectionString", IsRequired = true)]
        public string ConnectionString
        {
            get { return this["connectionString"] as string; }
        }

        [ConfigurationProperty("providerName", IsRequired = true)]
        public string ProviderName
        {
            get { return this["providerName"] as string; }
        }        

        #endregion
    }

    public class ConnectionStringsCollection : ConfigurationElementCollection
    {
        #region Indexer

        public ConnectionStringElement this[int index]
        {
            get
            {
                return base.BaseGet(index) as ConnectionStringElement;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }

        #endregion

        #region Overrides

        protected override ConfigurationElement CreateNewElement()
        {
            return new ConnectionStringElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConnectionStringElement)element).Name;
        }

        #endregion

        #region Public Methods

        public ConnectionStringElement GetByName(string name)
        {
            if (!String.IsNullOrEmpty(name))
            {
                foreach (ConnectionStringElement cstr in this)
                {
                    if (cstr.Name == name)
                    {
                        return cstr;
                    }
                }
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// Represents DB Task Configuration Exception thrown by the user
    /// </summary>
    public class DBTaskConfigurationException : Exception
    {
        public DBTaskConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
