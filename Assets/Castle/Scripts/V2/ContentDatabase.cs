using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Castle.V2
{
    [Serializable] public sealed class ContentRow
    {
        public string Table;
        public int Line;
        public string[] Headers;
        public string[] Values;
        public string Get(string key) { int i = Array.IndexOf(Headers, key); return i < 0 ? "" : Values[i]; }
        public string Id => Get("id");
        public string Name => Get("name");
        public string[] List(string key) => Get(key).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        public float Number(string key) => float.TryParse(Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    [CreateAssetMenu(menuName = "Castle/Content Database")]
    public sealed class ContentDatabase : ScriptableObject
    {
        public string Version = "example-v1";
        public List<ContentRow> Rows = new List<ContentRow>();
        public IEnumerable<ContentRow> Table(string table) => Rows.Where(r => r.Table == table);
        public ContentRow Find(string id) => Rows.FirstOrDefault(r => r.Id == id);
        public ContentRow Require(string id) => Find(id) ?? throw new InvalidOperationException("Unknown content: " + id);
        public ContentRow[] Plot(string group) => Table("Plot").Where(r => r.Get("group") == group).OrderBy(r => r.Number("order")).ToArray();
    }

    public static class ContentFormat
    {
        public static int Minute(string value)
        {
            var parts = value.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int hour) || !int.TryParse(parts[1], out int minute) || hour < 0 || hour > 24 || minute < 0 || minute > 59 || (hour == 24 && minute != 0)) throw new FormatException("时刻必须为 HH:mm（24:00 只作结束）");
            return hour * 60 + minute;
        }
        public static DateTime Date(string value) => DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        public static string Stamp(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        public static KeyValuePair<string, int> Grant(string value)
        {
            var parts = value.Split('*');
            return new KeyValuePair<string, int>(parts[0], parts.Length == 1 ? 1 : int.Parse(parts[1], CultureInfo.InvariantCulture));
        }
        public static string[] Answers(ContentRow row) => row.List("answer").Select(v => v.Contains("=") ? v.Substring(v.IndexOf('=') + 1) : v).ToArray();
    }
}
