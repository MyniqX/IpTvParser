using Downloader;
using IpTvParser.IpTvCatalog;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;

namespace IpTvParser.Helpers
{
    public class DownloadWatchable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [JsonIgnore]
        DownloadService? _service { get; set; } = null;
        public DownloadPackage? package { get; set; } = null;

        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public string IconFile { get; set; } = "";
        private string packFile { get; set; } = "";
        private string GetPackFile
        {
            get
            {
                if (string.IsNullOrWhiteSpace(packFile) == true)
                    using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                    {
                        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(ID);
                        byte[] hashBytes = md5.ComputeHash(inputBytes);
                        packFile = IpTvConfig.GetDownloadPackFile(Convert.ToHexString(hashBytes)) + ".json";
                    }
                return packFile;
            }
        }

        bool _canPlay = false;
        bool _canBrowse= false;
        bool _canStart = false;
        bool _canPause = false;
        bool _canStop = false;
        public bool CanPlay
        {
            set
            {
                if (_canPlay == value)
                    return;
                _canPlay = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanPlay)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
            get { return _canPlay; }
        }
        public bool CanBrowse
        {
            set
            {
                if (_canBrowse == value)
                    return;
                _canBrowse = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanBrowse)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
            get { return _canBrowse; }
        }
        public bool CanStart
        {
            set
            {
                if (_canStart == value)
                    return;
                _canStart = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanStart)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
            get { return _canStart; }
        }
        public bool CanPause
        {
            set
            {
                if (_canPause == value)
                    return;
                _canPause = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanPause)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
            get { return _canPause; }
        }
        public bool CanStop
        {
            set
            {
                if (_canStop == value)
                    return;
                _canStop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanStop)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
            get { return _canStop; }
        }


        public DownloadWatchable(WatchableObject wobj)
        {
            ID = wobj.ID;
            Name = wobj.Name;
            Source = wobj.Url;
            IconFile = wobj.LogoFileName;
            var extension = Source.Substring(Source.LastIndexOf('.'));
            var path = IpTvConfig.Config.DownloadFolder;
            if (wobj is TvShowWatchableObject tv)
            {
                path = Path.Combine(path, tv.Group);
                Directory.CreateDirectory(path);
                path = Path.Combine(path, $"Season {tv.Season}");
                Directory.CreateDirectory(path);
            }
            Destination = Path.Combine(path, Name.ReplaceInvalidFileNameCharactersWithSpace()) + extension;
        }

        public DownloadWatchable()
        {

        }

        private DownloadConfiguration createOption()
        {
            return new DownloadConfiguration()
            {
                ChunkCount = IpTvConfig.Config.downloadManager.MaxChuck,
                ParallelDownload = true,
                ParallelCount = 4,
            };
        }

        private DownloadService getService()
        {
            if (_service == null)
            {
                _service = new DownloadService(createOption());
                _service.DownloadStarted += Service_DownloadStarted;
                _service.DownloadFileCompleted += Service_DownloadFileCompleted;
                _service.DownloadProgressChanged += Service_DownloadProgressChanged;
                _service.ChunkDownloadProgressChanged += Service_ChunkDownloadProgressChanged;
                if (package != null)
                {
                    _service.Package = package;
                }
                ServiceStatus = _service.Package.IsSaveComplete ? EStatus.COMPLETED : EStatus.READY;
                resetButtons();
                reCreateServiceOnStart = false;
            }
            return _service;
        }

        [JsonIgnore]
        public object GetIcon
        {
            get
            {
                var ff = IpTvConfig.GetImagePath(IconFile);
                if (File.Exists(ff))
                {
                    return ff;
                }
                return MainWindow.PlayIcon;
            }
        }

        public async void Start(Action onFinish)
        {
            if (reCreateServiceOnStart)
            {
                _service = null;
            }
            var service = getService();
            _trigger = onFinish;
            if (service.Package.IsSaveComplete)
            {
                if (MessageBox.Show($"{Name} is already completed.", "Do you want to download it again?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }
            }
            switch (service.Status)
            {
                case DownloadStatus.Paused:
                    Resume();
                    break;
                case DownloadStatus.Running:
                case DownloadStatus.Completed:
                    break;
                default:
                    {
                        CanStart = false;
                        if (package == null)
                            await getService().DownloadFileTaskAsync(Source, Destination).ConfigureAwait(false);
                        else
                            await getService().DownloadFileTaskAsync(package).ConfigureAwait(false);
                    }
                    break;
            }
        }

        public void Resume()
        {
            getService().Resume();
            CanStart = false;
            CanPause = true;
        }

        public void Pause()
        {
            getService().Pause();
            CanPause = false;
            CanStart = true;
        }

        public async void Stop()
        {
            await getService().CancelTaskAsync().ConfigureAwait(false);
        }

        private long _recievedBytes = 0;
        private long _totalBytes = 0;
        private int _activeChunks = 0;

        public string RecievedSize { get; private set; } = "";
        public string TotalSize { get; private set; } = "";

        [JsonIgnore]
        public string Speed { get; private set; } = "";

        public string Status => ServiceStatus switch
        {
            EStatus.RUNNING => $"download is running with {_activeChunks} active chunk(s)...",
            EStatus.USERCANCELLED => "stopped...",
            EStatus.COMPLETED => "completed...",
            EStatus.ERROR => "error occurs...",
            _ => "ready to download..."
        };

        private Action? _trigger = null;
        public double Percent
        {
            get => getService().Package.SaveProgress;
            set { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Percent))); }
        }
        public long RecievedBytes
        {
            get { return _recievedBytes; }
            set
            {
                _recievedBytes = value;
                RecievedSize = $"{_recievedBytes.toSize()} [%{Percent:f2}]";
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecievedSize)));
            }
        }

        private double _speed
        {
            set
            {
                Speed = value.toSize();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Speed)));
            }
        }

        public long TotalBytes
        {
            get => _totalBytes;
            set
            {
                _totalBytes = value;
                TotalSize = _totalBytes.toSize();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalSize)));
            }
        }

        private void resetButtons()
        {
            var succesfully = getService().Package.IsSaveComplete;
            CanStart = succesfully == false;
            CanPause = false;
            CanStop = false;
            CanBrowse = succesfully == true;
            CanPlay = succesfully == true;
        }

        private void Service_DownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Console.WriteLine($"[DownloadManager] {Name} is stopped.");
                ServiceStatus = EStatus.USERCANCELLED;
            }
            else if (e.Error != null)
            {
                Console.WriteLine($"[DownloadManager] {Name} has stopped with error : {e.Error.Message}");
                ServiceStatus = EStatus.ERROR;
            }
            else
            {
                Console.WriteLine($"[DownloadManager] {Name} is completed.");
                ServiceStatus = EStatus.COMPLETED;
            }
            resetButtons();
            _trigger?.Invoke();
        }

        private void Service_DownloadProgressChanged(object? sender, Downloader.DownloadProgressChangedEventArgs e)
        {
            RecievedBytes = e.ReceivedBytesSize;
            _speed = e.BytesPerSecondSpeed;
            Percent = e.ProgressPercentage;
        }

        private void Service_DownloadStarted(object? sender, DownloadStartedEventArgs e)
        {
            if (package == null)
            {
                package = getService().Package;
            }
            Console.WriteLine($"[DownloadManager] {Name} is started to download.");
            TotalBytes = e.TotalBytesToReceive;
            ServiceStatus = EStatus.RUNNING;
            CanPause = true;
            CanStop = true;
            CanPlay = false;
            CanBrowse = false;
            _trigger?.Invoke();
        }
        private void Service_ChunkDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
        {
            _activeChunks = e.ActiveChunks;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }

        public void Clear()
        {
            getService().Clear();
        }

        bool reCreateServiceOnStart = false;
        public void setChunk(int value)
        {
            if (_activeChunks != value)
            {
                reCreateServiceOnStart = true;
            }
        }

        public EStatus ServiceStatus
        {
            get; set;
        } = EStatus.READY;
        public enum EStatus { READY, RUNNING, USERCANCELLED, COMPLETED, ERROR };
    }
}
