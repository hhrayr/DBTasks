using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace HUtils.DBTasks.DAL
{
    /// <summary>
    /// Represents base db param
    /// </summary>
    public abstract class BaseDBParam
    {
        #region Properties

        public string Name { get; private set; }
        public ParameterDirection Direction { get; private set; }
        public object Value { get; set; }
        public virtual Type ParamType { get { return Value != null ? Value.GetType() : null; } }

        #endregion

        #region .ctors

        public BaseDBParam(string name, ParameterDirection direction, object value)
        {
            Name = name;
            Direction = direction;
            Value = value;
        }

        public BaseDBParam(string name, object value)
        {
            Name = name;
            Value = value;
            Direction = ParameterDirection.Input;
        }

        #endregion
    }

    /// <summary>
    /// Simple type DB param
    /// </summary>
    public class SimpleDBParam : BaseDBParam
    {
        #region .ctors

        public SimpleDBParam(string name, ParameterDirection direction, object value)
            : base(name, direction, value)
        {
        }

        public SimpleDBParam(string name, object value)
            : base(name, value)
        {
        }

        #endregion
    }

    /// <summary>
    /// Structured type db param
    /// </summary>
    public class StructuredDBParam : BaseDBParam
    {
        #region Private Filds

        private Type _elementType = null;

        #endregion

        #region .ctors

        public StructuredDBParam(string name, ParameterDirection direction, IEnumerable<object> value)
            : base(name, direction, value)
        {
            var valType = value.GetType();

            if (valType.IsArray)
            {
                _elementType = valType.GetElementType();
            }
            else
            {
                var elementTypes = valType.GetGenericArguments();
                if (elementTypes != null && elementTypes.Length > 0)
                {
                    _elementType = elementTypes[0];
                }
            }
        }

        public StructuredDBParam(string name, IEnumerable<object> value)
            : this(name, ParameterDirection.Input, value)
        {
        }

        #endregion

        #region Overrides

        public override Type ParamType { get { return _elementType; } }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts to datatable
        /// </summary>
        /// <returns></returns>
        public DataTable ToDataTable()
        {
            var res = new DataTable();
            var props = ParamType.GetProperties();

            // adding columns              
            foreach (var prop in props)
            {
                var column = new DataColumn(prop.Name);
                column.DataType = prop.PropertyType;
                res.Columns.Add(column);
            }

            // adding rows
            foreach (var item in (IEnumerable<object>)Value)
            {
                var dr = res.NewRow();
                foreach (var prop in props)
                {
                    dr[prop.Name] = prop.GetValue(item, null);
                }
                res.Rows.Add(dr);
            }

            return res;
        }

        #endregion
    }

    /// <summary>
    /// Represents DB param collection functionality
    /// </summary>
    public class DBParamCollection : ICollection<BaseDBParam>
    {
        #region Private Fields

        private List<BaseDBParam> _list = new List<BaseDBParam>();

        #endregion

        #region Public Methods

        public void SetParamValue(string name, object value)
        {
            var param = _list.Single(p => p.Name == name
                && (p.Direction == ParameterDirection.Output
                    || p.Direction == ParameterDirection.InputOutput
                    || p.Direction == ParameterDirection.ReturnValue));

            param.Value = value;
        }

        public object GetParamValue(string name)
        {
            return _list.Single(p => p.Name == name).Value;
        }

        #endregion

        #region ICollection implementation

        public void Add(BaseDBParam item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(BaseDBParam item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(BaseDBParam[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(BaseDBParam item)
        {
            return _list.Remove(item);
        }

        public IEnumerator<BaseDBParam> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion
    }
}
