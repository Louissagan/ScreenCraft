using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
namespace ScreenCraft
{
    internal sealed class HistoryItem
    {
        public string Id,TimeUtc;public int Width,Height;
    }
    internal sealed class CaptureHistory
    {
        internal readonly string Folder;
        string Index{get{return Path.Combine(Folder,"index.json");}}
        internal CaptureHistory(string dataRoot){Folder=Path.Combine(Path.GetFullPath(dataRoot),"History");}
        internal HistoryItem[] List()
        {
            if(!FilePaths.Exists(Index))return new HistoryItem[0];
            if(new FileInfo(FilePaths.Native(Index)).Length>1024*1024)throw new InvalidDataException("历史索引过大，可清理自有历史后重试。");
            var values=new JavaScriptSerializer().Deserialize<HistoryItem[]>(File.ReadAllText(FilePaths.Native(Index)));
            if(values==null||values.Length>500)throw new InvalidDataException("历史索引无效。");
            var seen=new HashSet<string>();foreach(var item in values){if(item==null||!IsId(item.Id)||!seen.Add(item.Id)||item.Width<1||item.Height<1||item.Width>64000000L/item.Height)throw new InvalidDataException("历史索引包含无效条目。");}
            return values.Where(delegate(HistoryItem item){return FilePaths.Exists(ImagePath(item.Id));}).ToArray();
        }
        static bool IsId(string id){Guid value;return id!=null&&Guid.TryParseExact(id,"N",out value);}
        internal string ImagePath(string id){if(!IsId(id))throw new ArgumentException("历史 ID 无效。");return Path.Combine(Folder,id+".png");}
        internal void Record(Bitmap image,int limit)
        {
            limit=Math.Max(10,Math.Min(500,limit));var previous=List();var item=new HistoryItem{Id=Guid.NewGuid().ToString("N"),TimeUtc=DateTime.UtcNow.ToString("o"),Width=image.Width,Height=image.Height};FilePaths.EnsureDirectory(Folder);ImageFiles.SavePng(image,ImagePath(item.Id));
            var all=new List<HistoryItem>{item};all.AddRange(previous);try{Write(all.Take(limit).ToArray());}catch{FilePaths.Delete(ImagePath(item.Id));throw;}
            foreach(var old in all.Skip(limit))FilePaths.Delete(ImagePath(old.Id));
        }
        void Write(HistoryItem[] items){FilePaths.EnsureDirectory(Folder);string temp=Index+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(FilePaths.Native(temp),new JavaScriptSerializer().Serialize(items));FilePaths.Commit(temp,Index,true);}finally{if(FilePaths.Exists(temp))FilePaths.Delete(temp);}}
        internal void Delete(string id){var remaining=List().Where(delegate(HistoryItem item){return item.Id!=id;}).ToArray();string path=ImagePath(id);Write(remaining);if(FilePaths.Exists(path))FilePaths.Delete(path);}
        internal void Clear(){InstallerCleanup.ClearOwnedCache(Path.GetDirectoryName(Folder));}
    }
    internal static class CaptureCompletion
    {
        internal static string SaveDirectory(Preferences p,string dataRoot)
        {return SaveDirectory(p,dataRoot,delegate{return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);});}
        internal static string SaveDirectory(Preferences p,string dataRoot,Func<string> desktopDirectory)
        {
            string requested=p.SaveDirectory;
            if(string.IsNullOrWhiteSpace(requested)){requested=desktopDirectory();if(string.IsNullOrWhiteSpace(requested)||!Path.IsPathRooted(requested))throw new IOException("无法取得系统桌面目录，请在设置中选择自动保存目录。");}
            string directory=Path.GetFullPath(Path.IsPathRooted(requested)?requested:Path.Combine(dataRoot,requested));
            string history=Path.GetFullPath(Path.Combine(dataRoot,"History"));if(string.Equals(directory.TrimEnd(Path.DirectorySeparatorChar),history.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))throw new IOException("自动保存请使用 History 以外的目录，避免与历史清理混用。");return directory;
        }
        internal static string StoreSaveDirectory(string text)
        {
            string value=(text??"").Trim();if(value.Length==0)return "";
            string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            // Keep the default portable: do not serialize one user's desktop path as another user's default.
            if(!string.IsNullOrWhiteSpace(desktop)&&Path.IsPathRooted(value)&&string.Equals(Path.GetFullPath(value).TrimEnd('\\','/'),Path.GetFullPath(desktop).TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase))return "";
            return value;
        }
        internal static string FileName(Preferences p,DateTime local,string id)
        {
            string value=(p.FileNameTemplate??"ScreenCraft-{date}-{time}").Replace("{date}",local.ToString("yyyy-MM-dd")).Replace("{time}",local.ToString("HH-mm-ss-fff")).Replace("{id}",id.Substring(0,8));
            if(string.IsNullOrWhiteSpace(value)||value.Length>180||value.IndexOfAny(Path.GetInvalidFileNameChars())>=0||value.Contains("{")||value.Contains("}"))throw new IOException("文件名模板只支持 {date}、{time}、{id}，且不能含路径字符。");return value+".png";
        }
        internal static string Deliver(Bitmap image,Preferences p,string dataRoot,Action<Bitmap> copy,bool useAction=true)
        {
            bool save=useAction&&p.CompleteAction!="copy",clipboard=!useAction||p.CompleteAction!="save";string output=null;
            if(save){var folder=SaveDirectory(p,dataRoot);FilePaths.EnsureDirectory(folder);string name=FileName(p,DateTime.Now,Guid.NewGuid().ToString("N"));output=Path.Combine(folder,name);for(int i=1;FilePaths.Exists(output);i++)output=Path.Combine(folder,Path.GetFileNameWithoutExtension(name)+"-"+i+".png");SaveNewPng(image,output);}
            if(clipboard)copy(image);
            if(p.HistoryEnabled)new CaptureHistory(dataRoot).Record(image,p.HistoryLimit);
            return output;
        }
        static void SaveNewPng(Bitmap image,string path){string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var stream=FilePaths.Create(temp))image.Save(stream,System.Drawing.Imaging.ImageFormat.Png);FilePaths.Commit(temp,path,false);}finally{if(FilePaths.Exists(temp))FilePaths.Delete(temp);}}
    }
}
