using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

static class AppPaths {
    public static string Data;
    public static bool VerifyExistingAccount;
    public static bool UseExistingCodex;
    public static string Account { get { return VerifyExistingAccount||UseExistingCodex?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex"):Path.Combine(Data,"account"); } }
    public static string Server { get { return Path.Combine(Data,"runtime","0.163.0","codex-app-server.exe"); } }
    public static string Report;
    public static bool Verify, VerifyLogin;
    public static bool AutoStart,StartMini,StartFull,VerifyMini,VerifyBoard; public static string HealthFolder;
    public static bool VerifyOnly { get { return Verify || VerifyLogin || VerifyBoard; } }
    public static void Initialize(string[] args) {
        Data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TeacherCodexUsageWidget");
        bool followDefault=false;
        for(int i=0;i<args.Length;i++) {
            if(args[i]=="--data-dir" && i+1<args.Length)Data=Path.GetFullPath(args[++i]);
            else if(args[i]=="--report-dir" && i+1<args.Length)Report=Path.GetFullPath(args[++i]);
            else if(args[i]=="--verify")Verify=true;
            else if(args[i]=="--verify-login")VerifyLogin=true;
            else if(args[i]=="--verify-current-account"){Verify=true;VerifyExistingAccount=true;}
            else if(args[i]=="--follow-codex")followDefault=true;
            else if(args[i]=="--autostart")AutoStart=true;
            else if(args[i]=="--start-mini")StartMini=true;
            else if(args[i]=="--start-full")StartFull=true;
            else if(args[i]=="--verify-mini")VerifyMini=true; else if(args[i]=="--verify-board")VerifyBoard=true; else if(args[i]=="--post-update" && i+1<args.Length)HealthFolder=Path.GetFullPath(args[++i]);
        }
        if(VerifyBoard && !args.Contains("--data-dir"))throw new InvalidOperationException("UI 검사는 분리된 데이터 경로가 필요합니다."); Directory.CreateDirectory(Data);Directory.CreateDirectory(Path.Combine(Data,"account"));
        if(!VerifyOnly){
            string connection=Path.Combine(Data,"connection-mode.txt");
            UseExistingCodex=File.Exists(connection)?File.ReadAllText(connection).Trim()=="existing":followDefault;
        }
        if(!VerifyExistingAccount){
            string config=Path.Combine(Data,"account","config.toml");
            if(!File.Exists(config))File.WriteAllText(config,"cli_auth_credentials_store = \"file\"\n",new UTF8Encoding(false));
        }
        if(Report!=null)Directory.CreateDirectory(Report);
    }
    public static void SetExistingConnection(bool enabled){
        UseExistingCodex=enabled;
        if(!VerifyOnly)File.WriteAllText(Path.Combine(Data,"connection-mode.txt"),enabled?"existing":"independent");
    }
    static string Hash(string file) {
        using(var s=File.OpenRead(file))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();
    }
    public static void PrepareRuntime() {
        if(File.Exists(Server) && Hash(Server)==RuntimeInfo.Sha256)return;
        using(var splash=new Form {Text="사용량 위젯 준비",ClientSize=new Size(400,115),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,ControlBox=false,TopMost=true}) {
            splash.Controls.Add(new Label {Text="처음 실행할 도구를 준비하고 있어요.\n잠시만 기다려 주세요.",AutoSize=true,Location=new Point(28,25),Font=new Font("맑은 고딕",11)});
            splash.Show();splash.Refresh();
            string temp=Server+".preparing";
            Directory.CreateDirectory(Path.GetDirectoryName(Server));
            try {
                using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("runtime.zip"))using(var zip=new ZipArchive(s,ZipArchiveMode.Read)) {
                    var entry=zip.Entries.Single(e=>e.Name==RuntimeInfo.EntryName);
                    using(var input=entry.Open())using(var output=File.Create(temp))input.CopyTo(output);
                }
                if(Hash(temp)!=RuntimeInfo.Sha256)throw new Exception("연결 도구의 무결성 확인에 실패했습니다. 배포 파일을 다시 받아 주세요.");
                if(File.Exists(Server))File.Delete(Server);File.Move(temp,Server);
                foreach(string name in new[]{"Codex-LICENSE.txt","Codex-NOTICE.txt"})
                    using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(var output=File.Create(Path.Combine(Data,name)))input.CopyTo(output);
            } finally {if(File.Exists(temp))File.Delete(temp);splash.Close();}
        }
    }
    public static void ReportResult(object result,Control window) {
        if(Report==null)return;
        File.WriteAllText(Path.Combine(Report,"verification.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(result));
        using(var bmp=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(bmp,new Rectangle(Point.Empty,window.Size));bmp.Save(Path.Combine(Report,"preview.png"));}
    }
}
