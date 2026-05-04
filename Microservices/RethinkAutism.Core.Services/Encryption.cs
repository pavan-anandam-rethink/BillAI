using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace RethinkAutism.Core.Services
{
    public static class Encryption
    {
        private static System.Data.SqlClient.SqlConnection Conn
        {
            get
            {
                return
                    new System.Data.SqlClient.SqlConnection(
                        ConfigurationManager.ConnectionStrings["RethinkAutism"].ConnectionString);
            }
        }
        private  static Dictionary<string, List<string>> PropertyMaps { get; set; }
        public static void Encrypt<T>(List<T> target) where T : EncryptionBase
        {
            
            var DecryptedList = new List<DecryptedStringRow>();
            var PropertyMap = GetPropertyMap(typeof(T));
            var PropertyCount = PropertyMap.Count();
            foreach (var obj in target)
            {
                DecryptedList.AddRange(obj.GetDecryptedValues(PropertyMap));
            }
            var EncryptedList = EncryptList(DecryptedList);
            var y = 0;
            foreach (var obj in target)
            {
                obj.SetEncryptedValues(EncryptedList.GetRange(y, PropertyCount), PropertyMap);
                y += PropertyCount;
            }
        }
        public static void Decrypt<T>(List<T> target) where T : EncryptionBase
        {

            var EncryptedList = new List<EncryptedBinaryRow>();
            var PropertyMap = GetPropertyMap(typeof(T));
            var PropertyCount = PropertyMap.Count();
            foreach (var obj in target)
            {
                EncryptedList.AddRange(obj.GetEncryptedValues(PropertyMap));
            }
            var DecryptedList = DecryptList(EncryptedList);
            var y = 0;
            foreach (var obj in target)
            {
                obj.SetDecryptedValues(DecryptedList.GetRange(y, PropertyCount), PropertyMap);
                y += PropertyCount;
            }
        }
        internal static List<string> DecryptList(List<EncryptedBinaryRow> EncryptedList)
        {
            var st = new System.Data.DataTable("EncryptedBinaryRow");
            st.Columns.Add("Guid", typeof(Guid));
            st.Columns.Add("Binary", typeof(byte[]));
            foreach (var r in EncryptedList)
            {
                var nr = st.NewRow();
                nr["Guid"] = r.Guid;
                nr["Binary"] = r.Binary != null ? r.Binary.ToArray() : new byte[] { };
                st.Rows.Add(nr);
            }

            var cmd = new System.Data.SqlClient.SqlCommand("DecryptBinaryList", Conn);
            var Param = cmd.Parameters.AddWithValue("EncryptedList", st);
            Param.SqlDbType = System.Data.SqlDbType.Structured;
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            var da = new System.Data.SqlClient.SqlDataAdapter(cmd);
            var dt = new System.Data.DataTable();
            da.Fill(dt);
            var retList = new List<string>();
            foreach (System.Data.DataRow dr in dt.Rows)
            {

                retList.Add(dr["string"].GetType() != typeof(DBNull) ? (string)dr["string"] : "");
            }
            return retList;
        }
        internal static List<byte[]> EncryptList(List<DecryptedStringRow> DecryptedList)
        {
            var st = new System.Data.DataTable("DecryptedStringList");
            st.Columns.Add("Guid", typeof(System.Guid));
            st.Columns.Add("String", typeof(System.String));
            foreach (var r in DecryptedList)
            {
                var nr = st.NewRow();
                nr["Guid"] = r.Guid;
                nr["String"] = r.String;
                st.Rows.Add(nr);
            }
            var cmd = new System.Data.SqlClient.SqlCommand("EncryptStringList", Conn);
            var Param = cmd.Parameters.AddWithValue("DecryptedList", st);//DecryptedList.ToArray() );
            Param.SqlDbType = System.Data.SqlDbType.Structured;
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            var da = new System.Data.SqlClient.SqlDataAdapter(cmd);
            var dt = new System.Data.DataTable();
            da.Fill(dt);
            var retList = new List<byte[]>();
            foreach (System.Data.DataRow dr in dt.Rows)
            {
                var Binary = dr["Binary"].GetType() != typeof (DBNull)
                             ? (byte[])dr["Binary"]
                             : new byte[] { };
                retList.Add(Binary);
            }
            return retList;
        }
        internal static List<string> GetPropertyMap(Type Type)
        {
            if (PropertyMaps == null)
            {
                PropertyMaps = new Dictionary<string, List<string>>();
            }
            if (PropertyMaps.ContainsKey(Type.Name))
            {
                return PropertyMaps[Type.Name];
            }
            lock (PropertyMaps)
            {
                PropertyMaps.Add(Type.Name, GeneratePropertyMap(Type));
            }
            return PropertyMaps[Type.Name];
        }
        internal static List<string> GeneratePropertyMap(Type Type)
        {
            var PropertyMap = new List<string>();
            var Properties = Type.GetProperties();
            foreach (var p in Properties)
            {
                if (p.GetCustomAttributes(true).Any(a => a.GetType() == typeof(EncryptedProperty)))
                {
                    PropertyMap.Add(p.Name);
                }
            }
            return PropertyMap;
        }

    }

    internal class EncryptedBinaryRow
    {
        public Guid Guid { get; set; }
        public byte[] Binary { get; set; }
    }

    internal class DecryptedStringRow
    {
        public Guid Guid { get; set; }
        public String String { get; set; }
    }
}
