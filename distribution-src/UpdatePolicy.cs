using System;using System.IO;using System.Linq;using System.Text;using System.Text.RegularExpressions;using System.Security.Cryptography;using System.Web.Script.Serialization;using System.IO.Compression;
sealed class ReleaseInfo {public string Version,PackageUrl,Sha256,Notes;public long Size;}
static class UpdatePolicy {
 public const long MaxPackage=192L*1024*1024;
 public static readonly string[] AllowedFiles={"CodexUsageWidget.exe","WidgetUpdater.exe","TokenWidget.exe","Install.ps1","설치.cmd","README.txt","Codex-LICENSE.txt","Codex-NOTICE.txt","LICENSE.txt"};
 public static ReleaseInfo Read(string envelope,string publicKey,Version current,string repo){
  if(envelope==null||envelope.Length>98304)throw new IOException("업데이트 정보 크기를 확인할 수 없습니다.");var json=new JavaScriptSerializer();var data=json.Deserialize<System.Collections.Generic.Dictionary<string,string>>(envelope);
  byte[] payload=Convert.FromBase64String(data["payload"]),signature=Convert.FromBase64String(data["signature"]);if(payload.Length>32768)throw new IOException("업데이트 정보가 너무 큽니다.");
  using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(publicKey);if(!rsa.VerifyData(payload,"SHA256",signature))throw new IOException("업데이트 제작자 서명이 일치하지 않습니다.");}
  var p=json.Deserialize<System.Collections.Generic.Dictionary<string,object>>(Encoding.UTF8.GetString(payload));string version=Convert.ToString(p["version"]);Version parsed;
  if(!Version.TryParse(version,out parsed)||parsed.Major<0||parsed.Build<0||parsed.Revision>=0)throw new IOException("업데이트 버전이 올바르지 않습니다.");
  var r=new ReleaseInfo{Version=version,PackageUrl=Convert.ToString(p["package_url"]),Sha256=Convert.ToString(p["sha256"]),Size=Convert.ToInt64(p["size"]),Notes=p.ContainsKey("notes")?Convert.ToString(p["notes"]):""};
  Uri u;if(!Uri.TryCreate(r.PackageUrl,UriKind.Absolute,out u)||u.Scheme!="https"||u.Host!="github.com"||!String.IsNullOrEmpty(u.UserInfo)||!u.IsDefaultPort||u.Query!=""||u.Fragment!=""||u.AbsolutePath!="/"+repo+"/releases/download/v"+version+"/TokenWidget-"+version+"-win64.zip")throw new IOException("공식 배포 저장소 주소가 아닙니다.");
  if(!Regex.IsMatch(r.Sha256??"","^[a-fA-F0-9]{64}$")||r.Size<=0||r.Size>MaxPackage)throw new IOException("업데이트 파일 검증 정보가 올바르지 않습니다.");if(r.Notes.Length>8000)r.Notes=r.Notes.Substring(0,8000);return parsed>current?r:null;
 }
 public static string Hash(string path){using(var s=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 public static void ValidateFile(string path,ReleaseInfo r){if(new FileInfo(path).Length!=r.Size||!String.Equals(Hash(path),r.Sha256,StringComparison.OrdinalIgnoreCase))throw new IOException("업데이트 파일이 손상되었습니다. 다시 받아 주세요.");}
 public static void Extract(string path,string destination){using(var z=ZipFile.OpenRead(path)){long total=0;var seen=new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var e in z.Entries){if(!AllowedFiles.Contains(e.FullName)||!seen.Add(e.FullName)||e.FullName!=e.Name||e.Length<0||(total+=e.Length)>256L*1024*1024)throw new IOException("업데이트 압축 파일 구성이 올바르지 않습니다.");}if(!seen.Contains("CodexUsageWidget.exe")||!seen.Contains("WidgetUpdater.exe"))throw new IOException("업데이트 실행 파일이 없습니다.");Directory.CreateDirectory(destination);foreach(var e in z.Entries)e.ExtractToFile(Path.Combine(destination,e.Name),false);}}
}
