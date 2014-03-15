using System;

namespace HUtils.DBTasks
{
    /// <summary>
    /// Represents an attribute to ignor data contract property as input parameter
    /// </summary>
    public class DBContractAdvancedPropertyAttribute : Attribute
    {
        public DBContractAdvancedPropertyAttribute()
        {
            ExcludeFromParamList = true;            
        }

        public DBContractAdvancedPropertyAttribute(bool excludeFromParamList, bool excludeFromResultSet)
        {
            ExcludeFromParamList = excludeFromParamList;            
        }

        public bool ExcludeFromParamList { get; set; }        
    }
}
