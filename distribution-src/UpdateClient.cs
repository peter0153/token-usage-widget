using System;using System.IO;using System.Net;using System.Linq;using System.Text;using System.Threading.Tasks;using System.Diagnostics;using System.Windows.Forms;
static class UpdateNetwork {
 static bool Allowed(Uri u){return u.Scheme=="https"&&u.IsDefaultPort&&String.IsNullOrEmpty(u.UserInfo)&&(u.Host=="github.com"||u.Host=="release-assets.githubusercontent.com"||u.Host=="objects.githubusercontent.com"||u.Host=="github-releases.githubusercontent.com");}
 public static bool Download(string address,string target,long max){Uri u=new Uri(address);var clock=Stopwatch.StartNew();ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
  for(int i=0;i<7;i++){if(!Allowed(u))throw new IOException("업데이트 다운로드 주소가 올바르지 않습니다.");var q=(HttpWebRequest)WebRequest.Create(u);q.AllowAutoRedirect=false;q.Timeout=20000;q.ReadWriteTimeout=20000;q.UserAgent="TeacherCodexUsageWidget/"+BuildInfo.Version;
   try{using(var response=(HttpWebResponse)q.GetResponse()){
    if((int)response.StatusCode>=300&&(int)response.StatusCode<400){string location=response.Headers["Location"];if(String.IsNullOrEmpty(location))throw new IOException("다운로드 주소가 없습니다.");u=new Uri(u,location);continue;}
    if(response.StatusCode!=HttpStatusCode.OK)throw new IOException("다운로드 서버 응답을 확인해 주세요.");if(response.ContentLength>max)throw new IOException("업데이트 파일 크기 제한을 넘었습니다.");
    using(var input=response.GetResponseStream())using(var output=File.Create(target)){byte[] bytes=new byte[65536];long size=0;int n;while((n=input.Read(bytes,0,bytes.Length))>0){size+=n;if(size>max||clock.Elapsed.TotalMinutes>6)throw new IOException("업데이트 다운로드 제한을 넘었습니다.");output.Write(bytes,0,n);}}return true;
   }}catch(WebException e){using(var response=e.Response as HttpWebResponse){if(response!=null&&response.StatusCode==HttpStatusCode.NotFound)return false;}throw;}
  }throw new IOException("다운로드 이동이 너무 많습니다.");
 }
}
sealed class UpdateClient {
 readonly string root;string envelope;public ReleaseInfo Available;
 public UpdateClient(string data){root=data;}
 public async Task<ReleaseInfo> Check(){string dir=Path.Combine(root,"updates");Directory.CreateDirectory(dir);string file=Path.Combine(dir,"check-"+Guid.NewGuid().ToString("N")+".json");try{
   bool found=await Task.Run(()=>UpdateNetwork.Download("https://github.com/"+BuildInfo.Repository+"/releases/latest/download/update.json",file,98304));
   if(!found){Available=null;return null;}envelope=File.ReadAllText(file,Encoding.UTF8);Available=UpdatePolicy.Read(envelope,UpdateKey.PublicKey,new Version(BuildInfo.Version),BuildInfo.Repository);return Available;
  }finally{if(File.Exists(file))File.Delete(file);}
 }
 public async Task<string> Prepare(){if(Available==null||envelope==null)throw new IOException("먼저 업데이트를 확인해 주세요.");string dir=Path.Combine(root,"updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string package=Path.Combine(dir,"package.zip");
  var release=Available;bool ok=await Task.Run(()=>UpdateNetwork.Download(release.PackageUrl,package,UpdatePolicy.MaxPackage));if(!ok)throw new IOException("업데이트 파일이 아직 게시되지 않았습니다.");UpdatePolicy.ValidateFile(package,release);
  string helper=Path.Combine(Application.StartupPath,"WidgetUpdater.exe");if(!File.Exists(helper)||UpdatePolicy.Hash(helper)!=HelperInfo.Sha256)throw new IOException("업데이트 도구를 확인할 수 없습니다. 설치 파일로 다시 설치해 주세요.");
  File.WriteAllText(Path.Combine(dir,"update.json"),envelope,new UTF8Encoding(false));File.Copy(helper,Path.Combine(dir,"WidgetUpdater.exe"));return dir;
 }
 public static string InstalledFolder{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","TeacherCodexUsageWidget");}}
}
