namespace System.Data.SQLite
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
