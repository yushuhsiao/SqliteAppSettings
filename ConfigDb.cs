using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading;

namespace Leader.Services
{
    // sqlite commands
    // select * from sqlite_master;
    // pragma table_info('Config');

    public class ConfigDb
    {
        private object _sync = new object();
        public static string FileName = "data.db";
        public static string DataPath
        {
            get => SQLiteInit.DataPath;
            set => SQLiteInit.DataPath = value;
        }
        private static object init = new object();
        private static ConfigDb instance;
        public static ConfigDb Instance
        {
            get
            {
                lock (init)
                {
                    if (instance == null)
                        instance = new ConfigDb();
                    return instance;
                }
            }
        }


        public ConfigDb()
        {
            Init();
        }

        private void Init()
        {
            lock (_sync)
            {
                try
                {
                    // 自動建立資料庫並且建立需要的資料表
                    using (var db = _OpenDb())
                        db.InitSchema<ConfigDb>();
                }
                catch { }
            }
        }

        private SQLiteConnection _OpenDb() => SQLiteInit.OpenDb(FileName);

        public SQLiteConnection OpenConfigDb => Instance._OpenDb();

        public IEnumerable<IDbConnection> OpenDb()
        {
            lock (_sync)
                using (var db = this._OpenDb())
                    yield return db;
        }

        public void UpdateRow(string key1, string key2, string value) => UpdateRow(new Entity.ConfigRow { Key1 = key1, Key2 = key2, Value = value });

        public void UpdateRow(Entity.ConfigRow row)
        {
            string sql = $@"
INSERT INTO {TableName<Entity.ConfigRow>.Value} (Key1, Key2, Value)
VALUES (@Key1, @Key2, @Value)
ON CONFLICT(Key1, Key2) DO UPDATE SET Value = @Value";

            lock (_sync)
                using (var db = this._OpenDb())
                using (var tran = db.BeginTransaction())
                {
                    tran.Execute(sql, row);
                    tran.Commit();
                }
            UpdateVersion();
        }

        public Entity.ConfigRow UpdateRow(Entity.ConfigRow row, Entity.ConfigRow old = null)
        {
            if (row == null) return row;
            lock (_sync)
                using (var db = this._OpenDb())
                using (var tran = db.BeginTransaction())
                {
                    Entity.ConfigRow r;
                    if (old == null)
                    {
                        r = tran.QueryFirst<Entity.ConfigRow>($@"
INSERT INTO {TableName<Entity.ConfigRow>.Value}
       ( Key1, Key2, Value)
VALUES (@Key1,@Key2,@Value);
SELECT * FROM {TableName<Entity.ConfigRow>.Value}
WHERE Key1 = @Key1 AND Key2 = @Key2;", row);
                    }
                    else
                    {
                        r = tran.QueryFirst<Entity.ConfigRow>($@"UPDATE {TableName<Entity.ConfigRow>.Value} SET
    Key1 = @Key1,
    key2 = @Key2,
    Value = @Value
WHERE Key1 = @Key1_ AND Key2 = @Key2_ ;
SELECT * FROM {TableName<Entity.ConfigRow>.Value}
WHERE Key1 = @Key1 AND Key2 = @Key2 ;", new
                        {
                            Key1 = row.Key1,
                            Key2 = row.Key2,
                            Index1 = row.Index1,
                            Index2 = row.Index2,
                            Value = row.Value,
                            Key1_ = old.Key1,
                            Key2_ = old.Key2,
                            Index1_ = old.Index1,
                            Index2_ = old.Index2,
                        });
                    }
                    tran.Commit();
                    UpdateVersion();
                    return r;
                }
        }

        public void DeleteRow(Entity.ConfigRow row)
        {
            lock (_sync)
                using (var db = this._OpenDb())
                using (var tran = db.BeginTransaction())
                {
                    tran.Execute($"delete from {TableName<Entity.ConfigRow>.Value} where Key1=@Key1 and Key2=@Key2", row);
                    tran.Commit();
                }
            UpdateVersion();
        }

        public Entity.ConfigRow[] ReadConfig()
        {
            lock (_sync)
                using (var db = _OpenDb())
                    return db.Query<Entity.ConfigRow>($"select * from {TableName<Entity.ConfigRow>.Value} order by Key1 asc, Key2 asc").ToArray();
        }

        private int _version;
        public int Version
        {
            get => Interlocked.CompareExchange(ref _version, 0, 0);
            set => Interlocked.Exchange(ref _version, value);
        }

        private int UpdateVersion() => Interlocked.Increment(ref _version);
    }
}
namespace Leader.Entity
{
    [TableName("Config")]
    public class ConfigRow
    {
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public int Index1 { get; set; }
        public int Index2 { get; set; }
        public string Value { get; set; }

        public ConfigRow Clone() => (ConfigRow)base.MemberwiseClone();
    }
}
