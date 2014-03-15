using System;

namespace HUtils.DBTasks
{
    /// <summary>
    /// Represents an attribute to specify that the data contract property is mandatory
    /// </summary>
    public class DBContractMandatoryPropertyAttribute : Attribute
    {
        public DBContractMandatoryPropertyAttribute()
        {
            MandatoryParameter = true;
        }

        public DBContractMandatoryPropertyAttribute(bool mandatoryParameter)
        {
            MandatoryParameter = MandatoryParameter;
        }

        public bool MandatoryParameter { get; set; }
    }
}
