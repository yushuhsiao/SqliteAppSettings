/*
    實作 Microsoft.Extensions.Configuration.IConfigurationSource 與 Microsoft.Extensions.Configuration.ConfigurationProvider
    由 db 取得設定值
*/
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SQLite;
using System.Threading;


namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceExtensions
    {
        public static IConfigurationBuilder AddSqliteAppSettings(this IConfigurationBuilder builder)
        {
            builder.Add(new SqliteAppSettingsConfigurationSource());
            return builder;
        }
    }
}
//namespace System
//{
//    public static partial class ServiceExtensions
//    {
//        public static TValue GetValue<TValue>(this IConfiguration configuration, [CallerMemberName] string name = null, params object[] index)
//            => AppSettingBinder.GetValue<TValue>(configuration, name);
//    }
//}

namespace Microsoft.Extensions.Configuration
{
    public class SqliteAppSettingsConfigurationSource : IConfigurationSource
    {
        IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
            => new SqliteAppSettingsConfigurationProvider();
    }

    public class SqliteAppSettingsConfigurationProvider : AppSettingBinder.Provider
    {
        private IServiceProvider _service;
        private object _sync = new object();

        public override void OnInit(IServiceProvider service)
        {
            Interlocked.CompareExchange(ref _service, service, null);
        }

        //private bool GetServiceProvider(out IServiceProvider service)
        //{
        //    service = Interlocked.CompareExchange(ref _service, null, null);
        //    return service != null;
        //}

        private void ReadData()
        {
            try
            {
                Data.Clear();
                foreach (var row in ConfigDb.Instance.ReadConfig())
                {
                    string key = AppSettingBinder.BuildKey(row.Key1, row.Key2, 0, 0);
                    //string key2;
                    //if (string.IsNullOrEmpty(row.Key1))
                    //    key2 = row.Key2;
                    //else
                    //    key2 = $"{row.Key1}:{row.Key2}";
                    Data[key] = row.Value;
                }
            }
            catch { }

        }

        private int version = ConfigDb.Instance.Version;

        public override bool TryGet(string key, out string value)
        {
            lock (_sync)
            {
                int ver = ConfigDb.Instance.Version;
                if (Data.Count == 0 || version != ver)
                {
                    ReadData();
                    version = ver;
                }
            }
            var result = base.TryGet(key, out value);
            return result;
        }

        public override void Set(string key, string value)
        {
            int n = key.LastIndexOf(':');
            if (n > 0)
            {
                string key1 = key.Substring(0, n);
                string key2 = key.Substring(n + 1);
                ConfigDb.Instance.UpdateRow(key1, key2, value);
            }
            else
            {
                ConfigDb.Instance.UpdateRow("", key, value);
            }
        }
    }
}