using IpTvParser.IpTvCatalog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;

namespace IpTvParser.Helpers
{
	public class DownloadManager : INotifyPropertyChanged
	{
		public ObservableCollection<DownloadWatchable> downloadList { get; set; } = new();

		public event PropertyChangedEventHandler? PropertyChanged;

		public int MaxDownload { get; set; } = 2;

		public int MaxChuck { get; set; } = 4;

		[JsonIgnore]
		public bool QueueRunnnig { get; set; } = false;

		public bool AutoStartQueue { get; set; } = false;

		public double getTotalSpeed()
		{
			double total = 0;
			foreach (var item in downloadList)
			{
				if (item.ServiceStatus == DownloadWatchable.EStatus.RUNNING)
					total += item.DownloadSpeed;
			}
			return total;
		}

		protected void NotifyProperty(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		bool queueEnabled = false;
		public void StartQueue()
		{
			queueEnabled = true;
			Console.Write("Queue started.");
			foreach (var item in downloadList)
			{
				if (item.ServiceStatus is DownloadWatchable.EStatus.USERCANCELLED or
					DownloadWatchable.EStatus.ERROR)
					item.ServiceStatus = DownloadWatchable.EStatus.READY;
			}
			runQueue();
		}

		private void runQueue()
		{
			if (queueEnabled == false)
				return;
			if (downloadList.
				Count(item => item.ServiceStatus == DownloadWatchable.EStatus.RUNNING) >= MaxDownload)
				return;
			var item = downloadList.
				FirstOrDefault(item => item.ServiceStatus == DownloadWatchable.EStatus.READY);
			if (item == null)
				return;
			Console.Write($"{item.Name} is started to download.");
			item.Start(runQueue);
		}

		public bool AddQueue(WatchableObject wobj)
		{
			if (downloadList.Any(w => string.Compare(w.ID, wobj.ID) == 0))
				return false;

			downloadList.Add(new DownloadWatchable(wobj));

			IpTvConfig.Config?.Save();

			if (AutoStartQueue)
			{
				StartQueue();
			}

			return true;
		}

		internal void Remove(DownloadWatchable item)
		{
			if (MessageBox.Show($"File ll be removed from list.{Environment.NewLine}{item.Name}", "Remove File", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
				return;
			item.Clear();
			downloadList.Remove(item);
			IpTvConfig.Config?.Save();
		}

		internal void Stop(DownloadWatchable item)
		{
			item.Stop();
		}

		internal void Pause(DownloadWatchable item)
		{
			item.Pause();
		}

		internal void Start(DownloadWatchable item)
		{
			item.Start(runQueue);
		}

		public void PauseAll()
		{
			foreach (var item in downloadList)
			{
				if (item.CanPause)
					item.Pause();
			}
		}

		public void StopAll()
		{
			queueEnabled = false;
			foreach (var item in downloadList)
			{
				if (item.CanStop)
					item.Stop();
			}
		}

		public void RemoveIfCompleted()
		{
			var completed = downloadList.Where(item => item.ServiceStatus == DownloadWatchable.EStatus.COMPLETED).ToList();
			foreach (var item in completed)
				downloadList.Remove(item);
		}

		public void RemoveAll()
		{
			if (MessageBox.Show($"All Files ll be removed from list", "Remove Files", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
				return;
			foreach (var item in downloadList)
				item.Clear();
			downloadList.Clear();
			IpTvConfig.Config?.Save();
		}

		public void setChunkCount(int value)
		{
			if (MaxChuck != value)
			{
				foreach (var item in downloadList)
					item.setChunk(value);
				MaxChuck = value;
			}
		}

		public void setDownloadCount(int value)
		{
			if (MaxDownload < value)
			{
				runQueue();
			}
			MaxDownload = value;
		}
	}
}
