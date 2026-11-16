using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

class Snapshot {
    public double? ShortUsed, WeekUsed;
    public long? ShortReset, WeekReset;
    public int? ShortMinutes, WeekMinutes;
    public string Plan, UsageError;
    public string AccountId,LocalUsageError;
    public long? TodayLocalTokens;
    public int LocalMatchedSessions;
    public Dictionary<string,long?> DailyTokens=new Dictionary<string,long?>();
    public Dictionary<string,long?> LocalDailyTokens=new Dictionary<string,long?>();
    public string HistoryIdentity;public int? ResetCredits;public DateTime CreditsUpdated;public string CreditsError;
    public DateTime Updated;
}
class Rpc : IDisposable {
    Process process;
    StreamWriter input;
    JavaScriptSerializer json = new JavaScriptSerializer();
    int id;
    bool disposed;
    static void Trace(string message){try{var path=Path.Combine(AppPaths.Data,"connection.log");if(File.Exists(path)&&new FileInfo(path).Length>64000)File.WriteAllText(path,"");File.AppendAllText(path,DateTime.Now.ToString("HH:mm:ss ")+message+Environment.NewLine);}catch{}}
    public Rpc(string accountHome=null) {
        string exe=AppPaths.Server;
        process = new Process { StartInfo = new ProcessStartInfo(exe, "--listen stdio://") {
            UseShellExecute=false, CreateNoWindow=true, RedirectStandardInput=true,
            RedirectStandardOutput=true, RedirectStandardError=true,
            StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=new UTF8Encoding(false),
            WorkingDirectory=AppPaths.Data
        }};
        process.StartInfo.EnvironmentVariables["CODEX_HOME"]=accountHome??AppPaths.Account;
        foreach(string name in new[]{"OPENAI_API_KEY","CODEX_API_KEY","CODEX_ACCESS_TOKEN","CODEX_INTERNAL_ORIGINATOR_OVERRIDE"})process.StartInfo.EnvironmentVariables.Remove(name);
        process.ErrorDataReceived += delegate {};
        process.Start(); process.BeginErrorReadLine();
        input=new StreamWriter(process.StandardInput.BaseStream,new UTF8Encoding(false));
        input.NewLine="\n";input.AutoFlush=true;
        Trace("Started "+exe+" pid="+process.Id);
    }
    void Send(object value) {input.WriteLine(json.Serialize(value));input.Flush();}
    public async Task<Dictionary<string,object>> Call(string method, object args=null) {
        int request=++id;
        var message=new Dictionary<string,object>{{"id",request},{"method",method}};
        message["params"]=args??new Dictionary<string,object>();
        Send(message);
        Trace("Request "+request+" "+method);
        var timeout=Task.Delay(25000);
        while(true) {
            var read=process.StandardOutput.ReadLineAsync();
            if(await Task.WhenAny(read,timeout)!=read) throw new Exception(method+" 조회 시간 초과");
            string line=await read;
            if(line==null) throw new Exception("Codex 연결 종료");
            Dictionary<string,object> data;
            try {data=json.Deserialize<Dictionary<string,object>>(line);}catch {Trace("Ignored non-JSON output ("+line.Length+" characters)");continue;}
            Trace("Received id="+(data.ContainsKey("id")?Convert.ToString(data["id"]):"notification"));
            if(data.ContainsKey("method") && data.ContainsKey("id")) {
                Send(new {id=data["id"],error=new {code=-32601,message="Read-only widget does not handle this request"}});
                continue;
            }
            if(!data.ContainsKey("id") || Convert.ToInt32(data["id"])!=request) continue;
            if(data.ContainsKey("error")) {
                var detail=data["error"] as Dictionary<string,object>;
                string messageText=detail!=null&&detail.ContainsKey("message")?Convert.ToString(detail["message"]):"서버 조회 오류";
                throw new Exception(messageText);
            }
            return data["result"] as Dictionary<string,object>;
        }
    }
    public async Task Initialize() {
        await Call("initialize",new {clientInfo=new {name="teacher_codex_usage_widget",title="Teacher Codex Usage Widget",version=BuildInfo.Version},capabilities=new {experimentalApi=true}});
        Send(new {method="initialized"});
    }
    public async Task WaitForLogin(CancellationToken token) {
        while(true) {
            var read=process.StandardOutput.ReadLineAsync();
            if(await Task.WhenAny(read,Task.Delay(TimeSpan.FromMinutes(5),token))!=read)throw new OperationCanceledException();
            token.ThrowIfCancellationRequested();
            string line=await read;if(line==null)throw new Exception("로그인 연결이 종료되었습니다.");
            Dictionary<string,object> msg;try{msg=json.Deserialize<Dictionary<string,object>>(line);}catch{continue;}
            if(msg.ContainsKey("method")&&Convert.ToString(msg["method"])=="account/login/completed") {
                var p=msg["params"] as Dictionary<string,object>;
                if(p!=null&&p.ContainsKey("success")&&Convert.ToBoolean(p["success"]))return;
                throw new Exception("로그인이 완료되지 않았습니다. 다시 연결해 주세요.");
            }
        }
    }
    public void Dispose(){if(disposed)return;disposed=true;try{if(!process.HasExited){process.Kill();process.WaitForExit(2000);}}catch{} process.Dispose();}
}
