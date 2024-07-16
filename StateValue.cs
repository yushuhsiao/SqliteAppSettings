/*
    實作 Microsoft.Extensions.Configuration.IConfigurationSource 與 Microsoft.Extensions.Configuration.ConfigurationProvider
    由 db 取得設定值
*/
using Dapper;
using Leader.Services;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;


namespace Leader.Services
{
    public interface IStateValue
    {
        int this[string name] { get; set; }
        int this[string name, int defaultValue = 0] { get; set; }
        decimal this[string name, decimal defaultValue = 0] { get; set; }
        string this[string name, string defaultValue = null] { get; set; }

        int this[object prefix, string name] { get; set; }
        int this[object prefix, string name, int defaultValue = 0] { get; set; }
        decimal this[object prefix, string name, decimal defaultValue = 0] { get; set; }
        string this[object prefix, string name, string defaultValue = null] { get; set; }
    }

    internal class StateValue : IStateValue
    {
        private readonly Dictionary<string, StateRow> values;

        public StateValue()
        {
            foreach (var db in ConfigDb.Instance.OpenDb())
                values = db.Query<StateRow>($"SELECT * FROM {TableName<StateRow>.Value};").ToDictionary(x => x.Name);
        }

        public int this[string name]
        {
            get => GetRow(name, false)?.ValueInt ?? 0;
            set => SetRow(name, value);
        }
        public int this[string name, int defaultValue = 0]
        {
            get => GetRow(name, false)?.ValueInt ?? defaultValue;
            set => SetRow(name, value);
        }
        public decimal this[string name, decimal defaultValue = 0]
        {
            get => GetRow(name, false)?.ValueReal ?? defaultValue;
            set => SetRow(name, value);
        }
        public string this[string name, string defaultValue = null]
        {
            get => GetRow(name, false)?.ValueText ?? defaultValue;
            set => SetRow(name, value);
        }

        public int this[object prefix, string name]
        {
            get => GetRow(MakeName(prefix, name), false)?.ValueInt ?? 0;
            set => SetRow(MakeName(prefix, name), value);
        }
        public int this[object prefix, string name, int defaultValue = 0]
        {
            get => GetRow(MakeName(prefix, name), false)?.ValueInt ?? defaultValue;
            set => SetRow(MakeName(prefix, name), value);
        }
        public decimal this[object prefix, string name, decimal defaultValue = 0]
        {
            get => GetRow(MakeName(prefix, name), false)?.ValueReal ?? defaultValue;
            set => SetRow(MakeName(prefix, name), value);
        }
        public string this[object prefix, string name, string defaultValue = null]
        {
            get => GetRow(MakeName(prefix, name), false)?.ValueText ?? defaultValue;
            set => SetRow(MakeName(prefix, name), value);
        }

        private string MakeName(object prefix, string name) { if (prefix == null) return name; else return $"{prefix.GetType().FullName}.{name}"; }

        private StateRow GetRow(string name, bool create)
        {
            lock (values)
            {
                if (values.TryGetValue(name, out var row))
                    return row;
                if (create)
                    return values[name] = new StateRow() { Name = name };
            }
            return null;
        }

        private bool UpdateRow(string field, StateRow row)
        {
            foreach (var db in ConfigDb.Instance.OpenDb())
            {
                using (var tran = db.BeginTransaction())
                {
                    try
                    {
                        int cnt = tran.Execute($@"INSERT INTO {TableName<StateRow>.Value} (Name, {field})
VALUES (@Name, @{field})
ON CONFLICT(Name) DO UPDATE SET {field} = @{field} WHERE Name=@Name", row);
                        if (cnt == 1)
                        {
                            tran.Commit();
                            return true;
                        }
                    }
                    catch { }
                    tran.Rollback();
                }
            }
            return false;
        }

        private void SetRow(string name, int value)
        {
            StateRow row;
            int old;
            lock (values)
            {
                row = GetRow(name, true);
                if (row.ValueInt == value) return;
                old = row.ValueInt;
                row.ValueInt = value;
            }
            if (UpdateRow(nameof(row.ValueInt), row)) return;
            lock (values)
                row.ValueInt = old;
        }

        private void SetRow(string name, decimal value)
        {
            StateRow row;
            decimal old;
            lock (values)
            {
                row = GetRow(name, true);
                if (row.ValueReal == value) return;
                old = row.ValueReal;
                row.ValueReal = value;
            }
            if (UpdateRow(nameof(row.ValueReal), row)) return;
            lock (values)
                row.ValueReal = old;
        }

        private void SetRow(string name, string value)
        {
            StateRow row;
            string old;
            lock (values)
            {
                row = GetRow(name, true);
                if (row.ValueText == value) return;
                old = row.ValueText;
                row.ValueText = value;
            }
            if (UpdateRow(nameof(row.ValueText), row)) return;
            lock (values)
                row.ValueText = old;
        }

        [TableName("State")]
        private class StateRow
        {
            public string Name { get; set; }
            public int ValueInt { get; set; }
            public decimal ValueReal { get; set; }
            public string ValueText { get; set; }
        }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceExtensions
    {
        public static IServiceCollection AddStateCounter(this IServiceCollection services)
        {
            services.AddSingleton<IStateValue, StateValue>();
            return services;
        }
    }
}
