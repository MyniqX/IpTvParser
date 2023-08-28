using Downloader;
using IpTvParser.IpTvCatalog;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IpTvParser.Helpers
{
    public class CoverDownloader : INotifyPropertyChanged
    {
        public CoverDownloader()
        {
            Task.Run(() =>
            {
                try
                {
                    var di = new DirectoryInfo(IpTvConfig.ImageFolder);
                    directorySize = di.EnumerateFiles().Sum(f => f.Length);
                }
                catch
                {
                    directorySize = 0;
                }
            });
        }

        Dictionary<ViewObject,DownloadService> downloadList = new();
        List<DownloadService> downloadServices = new();
        public string SizeInfo
        {
            get
            {
                long kb = directorySize >> 10;
                if (kb < 1024)
                    return $"{kb} kb.";
                long mb = kb >> 10;
                if (mb < 1024)
                    return $"{mb} mb.";
                long gb = mb >> 10;
                return $"{gb} gb.";
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        int MaxDownload => 3;

        public void addDownload(ViewObject viewObject, DownloadService? service = null)
        {
            lock (this)
            {
                downloadList[viewObject] = service ?? new DownloadService();
                if (downloadServices.Count >= MaxDownload)
                    return;
            }
            startDownload();
        }

        long directorySize = 0;

        public void clearList()
        {
            lock (this)
                downloadList.Clear();
        }

        private void startDownload()
        {
            ViewObject vObj;
            DownloadService cDwn;
            lock (this)
            {
                if (downloadList.Count == 0)
                    return;
                var   firstPair =   downloadList.First();
                downloadList.Remove(firstPair.Key);
                downloadServices.Add(firstPair.Value);
                vObj = firstPair.Key;
                cDwn = firstPair.Value;
            }

            cDwn.DownloadProgressChanged += (o, e) => vObj.LogoPercent = e.ProgressPercentage;
            cDwn.DownloadFileCompleted += (o, e) =>
            {
                lock (this)
                    downloadServices.Remove(cDwn);
                vObj.LogoPercent = 0;
                directorySize += cDwn.Package.TotalFileSize;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SizeInfo)));
                Thread.Sleep(1);
                vObj.notifyImageChanged();
                Thread.Sleep(1);
                startDownload();
            };

            cDwn.DownloadFileTaskAsync(vObj.Logo, vObj.FullPathOfLogo);
        }

        internal void cleanCache()
        {
            DirectoryInfo di = new DirectoryInfo(IpTvConfig.ImageFolder);
            foreach (var fi in di.EnumerateFiles())
            {
                try
                { fi.Delete(); }
                catch { }
            }
            directorySize = 0;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SizeInfo)));
        }
    }
}
