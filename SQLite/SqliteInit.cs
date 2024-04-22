using Dapper;
using System.IO;
using System.Reflection;

namespace System.Data.SQLite
{
    public static class SQLiteInit
    {
        /// <summary>
        /// 從指定的 assembly 取得初始化 script
        /// </summary>
        /// <param name="db"></param>
        /// <param name="assembly"></param>
        public static void InitSchema(this SQLiteConnection db, Assembly assembly, Func<string, bool> expression = null)
        {
            var res_names = assembly.GetManifestResourceNames();
            expression = expression ?? DefaultExpression;
            foreach (var res_name in res_names)
                if (expression(res_name))
                    Init(db, assembly, res_name);
        }

        public static void InitSchema<T>(this SQLiteConnection db, Func<string, bool> expression = null) => InitSchema(db, typeof(T).Assembly, expression);

        private static bool DefaultExpression(string res_name) => res_name.EndsWith(".sql");

        public static void Init(SQLiteConnection db, Assembly asm, string res_name)
        {
            try
            {
                string[] sql;
                using (Stream r1 = asm.GetManifestResourceStream(res_name))
                using (StreamReader r2 = new StreamReader(r1))
                    sql = r2.ReadToEnd().Split(';');
                foreach (string sql1 in sql)
                {
                    try
                    {
                        var sql2 = sql1.Trim();
                        if (string.IsNullOrEmpty(sql2)) continue;
                        int n = db.Execute(sql2 + ";");
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static SQLiteConnection OpenDb(string filename, string basedir = null)
        {
            string path;
            if (basedir == null)
                path = Path.Combine(Environment.CurrentDirectory, "Data", filename);
            else
                path = Path.Combine(basedir, filename);
            string cn = $"URI=file:{path};";
            SQLiteConnection conn = new SQLiteConnection(cn, parseViaFramework: true);
            conn.Open();
            return conn;
        }
    }
}
















