using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

static class MiniBarRenderer {
    public const int Width=304,Height=38;
    public static int ClampWidth(int value){return Math.Max(148,Math.Min(360,value));}
    public static int DragWidth(int start,int delta,float scale){return ClampWidth(start-(int)Math.Round(delta/Math.Max(1,scale)));}
    static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    static void Block(Graphics g,RectangleF r,float radius,Color color){using(var p=Round(r,radius))using(var b=new SolidBrush(color))g.FillPath(b,p);}
    static void Text(Graphics g,string value,RectangleF r,float pixels,Color color,FontStyle weight=FontStyle.Regular,string family="Segoe UI"){
        using(var f=new Font(family,pixels,weight,GraphicsUnit.Pixel))using(var b=new SolidBrush(color))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap})g.DrawString(value,f,b,r,format);
    }
    static void ResetBadge(Graphics g,RectangleF circle,Color accent,Color ink){
        float cx=circle.X+circle.Width/2,cy=circle.Y+circle.Height/2;
        using(var pen=new Pen(accent,1.4f)){g.DrawEllipse(pen,circle);g.DrawLine(pen,cx,cy,cx,cy-4);g.DrawLine(pen,cx,cy,cx+3.5f,cy+1.5f);}
    }    public static string Countdown(long? reset,DateTimeOffset now,string fallback){if(!reset.HasValue)return fallback;var left=DateTimeOffset.FromUnixTimeSeconds(reset.Value)-now;if(left.TotalSeconds<=0)return "초기화 확인 중";int minutes=(int)Math.Ceiling(left.TotalMinutes);return (minutes/60)+"h "+(minutes%60).ToString("00")+"m 남음";}
    public static void Draw(Graphics g,float scale,bool light,bool hover,double? remaining,long? reset,DateTimeOffset now,bool stale,string fallback,int width=Width){
        width=ClampWidth(width);bool small=width<200,detailed=width>=284;float pct=small?58:72;
        g.ScaleTransform(scale,scale);g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
        var theme=BoardTheme.Get(light?"light":"dark"); var surface=theme.Surface;
        var pill=theme.Tint;
        var fg=theme.Ink;
        var accent=remaining.HasValue?theme.Quota(remaining.Value):theme.Accent;
        
        g.SmoothingMode=SmoothingMode.AntiAlias;Block(g,new RectangleF(1,1,width-2,36),8,surface);
        g.SmoothingMode=SmoothingMode.AntiAlias; if(stale)using(var dot=new SolidBrush(theme.Quota(42)))g.FillEllipse(dot,width-7,2,4,4);
        using(var outline=Round(new RectangleF(1.5f,1.5f,width-3,35),8))using(var pen=new Pen(hover?accent:light?Color.FromArgb(186,210,199):Color.FromArgb(64,82,90),hover?1.7f:1))g.DrawPath(pen,outline);
        Text(g,remaining.HasValue?remaining.Value.ToString("0")+"%":"—",new RectangleF(4,2,pct-4,27),small?21:24,accent,FontStyle.Bold);
        float track=pct-24;Block(g,new RectangleF(14,30,track,3),1.5f,light?Color.FromArgb(186,210,199):Color.FromArgb(69,89,96));
        if(remaining.HasValue&&remaining>0)Block(g,new RectangleF(14,30,(float)(track*Math.Min(100,remaining.Value)/100),3),Math.Min(1.5f,(float)(24*remaining.Value/100)),accent);
        using(var pen=new Pen(light?Color.FromArgb(190,213,202):Color.FromArgb(64,82,90)))g.DrawLine(pen,pct+6,9,pct+6,29);
        if(!detailed){float left=pct+12;Block(g,new RectangleF(left,6,width-left-6,26),7,pill);string value=Countdown(reset,now,fallback);if(small)value=value.Replace(" 남음","");Text(g,value,new RectangleF(left+2,6,width-left-10,26),small?13:14,fg,FontStyle.Regular,"맑은 고딕");return;}
        Block(g,new RectangleF(86,6,92,26),7,pill);
        ResetBadge(g,new RectangleF(92,10,18,18),accent,light?Color.White:Color.FromArgb(20,51,40));
        Text(g,reset.HasValue?DateTimeOffset.FromUnixTimeSeconds(reset.Value).LocalDateTime.ToString("HH:mm"):"—",new RectangleF(110,6,64,26),17,fg,FontStyle.Bold);
        Block(g,new RectangleF(184,6,width-190,26),7,pill);
        Text(g,Countdown(reset,now,fallback),new RectangleF(186,6,width-194,26),14,fg,FontStyle.Regular,"맑은 고딕");
    }
}
