using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;

class LocalUsageResult {
    public long? Tokens;
    public int MatchedSessions;
    public string Error;
}
class LocalUsageCounter {
    class FileSummary {
        public long Length;
        public DateTime Modified;
        public string SessionId,AccountId;
        public DateTime? FirstDay;
        public Dictionary<string,long> Days=new Dictionary<string,long>();
    }
    Dictionary<string,FileSummary> cache=new Dictionary<string,FileSummary>(StringComparer.OrdinalIgnoreCase);
    static JavaScriptSerializer Json(){return new JavaScriptSerializer {MaxJsonLength=4*1024*1024};}
    static Dictionary<string,object> Obj(Dictionary<string,object> d,string key){object v;return d!=null&&d.TryGetValue(key,out v)?v as Dictionary<string,object>:null;}
    static string Str(Dictionary<string,object> d,string key){object v;return d!=null&&d.TryGetValue(key,out v)?Convert.ToString(v):null;}
    static long? Num(Dictionary<string,object> d,string key){long value;return Int64.TryParse(Str(d,key),out value)?(long?)value:null;}
    static DateTimeOffset? Time(string value){DateTimeOffset parsed;return DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out parsed)?(DateTimeOffset?)parsed:null;}
    FileSummary Read(string path){
        var info=new FileInfo(path);
        FileSummary existing;
        if(cache.TryGetValue(path,out existing)&&existing.Length==info.Length&&existing.Modified==info.LastWriteTimeUtc)return existing;
        var result=new FileSummary{Length=info.Length,Modified=info.LastWriteTimeUtc};
        var json=Json();DateTimeOffset? created=null;long? previous=null;
        using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
        using(var reader=new StreamReader(stream,Encoding.UTF8,true)){
            string line;
            while((line=reader.ReadLine())!=null){
                if(line.IndexOf("\"session_meta\"",StringComparison.Ordinal)<0&&line.IndexOf("\"token_count\"",StringComparison.Ordinal)<0)continue;
                Dictionary<string,object> entry;try{entry=json.Deserialize<Dictionary<string,object>>(line);}catch{continue;}
                var payload=Obj(entry,"payload");
                if(Str(entry,"type")=="session_meta"){
                    result.SessionId=Str(payload,"id");result.AccountId=Str(payload,"creator_account_id");created=Time(Str(payload,"timestamp"));continue;
                }
                if(Str(entry,"type")!="event_msg"||Str(payload,"type")!="token_count")continue;
                var usage=Obj(payload,"info");long? total=Num(Obj(usage,"total_token_usage"),"total_tokens");
                if(!total.HasValue||total<0)continue;
                long? last=Num(Obj(usage,"last_token_usage"),"total_tokens");
                long delta=previous.HasValue&&total>=previous?total.Value-previous.Value:Math.Max(0,last??0);
                previous=total;
                var stamp=Time(Str(entry,"timestamp"));
                if(!stamp.HasValue||!created.HasValue||stamp.Value<created.Value)continue;
                string day=stamp.Value.LocalDateTime.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
                long old;result.Days.TryGetValue(day,out old);result.Days[day]=checked(old+delta);
            }
        }
        if(created.HasValue)result.FirstDay=created.Value.LocalDateTime.Date;cache[path]=result;return result;
    }
    public LocalUsageResult ReadToday(string accountId,IEnumerable<string> homes,DateTime today){return ReadDays(accountId,homes,today,1)[today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)];}
    public Dictionary<string,LocalUsageResult> ReadDays(string accountId,IEnumerable<string> homes,DateTime today,int count){
        count=Math.Max(1,Math.Min(31,count));var values=new Dictionary<string,Dictionary<string,long>>();for(int i=0;i<count;i++)values[today.AddDays(-i).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)]=new Dictionary<string,long>();
        bool failed=false;DateTime? first=null;var sessions=new HashSet<string>();
        if(!String.IsNullOrEmpty(accountId))
        foreach(var home in homes.Where(x=>!String.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)){
            foreach(var folder in new[]{"sessions","archived_sessions"}){
                string root=Path.Combine(home,folder);if(!Directory.Exists(root))continue;
                string[] files;try{files=Directory.GetFiles(root,"*.jsonl",SearchOption.AllDirectories);}catch{failed=true;continue;}
                foreach(string path in files){
                    try{
                        var summary=Read(path);
                        if(summary.AccountId!=accountId||String.IsNullOrEmpty(summary.SessionId))continue;
                        sessions.Add(summary.SessionId);if(summary.FirstDay.HasValue&&(!first.HasValue||summary.FirstDay<first))first=summary.FirstDay;
                        foreach(var row in values){long amount;summary.Days.TryGetValue(row.Key,out amount);long old;row.Value.TryGetValue(summary.SessionId,out old);row.Value[summary.SessionId]=Math.Max(old,amount);}
                    }catch{failed=true;}
                }
            }
        }
        var result=new Dictionary<string,LocalUsageResult>();foreach(var row in values){long? total=null;try{if(sessions.Count>0&&first.HasValue&&DateTime.ParseExact(row.Key,"yyyy-MM-dd",CultureInfo.InvariantCulture)>=first.Value)total=row.Value.Values.Sum();}catch(OverflowException){failed=true;}result[row.Key]=new LocalUsageResult{Tokens=total,MatchedSessions=sessions.Count,Error=String.IsNullOrEmpty(accountId)?"현재 계정을 확인하지 못했습니다.":failed?"일부 PC 기록을 읽지 못했습니다.":null};}return result;
    }
    public static IEnumerable<string> DefaultHomes(){
        return new[]{Environment.GetEnvironmentVariable("CODEX_HOME"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex")};
    }
}
