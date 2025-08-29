using Dapper;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    public struct SqliteDateTime : IFormattable
    {
        public DateTime Value { get; set; }

        public static implicit operator SqliteDateTime(DateTime value) => new SqliteDateTime { Value = value };
        public static implicit operator DateTime(SqliteDateTime value) => value.Value;

        public override string ToString() => Value.ToString();
        string IFormattable.ToString(string format, IFormatProvider formatProvider) => Value.ToString(format, formatProvider);

        //public string ToString([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format) => Value.ToString(format);

        public string ToString(IFormatProvider provider) => Value.ToString(provider);

        //public string ToString([StringSyntax(StringSyntaxAttribute.DateTimeFormat)] string format, IFormatProvider provider) => Value.ToString(format, provider);

        //bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider) => Value.TryFormat(destination, out charsWritten, format, provider);



        class _TypeHandler : SqlMapper.TypeHandler<SqliteDateTime>
        {
            public override SqliteDateTime Parse(object value)
            {
                try
                {
                    if (value is long _value)
                        return new SqliteDateTime { Value = DateTimeOffset.FromUnixTimeMilliseconds(_value).LocalDateTime };
                }
                catch { }
                return default;
            }

            public override void SetValue(IDbDataParameter parameter, SqliteDateTime value)
            {
                try
                {
                    parameter.Value = new DateTimeOffset(value.Value).ToUnixTimeMilliseconds();
                    parameter.DbType = DbType.Int64;
                }
                catch
                {
                    parameter.Value = value;
                }
            }
        }

        public static void AddTypeHandler() => SqlMapper.AddTypeHandler(new _TypeHandler());

    }
}