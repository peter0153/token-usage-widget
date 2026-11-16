using System;
using System.IO;
using System.Web.Script.Serialization;
class LocalUsageTests {
    static JavaScriptSerializer json=new JavaScriptSerializer();
    static DateTime today=DateTime.Today;
    static string Stamp(DateTime t){return new DateTimeOffset(DateTime.SpecifyKind(t,DateTimeKind.Local)).ToString("o");}
    static object Meta(string id,string account,DateTime created){return new {type="session_meta",payload=new {id=id,creator_account_id=account,timestamp=Stamp(created)}};}
    static object Event(DateTime when,long total,long last){return new {timestamp=Stamp(when),type="event_msg",payload=new {type="token_count",info=new {total_token_usage=new {total_tokens=total},last_token_usage=new {total_tokens=last}}}};}
    static void Write(string file,params object[] rows){using(var w=new StreamWriter(file))foreach(var row in rows)w.WriteLine(json.Serialize(row));}
    static void Assert(bool value,string what){if(!value)throw new Exception(what);}
    static void Main(string[] args){
        string root=Path.Combine(Path.GetFullPath(args[0]),"counter-"+Guid.NewGuid().ToString("N"));
        string sessions=Path.Combine(root,"sessions"),archive=Path.Combine(root,"archived_sessions");Directory.CreateDirectory(sessions);Directory.CreateDirectory(archive);
        string basic=Path.Combine(sessions,"basic.jsonl");
        Write(basic,Meta("basic","account-a",today.AddDays(-1).AddHours(20)),Event(today.AddMinutes(-1),100,100),Event(today.AddMinutes(1),150,50),Event(today.AddMinutes(2),150,50),Event(today.AddMinutes(3),180,30),Event(today.AddMinutes(4),20,20));
        File.Copy(basic,Path.Combine(archive,"duplicate.jsonl"));
        Write(Path.Combine(sessions,"fork.jsonl"),Meta("fork","account-a",today.AddHours(9)),Event(today.AddHours(8),150,150),Event(today.AddHours(9).AddMinutes(1),190,40));
        Write(Path.Combine(sessions,"other-account.jsonl"),Meta("other","account-b",today),Event(today.AddHours(1),900,900));
        Write(Path.Combine(sessions,"missing-baseline.jsonl"),Meta("reset","account-a",today),Event(today.AddHours(1),5000,17));
        File.AppendAllText(basic,"{partial\n");
        var counter=new LocalUsageCounter();var initial=counter.ReadToday("account-a",new[]{root},today);
        Assert(initial.Tokens==157,"Midnight, duplicates, forks, reset, account isolation or missing baseline failed.");
        Assert(initial.MatchedSessions==3,"Duplicate session counted twice.");
        Assert(counter.ReadToday("account-a",new[]{root,root},today).Tokens==157,"Cached scan or repeated root doubled usage.");
        File.AppendAllText(basic,json.Serialize(Event(today.AddMinutes(5),30,10))+Environment.NewLine);
        Assert(counter.ReadToday("account-a",new[]{root},today).Tokens==167,"New token usage did not accumulate.");
        Assert(counter.ReadToday("account-c",new[]{root},today).Tokens==null,"Unknown account was shown as zero.");
        Assert(counter.ReadToday("account-a",new[]{root},today.AddDays(1)).Tokens==0,"Date rollover carried yesterday into today.");
        var days=counter.ReadDays("account-a",new[]{root},today,10);Assert(days.Count==10&&days[today.ToString("yyyy-MM-dd")].Tokens==167&&days[today.AddDays(-1).ToString("yyyy-MM-dd")].Tokens==100,"Historical range changed account isolation or per-day deltas.");Assert(days[today.AddDays(-9).ToString("yyyy-MM-dd")].Tokens==null,"History before first recorded session was invented as zero.");
        Console.WriteLine("PASS: day boundary, repeated events, fork history, counter reset, account isolation, archive deduplication, live append, missing records and date rollover.");
    }
}
