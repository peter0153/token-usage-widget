using System;using System.IO;using System.Linq;using System.Text;using System.Threading;using System.Diagnostics;using System.Reflection;using System.Windows.Forms;using System.Web.Script.Serialization;
class WidgetUpdater {
 public static readonly string Data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TeacherCodexUsageWidget");
 public static string InstallFolder{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","TeacherCodexUsageWidget");}}
 static bool ChildOf(string value,string parent){return Path.GetFullPath(value).StartsWith(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}
 [STAThread]static void Main(string[] args){try{
   if(args.Length!=3)throw new IOException("업데이트 실행 정보가 없습니다.");string pending=Path.GetFullPath(args[0]);if(!ChildOf(pending,Path.Combine(Data,"updates"))||Path.GetFileName(pending).Length!=32)throw new IOException("업데이트 작업 경로가 올바르지 않습니다.");
   int oldId=Int32.Parse(args[1]);long oldTicks=Int64.Parse(args[2]);string target=Path.Combine(InstallFolder,"CodexUsageWidget.exe");
   try{using(var old=Process.GetProcessById(oldId)){if(old.StartTime.ToUniversalTime().Ticks!=oldTicks||!String.Equals(old.MainModule.FileName,target,StringComparison.OrdinalIgnoreCase))throw new IOException("업데이트 대상 프로그램이 다릅니다.");if(!old.WaitForExit(45000))throw new IOException("프로그램 종료를 기다리지 못했습니다.");}}catch(ArgumentException){}
   Apply(pending,InstallFolder,true);Application.Exit();
  }catch(Exception e){MessageBox.Show("업데이트를 완료하지 못했습니다. 기존 버전과 설정은 유지됩니다.\n"+(e is IOException?e.Message:"설치 파일을 다시 확인해 주세요."),"사용량 위젯 업데이트",MessageBoxButtons.OK,MessageBoxIcon.Error);Environment.ExitCode=1;}}
 // A small transaction boundary also used by the disposable-install integration test.
 public static void Apply(string pending,string install,bool launch){
  string envelope=File.ReadAllText(Path.Combine(pending,"update.json"),Encoding.UTF8);var release=UpdatePolicy.Read(envelope,UpdateKey.PublicKey,new Version(0,0,0),BuildInfo.Repository);if(release==null)throw new IOException("업데이트 정보가 없습니다.");
  string target=Path.Combine(install,"CodexUsageWidget.exe");if(File.Exists(target)){Version before=AssemblyName.GetAssemblyName(target).Version;if(new Version(release.Version)<=before)throw new IOException("현재보다 이전이거나 같은 버전입니다.");}
  string archive=Path.Combine(pending,"package.zip");UpdatePolicy.ValidateFile(archive,release);string extracted=Path.Combine(pending,"verified");UpdatePolicy.Extract(archive,extracted);
  if(AssemblyName.GetAssemblyName(Path.Combine(extracted,"CodexUsageWidget.exe")).Version!=new Version(release.Version+".0"))throw new IOException("설치 버전과 서명된 버전이 일치하지 않습니다.");
  // Keep the launcher stable: it recovers this journal if the updater is interrupted.
  string[] files=Directory.GetFiles(extracted).Select(Path.GetFileName).Where(n=>n!="TokenWidget.exe").ToArray();Process next=null;File.WriteAllText(Path.Combine(pending,"installed-version.txt"),release.Version);
  using(var tx=InstallTransaction.Begin(install,files.Concat(new[]{"installed-version.txt"}))){try{foreach(string name in files)tx.Replace(Path.Combine(extracted,name),name);tx.Replace(Path.Combine(pending,"installed-version.txt"),"installed-version.txt");
   if(launch)next=InstallTransaction.StartHealthy(target,pending,release.Version,"");File.WriteAllText(Path.Combine(pending,"result.txt"),"updated "+release.Version);tx.Commit();
  }catch{InstallTransaction.Stop(next);tx.Rollback();File.WriteAllText(Path.Combine(pending,"result.txt"),"rolled-back");if(launch&&File.Exists(target))Process.Start(new ProcessStartInfo(target){WorkingDirectory=install,UseShellExecute=false});throw;}}
 }
}
