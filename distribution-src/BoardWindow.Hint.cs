using System;using System.Drawing;using System.Windows.Forms;
sealed class MiniHintWindow : Form {
 public MiniHintWindow(string text,BoardTheme t){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=t.Surface;ClientSize=new Size(316,110);Controls.Add(new Label{Text=text,AutoSize=false,Bounds=new Rectangle(12,8,294,96),ForeColor=t.Ink,Font=new Font("맑은 고딕",9)});}
 protected override bool ShowWithoutActivation{get{return true;}}
}
partial class UsageWidget {
 MiniHintWindow miniHint;
 void ShowMiniHint(bool on){if(on&&(miniWindow.Resizing||shared!=null&&shared.Visible))return;if(miniHint!=null){miniHint.Close();miniHint.Dispose();miniHint=null;}if(!on||closing)return;var s=MiniState;if(s==null)return;var d=s.Data;string value=s.Entry.Alias+" · Codex 5시간";if(d!=null){value+="\n마지막 확인 "+d.Updated.ToString("MM/dd HH:mm");if(d.ShortReset.HasValue)value+=" · 재충전 "+DateTimeOffset.FromUnixTimeSeconds(d.ShortReset.Value).LocalDateTime.ToString("MM/dd HH:mm");}if(s.Error!=null)value+="\n이전 값 · 연결 확인 필요";value+="\n왼쪽 가장자리를 드래그해 너비를 조절하세요.";miniHint=new MiniHintWindow(value,theme);var b=miniWindow.Bounds;var screen=Screen.FromRectangle(b).WorkingArea;miniHint.Location=new Point(Math.Max(screen.Left,Math.Min(b.Left,screen.Right-miniHint.Width)),Math.Max(screen.Top,b.Top-miniHint.Height-6));miniHint.Show();}
}
