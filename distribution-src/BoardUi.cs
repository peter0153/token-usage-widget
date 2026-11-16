using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Text;using System.Windows.Forms;using Microsoft.Win32;
static class BoardColors {public static string Band(double remaining){return remaining<=10?"red":remaining<=50?"orange":remaining<=80?"blue":"green";}}
sealed class BoardTheme {
 public bool Dark;public Color Bg,Surface,Ink,Muted,Line,Track,Accent,Tint;
 static Color C(string hex){return ColorTranslator.FromHtml(hex);}
 public static bool SystemDark(){try{using(var k=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))return k!=null&&Convert.ToInt32(k.GetValue("AppsUseLightTheme",1))==0;}catch{return false;}}
 public static BoardTheme Get(string mode){bool d=mode=="dark"||mode!="light"&&SystemDark();return new BoardTheme{Dark=d,Bg=C(d?"#121A18":"#FAFAF7"),Surface=C(d?"#1B2622":"#FFFFFF"),Ink=C(d?"#F1F5F1":"#20312D"),Muted=C(d?"#ADBAB2":"#626E68"),Line=C(d?"#53665B":"#D3DBD4"),Track=C(d?"#34443B":"#E9EDE9"),Accent=C(d?"#83DFC0":"#12745F"),Tint=C(d?"#223A30":"#E9F3EE")};}
 public Color Quota(double v){string b=BoardColors.Band(v);return ColorTranslator.FromHtml(Dark?(b=="red"?"#FF6B75":b=="orange"?"#FFAB52":b=="blue"?"#70A8FF":"#34D399"):(b=="red"?"#DE3A42":b=="orange"?"#DF7B21":b=="blue"?"#2878E6":"#087F68"));}
}
static class BoardDraw {
 public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));if(d<=0){p.AddRectangle(r);return p;}p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 public static void Box(Graphics g,RectangleF r,Color fill,float radius=8,Color? stroke=null){using(var p=Round(r,radius)){using(var b=new SolidBrush(fill))g.FillPath(b,p);if(stroke.HasValue)using(var pen=new Pen(stroke.Value))g.DrawPath(pen,p);}}
 public static void Text(Graphics g,string s,RectangleF r,float pixels,Color color,bool bold=false,bool right=false,bool center=false){using(var f=new Font("맑은 고딕",pixels,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(color))using(var sf=new StringFormat{Alignment=center?StringAlignment.Center:right?StringAlignment.Far:StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(s??"",f,b,r,sf);}
 public static void Line(Graphics g,float y,float width,Color c){using(var p=new Pen(c))g.DrawLine(p,20,y,width-20,y);}
 public static void Bar(Graphics g,RectangleF r,double? remaining,BoardTheme t){Box(g,r,t.Track,r.Height/2,t.Line);if(remaining.HasValue&&remaining.Value>0)Box(g,new RectangleF(r.X,r.Y,(float)(r.Width*Math.Min(100,remaining.Value)/100),r.Height),t.Quota(remaining.Value),r.Height/2);}
 public static Button Button(string text,EventHandler click,BoardTheme t){var b=new Button{Text=text,FlatStyle=FlatStyle.Flat,BackColor=t.Tint,ForeColor=t.Accent,Font=new Font("맑은 고딕",9),Cursor=Cursors.Hand,UseVisualStyleBackColor=false};b.FlatAppearance.BorderSize=0;b.Click+=click;return b;}
}
