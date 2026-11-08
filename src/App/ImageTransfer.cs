using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace Stackshot
{
    // Files remain until the next cleanup window because some drop targets open them after OLE returns.
    internal static class ImageTransfer
    {
        internal static DataObject Prepare(Bitmap image,string dataRoot,out string path)
        {
            string folder=Path.Combine(Path.GetFullPath(dataRoot),"Transfers");FilePaths.EnsureDirectory(folder);
            foreach(var file in Directory.GetFiles(FilePaths.Native(folder),"*.png")){Guid id;if(Guid.TryParseExact(Path.GetFileNameWithoutExtension(file),"N",out id)&&File.GetLastWriteTimeUtc(file)<DateTime.UtcNow.AddDays(-1))try{File.Delete(file);}catch(IOException){}catch(UnauthorizedAccessException){}}
            path=Path.Combine(folder,Guid.NewGuid().ToString("N")+".png");ImageFiles.SavePng(image,path);var data=new DataObject();data.SetData(DataFormats.FileDrop,new[]{path});return data;
        }
    }
    public partial class PinWindow
    {
        void DragImageFile(){string path;var data=ImageTransfer.Prepare(img,Settings.DataDir,out path);try{DoDragDrop(data,DragDropEffects.Copy);}catch(Exception ex){Tell("拖出图片失败",ex.Message,null);}}
    }
}
