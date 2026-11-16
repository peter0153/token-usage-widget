using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

class StartupRegistration {
    const string KeyPath="Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    readonly string name;
    public StartupRegistration(string valueName="TeacherCodexUsageWidget"){name=valueName;}
    public string Command {get{using(var key=Registry.CurrentUser.OpenSubKey(KeyPath))return key==null?null:key.GetValue(name) as string;}}
    public bool Enabled {get{return !String.IsNullOrWhiteSpace(Command);}}
    public void RestoreCommand(string command){using(var key=Registry.CurrentUser.CreateSubKey(KeyPath)){if(String.IsNullOrWhiteSpace(command))key.DeleteValue(name,false);else key.SetValue(name,command,RegistryValueKind.String);}}
    public static string MakeCommand(string exe,bool followCodex){return "\""+exe+"\" --autostart"+(followCodex?" --follow-codex":"");}
    public void SetEnabled(bool enabled,string exe,bool followCodex){
        using(var key=Registry.CurrentUser.CreateSubKey(KeyPath)){
            if(enabled){string command=MakeCommand(exe,followCodex);if(command.Length>260)throw new InvalidOperationException("프로그램 경로가 너무 깁니다.");key.SetValue(name,command,RegistryValueKind.String);}
            else key.DeleteValue(name,false);
        }
    }
}
static class PercentTrayIcon {
    [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr icon);
    public static Color Accent(double? remaining){return remaining.HasValue&&remaining.Value<=10?Color.FromArgb(255,130,130):remaining.HasValue&&remaining.Value<=25?Color.FromArgb(255,205,115):Color.FromArgb(115,228,191);}
    public static Icon Create(double? remaining){
        using(var bmp=new Bitmap(32,32))using(var g=Graphics.FromImage(bmp)){
            g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);
            using(var b=new SolidBrush(Color.FromArgb(25,33,44)))g.FillEllipse(b,1,1,30,30);
            using(var p=new Pen(Color.FromArgb(85,99,118),2.2f))g.DrawEllipse(p,2,2,28,28);
            var accent=Accent(remaining);
            if(remaining.HasValue&&remaining>0)using(var p=new Pen(accent,2.2f))g.DrawArc(p,2,2,28,28,-90,(float)(Math.Max(0,Math.Min(100,remaining.Value))*3.6));
            string value=remaining.HasValue?Math.Max(0,Math.Min(100,remaining.Value)).ToString("0"):"—";
            using(var f=new Font("Segoe UI",value.Length>=3?10:13,FontStyle.Bold))using(var b=new SolidBrush(accent))using(var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(value,f,b,new RectangleF(0,0,32,32),fmt);
            IntPtr handle=bmp.GetHicon();try{using(var borrowed=Icon.FromHandle(handle))return (Icon)borrowed.Clone();}finally{DestroyIcon(handle);}
        }
    }
}
