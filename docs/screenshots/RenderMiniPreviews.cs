using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;
// Documentation only: render the actual mini-bar code with synthetic, fixed data.
class RenderMiniPreviews {
 static int Main(string[] args){
  if(args.Length!=1)return 2;Directory.CreateDirectory(args[0]);
  var now=new DateTimeOffset(2026,11,16,16,11,0,TimeSpan.FromHours(9));
  long reset=now.AddHours(3).AddMinutes(47).ToUnixTimeSeconds();
  foreach(bool light in new[]{false,true})foreach(int width in new[]{160,360}){
   using(var image=new Bitmap(width*2,MiniBarRenderer.Height*2,PixelFormat.Format32bppPArgb)){
    using(var graphics=Graphics.FromImage(image)){graphics.Clear(Color.Transparent);MiniBarRenderer.Draw(graphics,2,light,false,60,reset,now,false,"",width);}
    if(image.GetPixel(0,0).A!=0||image.GetPixel(image.Width-1,image.Height-1).A!=0)throw new Exception("Preview lost transparent corners");
    image.Save(Path.Combine(args[0],"taskbar-"+(width==360?"expanded":"small")+"-"+(light?"light":"dark")+".png"),ImageFormat.Png);
   }
  }
  Console.WriteLine("Rendered 4 synthetic mini-bar previews; expanded width 360, compact width 160.");return 0;
 }
}
