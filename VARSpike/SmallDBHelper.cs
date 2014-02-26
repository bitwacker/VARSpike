using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace VARSpike
{
    public static class SmallDBHelper
    {
        public class SqlText
        {
            public SqlText(string text)
            {
                Text = text;
            }

            public string Text { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        public static string SafeStringFormat(string stringFormat, object[] args)
        {
            if (stringFormat == null) return null;
            if (args == null) return stringFormat;
            if (args.Length == 0) return stringFormat;
            return String.Format(stringFormat, args);
        }

        public static object[] Escape(object[] args)
        {
            if (args == null) return null;
            if (args.Length == 0) return args;

            var clone = new object[args.Length];
            for (int cc = 0; cc < args.Length; cc++)
            {
                clone[cc] = Escape(args[cc]);
            }
            return clone;
        }


        public static object AdaptForDB(object value)
        {
            if (value == null) return null;

            //var idbase = value as IdBase;
            //if (idbase != null)
            //{
            //    return idbase.Key;
            //}

            //var idcode = value as IdCodeBase;
            //if (idcode != null)
            //{
            //    return idcode.Code;
            //}

            return value;
        }

        /// <summary>
        /// For safe date comparisons in sql use ISO 8601 date formats
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Make8601LongFormat(DateTime input)
        {
            return input.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo);
        }

        public static string Escape(object arg)
        {
            if (arg == null) return "NULL";

            arg = AdaptForDB(arg);



            if (arg is SqlText)
            {
                // Don't escape
                return arg.ToString();
            }
            if (arg is DateTime || arg is DateTime?)
            {
                var dt = (DateTime)arg;
                if (dt == DateTime.MinValue) return "NULL";
                return "'" + Make8601LongFormat((DateTime)arg) + "'";
            }
            if (arg is Enum)
            {
                return ((int)arg).ToString();
            }
            if (arg is bool)
            {
                return (bool)arg ? "1" : "0";
            }
            var toStr = arg.ToString();
            var escaped = toStr.Replace("'", "''");
            return "N'" + escaped + "'";
        }

        public static List<T> ExecuteQuery<T>(string connectionString, Func<SqlDataReader, T> readRow, string sql, params object[] sqlParams)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");

            var sqlText = SafeStringFormat(sql, Escape(sqlParams));
           
           
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    using (var command = conn.CreateCommand())
                    {
                    
                        command.CommandText = sqlText;
                        using (var reader = command.ExecuteReader())
                        {
                            var result = new List<T>();
                            while (reader.Read())
                            {
                                result.Add(readRow(reader));
                            }
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("SQL Failed: " + sqlText, ex);
            }
        }

        public static T ExecuteQueryScalar<T>(string connectionString, string sql, params object[] sqlParams)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");

            var sqlText = SafeStringFormat(sql, Escape(sqlParams));


            try
            {
                using (var conn = new SqlConnection(connectionString))
                {

                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = sqlText;
                        var res = command.ExecuteScalar();
                        try
                        {
                            return (T)res;
                        }
                        catch (Exception)
                        {
                            throw new Exception(string.Format("Expected {0}, but got {1}", typeof(T), res.GetType()));
                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("SQL Failed: " + sqlText, ex);
            }
        }

        public static string BuildConnectionString(string dbName, string server = "localhost")
        {
            return string.Format("Server={1};Database={0};Trusted_Connection=True;", dbName, server);
        }
    }
}
