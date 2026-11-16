using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

// A real WS_CHILD window owned by Explorer's taskbar, not a floating desktop form.
class TaskbarUsageWindow : NativeWindow, IDisposable {
    [StructLayout(LayoutKind.Sequential)]struct RECT{public int Left,Top,Right,Bottom;public Rectangle Rectangle{get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}}}
    [StructLayout(LayoutKind.Sequential)]struct POINT{public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]struct PAINTSTRUCT{
        public IntPtr hdc;[MarshalAs(UnmanagedType.Bool)]public bool erase;public RECT paint;
        [MarshalAs(UnmanagedType.Bool)]public bool restore;[MarshalAs(UnmanagedType.Bool)]public bool update;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=32)]public byte[] reserved;
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string cls,string title);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string title);
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out RECT rect);
    [DllImport("user32.dll")]static extern bool ScreenToClient(IntPtr h,ref POINT p);
    [DllImport("user32.dll")]static extern IntPtr GetParent(IntPtr h);
    [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")]static extern bool IsChild(IntPtr parent,IntPtr child);
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
    [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")]static extern bool InvalidateRect(IntPtr h,IntPtr rect,bool erase);
    [DllImport("user32.dll")]static extern IntPtr BeginPaint(IntPtr h,out PAINTSTRUCT paint);
    [DllImport("user32.dll")]static extern bool EndPaint(IntPtr h,ref PAINTSTRUCT paint);
    [StructLayout(LayoutKind.Sequential)]struct SIZE{public int Width,Height;}
    [StructLayout(LayoutKind.Sequential,Pack=1)]struct BLENDFUNCTION{public byte Operation,Flags,Alpha,Format;}
    [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dc,IntPtr destination,ref SIZE size,IntPtr sourceDc,ref POINT source,uint key,ref BLENDFUNCTION blend,uint flags);
    [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder text,int length);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint process);
    [DllImport("user32.dll")]static extern uint GetDpiForWindow(IntPtr h);
    [StructLayout(LayoutKind.Sequential)]struct TRACKMOUSEEVENT{public uint cbSize,flags;public IntPtr hwndTrack;public uint hoverTime;}
    [DllImport("user32.dll")]static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT track);
    [DllImport("user32.dll")]static extern IntPtr SetCursor(IntPtr cursor);
    [DllImport("user32.dll")]static extern IntPtr SetCapture(IntPtr h);
    [DllImport("user32.dll")]static extern bool ReleaseCapture();
    IntPtr taskbar;
    bool wantedVisible,disposed,lightTheme,hovered;
    double? remaining;
    long? reset;
    bool stale,connected,checking;
    float scale=1;
    int logicalWidth=192,resizeStartX,resizeStartWidth;bool resizing;Screen resizeScreen;
    public int LogicalWidth{get{return logicalWidth;}set{logicalWidth=MiniBarRenderer.ClampWidth(value);}}
    public string ThemeMode="system";public bool Resizing{get{return resizing;}}
    public event Action<int> WidthChanged;
    public event Action OpenFull; public event Action<bool> HoverChanged;
    public ContextMenuStrip ContextMenuStrip {get;set;}
    public string LastError {get;private set;}
    public bool IsDisposed {get{return disposed;}}
    public bool Hovered {get{return hovered;}}
    public bool FramePresented {get;private set;}
    public bool ReceivesPointer {get{var r=Bounds;return Visible&&WindowFromPoint(new POINT{X=r.Left+r.Width/2,Y=r.Top+r.Height/2})==Handle;}}
    public string PointerTarget {get{var r=Bounds;IntPtr target=WindowFromPoint(new POINT{X=r.Left+r.Width/2,Y=r.Top+r.Height/2});var name=new StringBuilder(256);GetClassName(target,name,name.Capacity);uint pid;GetWindowThreadProcessId(target,out pid);return name+" (pid "+pid+")";}}
    public bool ShowInTaskbar {get{return false;}}
    public bool Embedded {get{return Handle!=IntPtr.Zero&&IsWindow(taskbar)&&GetParent(Handle)==taskbar&&IsChild(taskbar,Handle);}}
    public bool Visible {get{return !disposed&&Handle!=IntPtr.Zero&&IsWindowVisible(Handle);}}
    public Rectangle TaskbarBounds {get{RECT r;return taskbar!=IntPtr.Zero&&GetWindowRect(taskbar,out r)?r.Rectangle:Rectangle.Empty;}}
    public Rectangle Bounds {get{RECT r;return Handle!=IntPtr.Zero&&GetWindowRect(Handle,out r)?r.Rectangle:Rectangle.Empty;}}
    public Size Size {get{return new Size(Math.Max(1,(int)(logicalWidth*scale)),Math.Max(1,(int)(MiniBarRenderer.Height*scale)));}}
    public int Width {get{return Size.Width;}}
    public int Height {get{return Size.Height;}}
    public string ParentClass {get{if(!Embedded)return "";var name=new StringBuilder(256);GetClassName(taskbar,name,name.Capacity);return name.ToString();}}
    static IntPtr TaskbarForScreen(Screen screen){
        IntPtr primary=FindWindow("Shell_TrayWnd",null);RECT bounds;
        if(primary!=IntPtr.Zero&&GetWindowRect(primary,out bounds)&&screen.Bounds.Contains(new Point((bounds.Left+bounds.Right)/2,(bounds.Top+bounds.Bottom)/2)))return primary;
        IntPtr next=IntPtr.Zero;
        while((next=FindWindowEx(IntPtr.Zero,next,"Shell_SecondaryTrayWnd",null))!=IntPtr.Zero)
            if(GetWindowRect(next,out bounds)&&screen.Bounds.Contains(new Point((bounds.Left+bounds.Right)/2,(bounds.Top+bounds.Bottom)/2)))return next;
        return primary;
    }
    public void DockAt(Screen screen){
        if(disposed)return;
        IntPtr current=TaskbarForScreen(screen??Screen.PrimaryScreen);
        if(current==IntPtr.Zero){LastError="작업표시줄을 찾지 못했습니다.";return;}
        if(current!=taskbar||Handle==IntPtr.Zero||!IsWindow(Handle)){
            if(Handle!=IntPtr.Zero)DestroyHandle();taskbar=current;
            try{scale=Math.Max(1,GetDpiForWindow(taskbar)/96f);}catch{scale=1;}
            try{
                CreateHandle(new CreateParams{Caption="Codex 5-hour taskbar display",Parent=taskbar,Style=unchecked((int)0x44000000),ExStyle=0x08080000,ClassStyle=8,Width=Width,Height=Height});
                if(!Embedded){LastError="작업표시줄 자식 창 연결을 확인하지 못했습니다.";return;}
            }catch(Exception e){LastError=e.Message;return;}
        }
        RECT bar,notify,start;
        if(!GetWindowRect(taskbar,out bar)){LastError="작업표시줄 위치를 읽지 못했습니다.";return;}
        IntPtr notifyHandle=FindWindowEx(taskbar,IntPtr.Zero,"TrayNotifyWnd",null);
        bool notifyFound=notifyHandle!=IntPtr.Zero&&GetWindowRect(notifyHandle,out notify)&&notify.Right>notify.Left;
        int right=bar.Right-(int)((screen!=null&&!screen.Primary?120:340)*scale);
        if(notifyFound){GetWindowRect(notifyHandle,out notify);right=notify.Left;}
        if(right-bar.Left<Width+(int)(12*scale)){LastError="작업표시줄 공간이 부족합니다. 큰 위젯을 이용해 주세요.";if(Handle!=IntPtr.Zero)ShowWindow(Handle,0);return;} int x=Math.Max(bar.Left,right-Width-(int)(6*scale));
        int y=bar.Top+(bar.Bottom-bar.Top-Height)/2;
        IntPtr startHandle=FindWindowEx(taskbar,IntPtr.Zero,"Start",null);
        if(startHandle!=IntPtr.Zero&&GetWindowRect(startHandle,out start)&&start.Bottom>start.Top)
            y=bar.Bottom-(start.Bottom-start.Top)+(start.Bottom-start.Top-Height)/2;
        var local=new POINT{X=x,Y=y};ScreenToClient(taskbar,ref local);
        if(!SetWindowPos(Handle,IntPtr.Zero,local.X,local.Y,Width,Height,0x10)){LastError="작업표시줄 표시 위치를 변경하지 못했습니다.";return;}
        if(ThemeMode=="dark"||ThemeMode=="light")lightTheme=ThemeMode=="light";else try{using(var key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))lightTheme=key!=null&&Convert.ToInt32(key.GetValue("SystemUsesLightTheme",0))!=0;}catch{lightTheme=false;}
        LastError=null;
        PresentFrame();
        if(wantedVisible)ShowWindow(Handle,8);else ShowWindow(Handle,0);
    }
    public void Show(Screen screen){wantedVisible=true;DockAt(screen);if(Embedded&&LastError==null)ShowWindow(Handle,8);}
    public void Hide(){wantedVisible=false;if(Handle!=IntPtr.Zero)ShowWindow(Handle,0);}
    public void OpenMenu(Point anchor){if(ContextMenuStrip!=null&&!ContextMenuStrip.Visible)ContextMenuStrip.Show(anchor);}
    public void UpdateUsage(double? used,long? resetAt,bool error,bool signedIn,bool busy){
        remaining=used.HasValue?(double?)Math.Max(0,Math.Min(100,100-used.Value)):null;
        reset=resetAt;stale=error;connected=signedIn;checking=busy;
        if(Handle!=IntPtr.Zero)InvalidateRect(Handle,IntPtr.Zero,false);
    }
    public void DrawToBitmap(Bitmap target,Rectangle bounds){using(var g=Graphics.FromImage(target)){PaintContent(g,false);}}
    void PaintContent(Graphics g,bool transparent){
        g.Clear(lightTheme?Color.FromArgb(230,241,235):Color.FromArgb(30,34,40));
        MiniBarRenderer.Draw(g,scale,lightTheme,hovered,remaining,reset,DateTimeOffset.Now,stale,checking?"확인 중":connected?"조회 대기":"연결 필요",logicalWidth);
    }
    void PresentFrame(){
        if(disposed||Handle==IntPtr.Zero||!IsWindow(Handle))return;
        using(var bitmap=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb)){
            using(var g=Graphics.FromImage(bitmap))PaintContent(g,false);
            IntPtr dc=CreateCompatibleDC(IntPtr.Zero),surface=IntPtr.Zero,previous=IntPtr.Zero;
            try{
                surface=bitmap.GetHbitmap(Color.FromArgb(0));previous=SelectObject(dc,surface);
                var size=new SIZE{Width=Width,Height=Height};var source=new POINT();var blend=new BLENDFUNCTION{Alpha=255,Format=1};
                FramePresented=UpdateLayeredWindow(Handle,IntPtr.Zero,IntPtr.Zero,ref size,dc,ref source,0,ref blend,2);
                if(!FramePresented)LastError="작업표시줄 그림 표시 실패 ("+Marshal.GetLastWin32Error()+")";
            }finally{if(previous!=IntPtr.Zero)SelectObject(dc,previous);if(surface!=IntPtr.Zero)DeleteObject(surface);if(dc!=IntPtr.Zero)DeleteDC(dc);}
        }
    }
    protected override void WndProc(ref Message m){
        if(m.Msg==0x201&&((short)(m.LParam.ToInt64()&0xffff))<Math.Max(7,7*scale)){resizing=true;resizeStartX=Cursor.Position.X;resizeStartWidth=logicalWidth;resizeScreen=Screen.FromRectangle(TaskbarBounds);hovered=false;if(HoverChanged!=null)HoverChanged(false);SetCapture(Handle);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x200&&resizing){LogicalWidth=MiniBarRenderer.DragWidth(resizeStartWidth,Cursor.Position.X-resizeStartX,scale);DockAt(resizeScreen);SetCursor(Cursors.SizeWE.Handle);m.Result=IntPtr.Zero;return;}
        if((m.Msg==0x202||m.Msg==0x215||m.Msg==0x1f)&&resizing){resizing=false;ReleaseCapture();if(WidthChanged!=null)WidthChanged(logicalWidth);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0xF){
            PAINTSTRUCT ps;IntPtr dc=BeginPaint(Handle,out ps);
            try{PresentFrame();}
            finally{EndPaint(Handle,ref ps);}m.Result=IntPtr.Zero;return;
        }
        if(m.Msg==0x14){m.Result=new IntPtr(1);return;}
        if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
        if(m.Msg==0x84){m.Result=new IntPtr(1);return;}
        if(m.Msg==0x200){if(!hovered){hovered=true;var track=new TRACKMOUSEEVENT{cbSize=(uint)Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),flags=2,hwndTrack=Handle};TrackMouseEvent(ref track);PresentFrame();if(HoverChanged!=null)HoverChanged(true);}SetCursor(Cursor.Position.X-Bounds.Left<7*scale?Cursors.SizeWE.Handle:Cursors.Hand.Handle);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x2A3){hovered=false;PresentFrame();if(HoverChanged!=null)HoverChanged(false);m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x20){SetCursor(resizing||Cursor.Position.X-Bounds.Left<7*scale?Cursors.SizeWE.Handle:Cursors.Hand.Handle);m.Result=new IntPtr(1);return;}
        if(m.Msg==0x203){if(OpenFull!=null)OpenFull();m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x205||m.Msg==0x7B){OpenMenu(Cursor.Position);m.Result=IntPtr.Zero;return;}
        base.WndProc(ref m);
    }
    public void Dispose(){disposed=true;wantedVisible=false;if(resizing){resizing=false;ReleaseCapture();}if(Handle!=IntPtr.Zero)DestroyHandle();}
}
