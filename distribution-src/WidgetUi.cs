using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

class WidgetButton : Button {
    public bool CrossIcon;
    bool hovered;
    protected override void OnMouseEnter(EventArgs e){hovered=true;base.OnMouseEnter(e);Invalidate();}
    protected override void OnMouseLeave(EventArgs e){hovered=false;base.OnMouseLeave(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){
        if(!CrossIcon){base.OnPaint(e);return;}
        var background=hovered?FlatAppearance.MouseOverBackColor:BackColor;
        if(background.IsEmpty)background=BackColor;
        e.Graphics.Clear(background);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        float cx=(ClientSize.Width-1)/2f,cy=(ClientSize.Height-1)/2f;
        using(var pen=new Pen(ForeColor,1.6f)){
            e.Graphics.DrawLine(pen,cx-4.5f,cy-4.5f,cx+4.5f,cy+4.5f);
            e.Graphics.DrawLine(pen,cx+4.5f,cy-4.5f,cx-4.5f,cy+4.5f);
        }
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-3,-3),ForeColor,background);
    }
}
static class ResetCountdown {
    public static string Remaining(long reset,DateTimeOffset now){
        var duration=DateTimeOffset.FromUnixTimeSeconds(reset)-now;
        if(duration<=TimeSpan.Zero)return "(초기화 확인 중)";
        if(duration.TotalMinutes<1)return "(1분 미만 남음)";
        if(duration.TotalDays>=1)return "("+((int)duration.TotalDays)+"일 "+duration.Hours+"시간 남음)";
        if(duration.TotalHours>=1)return "("+duration.Hours+"시간 "+duration.Minutes+"분 남음)";
        return "("+duration.Minutes+"분 남음)";
    }
    public static bool CheckBoundaries(){
        var now=new DateTimeOffset(2026,10,22,0,0,0,TimeSpan.Zero);
        return Remaining(now.AddMinutes(225).ToUnixTimeSeconds(),now)=="(3시간 45분 남음)"
            &&Remaining(now.AddDays(2).AddHours(3).ToUnixTimeSeconds(),now)=="(2일 3시간 남음)"
            &&Remaining(now.AddMinutes(45).ToUnixTimeSeconds(),now)=="(45분 남음)"
            &&Remaining(now.AddSeconds(59).ToUnixTimeSeconds(),now)=="(1분 미만 남음)"
            &&Remaining(now.ToUnixTimeSeconds(),now)=="(초기화 확인 중)"
            &&Remaining(now.AddSeconds(-1).ToUnixTimeSeconds(),now)=="(초기화 확인 중)";
    }
}
static class DailyUsageView {
    public static string DateKey(DateTime day){return day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
    public static string Value(Dictionary<string,long?> daily,DateTime day){
        long? tokens;
        return daily!=null&&daily.TryGetValue(DateKey(day),out tokens)&&tokens.HasValue?tokens.Value.ToString("N0"):"미집계";
    }
    public static void Paint(Graphics g,Dictionary<string,long?> daily,string error,DateTime today,long? todayLocalTokens=null,string localError=null){
        var fg=Color.FromArgb(240,244,250);var muted=Color.FromArgb(149,162,180);var mint=Color.FromArgb(115,228,191);
        using(var b=new SolidBrush(Color.FromArgb(28,36,48)))g.FillRectangle(b,20,350,340,157);
        using(var f=new Font("맑은 고딕",9))using(var brush=new SolidBrush(muted))g.DrawString("일별 사용량",f,brush,35,359);
        using(var f=new Font("맑은 고딕",8))using(var brush=new SolidBrush(muted))g.DrawString("토큰",f,brush,316,361);
        string[] names={"오늘","어제","그제","3일 전"};
        for(int i=0;i<4;i++){
            var day=today.AddDays(-i);float y=382+25*i;
            bool local=i==0&&todayLocalTokens.HasValue;
            string value=local?todayLocalTokens.Value.ToString("N0"):Value(daily,day);
            using(var f=new Font("맑은 고딕",9))using(var b=new SolidBrush(muted))g.DrawString(names[i]+" · "+day.ToString("MM/dd")+(local?" · PC":""),f,b,35,y+1);
            using(var f=new Font("맑은 고딕",11,value=="미집계"?FontStyle.Regular:FontStyle.Bold))
            using(var b=new SolidBrush(value=="미집계"?muted:i==0?mint:fg))
            using(var format=new StringFormat{Alignment=StringAlignment.Far})g.DrawString(value,f,b,new RectangleF(176,y-1,169,25),format);
        }
        using(var f=new Font("맑은 고딕",8))using(var b=new SolidBrush(muted))g.DrawString(todayLocalTokens.HasValue?(localError!=null?"오늘: 이 PC 부분 집계 · 일부 기록 확인 불가":"오늘: 이 PC 누적(참고) · 다른 기기 미포함"):error??"서버 일별 집계 · 오늘 값은 반영이 늦을 수 있어요.",f,b,23,516);
    }
    public static bool CheckMissingAndZero(){
        var day=new DateTime(2026,10,22);
        var rows=new Dictionary<string,long?>{{DateKey(day.AddDays(-1)),0},{DateKey(day.AddDays(-2)),1234567}};
        return Value(rows,day)=="미집계"&&Value(rows,day.AddDays(-1))=="0"&&Value(rows,day.AddDays(-2))==1234567L.ToString("N0")&&Value(rows,day.AddDays(-3))=="미집계";
    }
}
