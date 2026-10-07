using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Castle.V2
{
    public static class CsvContent
    {
        // RFC 4180：字段内逗号、双引号和换行均保留；诊断使用源文件物理行号。
        public static List<ContentRow> Parse(string table, string text)
        {
            var records = new List<KeyValuePair<int, string[]>>();
            var fields = new List<string>(); var value = new StringBuilder();
            bool quoted = false, closed = false; int line = 1, start = 1;
            text = text.TrimStart('\uFEFF');
            for (int i = 0; i <= text.Length; i++)
            {
                char c = i == text.Length ? '\n' : text[i];
                if (quoted)
                {
                    if (i == text.Length) throw new FormatException(table + ":" + start + " 未闭合引号");
                    if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { value.Append('"'); i++; } else { quoted = false; closed = true; } }
                    else { value.Append(c); if (c == '\n') line++; }
                    continue;
                }
                if (c == '"' && value.Length == 0 && !closed) { quoted = true; continue; }
                if (c == ',' || c == '\r' || c == '\n')
                {
                    fields.Add(value.ToString()); value.Clear(); closed = false;
                    if (c == ',') continue;
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    if (fields.Any(f => f.Length > 0)) records.Add(new KeyValuePair<int, string[]>(start, fields.ToArray()));
                    fields.Clear(); line++; start = line; continue;
                }
                if (closed) throw new FormatException(table + ":" + line + " 引号结束后只能跟分隔符");
                if (c == '"') throw new FormatException(table + ":" + line + " 字段中的引号必须转义");
                value.Append(c);
            }
            if (records.Count == 0) throw new FormatException(table + ":1 缺少表头");
            var headers = records[0].Value.Select(s => s.Trim()).ToArray();
            if (headers.Distinct().Count() != headers.Length) throw new FormatException(table + ":1 重复表头");
            return records.Skip(1).Select(r =>
            {
                if (r.Value.Length != headers.Length) throw new FormatException(table + ":" + r.Key + " 字段数量与表头不符");
                return new ContentRow { Table = table, Line = r.Key, Headers = headers, Values = r.Value };
            }).ToList();
        }

        public static readonly Dictionary<string, string> Schema = new Dictionary<string, string>
        {
            { "Room", "id,name,floor,start,end,need" },
            { "Event", "id,name,room,trigger,target,start,end,need,plot,time,reward,use,goal,plan_b,action,record" },
            { "Plot", "id,group,order,speaker,text,at,anchor,link_entry" },
            { "Record", "id,name,device,start,duration,plot,need" },
            { "Entry", "id,name,type,text,icon,owner,record,source" },
            { "Puzzle", "id,name,type,need,target,map,slots,candidates,answer,reference,success,feedback,fail_text" }
        };
    }
}
