using System;
using System.IO;
using System.Drawing.Imaging;
using ScreenCraft;
static class IconBuilder
{
    static int Main(string[] args){int[] sizes={16,20,24,32,48,64,128,256};var images=new byte[sizes.Length][];for(int i=0;i<sizes.Length;i++)using(var bitmap=BrandArtwork.Render(sizes[i]))using(var stream=new MemoryStream()){bitmap.Save(stream,ImageFormat.Png);images[i]=stream.ToArray();}Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0])));using(var file=File.Create(args[0]))using(var writer=new BinaryWriter(file)){writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;}foreach(var bytes in images)writer.Write(bytes);}return 0;}
}
