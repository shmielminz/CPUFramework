using Microsoft.Data.SqlClient;
using System.Data;

namespace CPUFramework
{
    public class bizUsers : bizObject<bizUsers>
    {
        public void Login()
        {
            SqlCommand cmd = SQLUtility.GetSqlCommand("UserLogin");
            SQLUtility.SetParamValue(cmd, "UserName", Username);
            SQLUtility.SetParamValue(cmd, "Password", Password);
            this.Password = "";
            DataTable dt = SQLUtility.GetDataTable(cmd);
            if (dt.Rows.Count > 0)
            {
                this.LoadProps(dt.Rows[0]);
            }
        }

        public void LoadBySessionKey()
        {
            SqlCommand cmd = SQLUtility.GetSqlCommand(this.GetSprocName);
            SQLUtility.SetParamValue(cmd, "SessionKey", SessionKey);
            DataTable dt = SQLUtility.GetDataTable(cmd);
            if (dt.Rows.Count > 0)
            {
                this.LoadProps(dt.Rows[0]);
            }
        }

        public int UsersId { get; set; }
        public int RolesId { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string SessionKey { get; set; } = "";
        public string RoleName { get; set; } = "";
        public int RoleRank { get; set; }

    }
}
