﻿using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CPUFramework
{
    public class bizObject<T> : INotifyPropertyChanged where T : bizObject<T>, new()
    {
        string _typname = ""; string _tablename = ""; string _getsproc = ""; string _updatesproc = ""; string _deletesproc = "";
        string _primarykeyname = ""; string _primarykeyparamname = "";
        DataTable _datatable = new();
        List<PropertyInfo> _properties = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public bizObject()
        {
            Type t = this.GetType();
            _typname = t.Name;
            _tablename = _typname;
            if (_tablename.ToLower().StartsWith("biz")) { _tablename = _tablename.Substring(3); }
            _getsproc = _tablename + "Get";
            _updatesproc = _tablename + "Update";
            _deletesproc = _tablename + "Delete";
            _primarykeyname = _tablename + "Id";
            _primarykeyparamname = "@" + _primarykeyname;
            _properties = t.GetProperties().ToList();
        }
        public DataTable Load(int primarykeyvalue)
        {
            DataTable dt;
            SqlCommand cmd = SQLUtility.GetSqlCommand(_getsproc);
            SQLUtility.SetParamValue(cmd, _primarykeyparamname, primarykeyvalue);
            dt = SQLUtility.GetDataTable(cmd);
            if (dt.Rows.Count > 0)
            {
                LoadProps(dt.Rows[0]);
            }
            foreach (DataColumn col in dt.Columns) col.ReadOnly = false;
            _datatable = dt;
            return dt;
        }

        public List<T> GetList(bool includeblank = false)
        {
            SqlCommand cmd = SQLUtility.GetSqlCommand(_getsproc);
            SQLUtility.SetParamValue(cmd, "@All", 1);
            SQLUtility.SetParamValue(cmd, "@IncludeBlank", includeblank);
            var dt = SQLUtility.GetDataTable(cmd);
            return GetListFromDataTable(dt);
        }

        public List<T> GetListThatHaveChildRecords(bool includeblank = false)
        {
            SqlCommand cmd = SQLUtility.GetSqlCommand(_getsproc);
            SQLUtility.SetParamValue(cmd, "@List", 1);
            SQLUtility.SetParamValue(cmd, "@IncludeBlank", includeblank);
            var dt = SQLUtility.GetDataTable(cmd);
            return GetListFromDataTable(dt);
        }

        protected List<T> GetListFromDataTable(DataTable dt)
        {
            List<T> lst = new();
            foreach (DataRow dr in dt.Rows)
            {
                T obj = new();
                obj.LoadProps(dr);
                lst.Add(obj);
            }
            return lst;
        }

        protected void LoadProps(DataRow dr)
        {
            foreach (DataColumn col in dr.Table.Columns)
            {
                SetProp(col.ColumnName, dr[col.ColumnName]);
            }
        }

        public void Delete(int id)
        {
            this.ErrorMessage = "";
            SqlCommand cmd = SQLUtility.GetSqlCommand(_deletesproc);
            SQLUtility.SetParamValue(cmd, _primarykeyparamname, id);
            SQLUtility.ExecuteSql(cmd);
        }

        public void Delete()
        {
            this.ErrorMessage = "";
            PropertyInfo? prop = GetProp(_primarykeyname, true, false);
            if (prop != null)
            {
                object? id = prop.GetValue(this);
                if (id != null)
                {
                    this.Delete((int)id);
                }
            }
        }

        public void Delete(DataTable datatable)
        {
            int id = (int)datatable.Rows[0][_primarykeyname];
            this.Delete(id);
        }

        public void Save()
        {
            this.ErrorMessage = "";
            SqlCommand cmd = SQLUtility.GetSqlCommand(_updatesproc);
            foreach (SqlParameter param in cmd.Parameters)
            {
                var prop = GetProp(param.ParameterName, true, false);
                if (prop != null)
                {
                    object? val = prop.GetValue(this);
                    if (val == null) { val = DBNull.Value; }
                    param.Value = val;
                }
            }
            SQLUtility.ExecuteSql(cmd);
            foreach (SqlParameter param in cmd.Parameters)
            {
                if (param.Direction == ParameterDirection.InputOutput)
                {
                    SetProp(param.ParameterName, param.Value);
                }
            }
        }

        public void Save(DataTable datatable)
        {
            this.ErrorMessage = "";
            if (datatable.Rows.Count == 0)
            {
                throw new Exception($"Cannot call {_tablename} Save method, there were no rows returned from table.");
            }
            DataRow r = datatable.Rows[0];

            SQLUtility.SaveDataRow(r, _updatesproc);
        }

        private PropertyInfo? GetProp(string propname, bool forread, bool forwrite)
        {
            propname = propname.ToLower();
            if (propname.StartsWith("@")) { propname = propname.Substring(1); }
            PropertyInfo? prop = _properties.FirstOrDefault(p =>
            p.Name.ToLower() == propname
            && (!forread || p.CanRead)
            && (!forwrite || p.CanWrite)
            );
            return prop;
        }

        private void SetProp(string propname, object? value)
        {
            var prop = GetProp(propname, false, true);
            if (prop != null)
            {
                if (value == DBNull.Value) { value = null; }
                try
                {
                    prop.SetValue(this, value);
                }
                catch (Exception ex)
                {
                    string msg = $"{_typname}.{prop.Name} is being set to {value} and that is wrong data type. {ex.Message}";
                    throw new CPUDevException(msg, ex);
                }
            }
        }

        protected string GetSprocName { get => _getsproc; }

        protected void InvokePropertyChanged([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        public string ErrorMessage { get; set; } = "";
    }
}
