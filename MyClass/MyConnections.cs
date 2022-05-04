using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;

namespace GLAU_Exam.MyClass
{
    public class MyConnections
    {
        public static MySqlConnection MyConnectionString(string type)
        {
            MySqlConnection m = new MySqlConnection();
            if (type == "able")
            {
                m.ConnectionString = ConfigurationManager.ConnectionStrings["able"].ConnectionString;
            }

         
            

            return m;
        }

        public static DataTable Select(string query, string connectionName)
        {
            MySqlDataAdapter da2 = new MySqlDataAdapter(query, MyConnectionString(connectionName));
            DataTable dt2 = new DataTable();
            da2.Fill(dt2);

            return dt2;
        }
        public static int DeleteInsertUpdate(string query, string connectionName)
        {
            MySqlDataAdapter da2 = new MySqlDataAdapter(query, MyConnectionString(connectionName));
            DataTable dt2 = new DataTable();
            int res = da2.Fill(dt2);
            return res;
        }
    }
}