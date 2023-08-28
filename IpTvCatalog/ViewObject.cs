using IpTvParser.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace IpTvParser.IpTvCatalog
{
	public abstract class ViewObject : INotifyPropertyChanged, IComparable<ViewObject>
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		string _name = "";
		string _logo = "";
		public string Name
		{
			set
			{
				if (string.Compare(_name, value, StringComparison.OrdinalIgnoreCase) == 0)
					return;
				_name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
			}

			get
			{
				return _name;
			}
		}

		public GroupObject? UpperLevel { get; set; } = null;

		public virtual bool isWatchEnabled => false;
		public virtual bool isWatchedEnabled => false;
		public virtual bool isUnmarkEnabled => false;
		public virtual bool isUrlCopiable => false;
		public virtual bool isDownloadable => false;
		public virtual bool isSearchEnabled => false;

		public virtual string Logo
		{
			set
			{
				_logo = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Logo)));
			}

			get
			{
				return _logo;
			}
		}

		[JsonIgnore]
		double _logoPercent = 0;

		[JsonIgnore]
		public virtual object GetIcon { get; set; } = MainWindow.GroupIcon;

		public virtual object GetListIcon { get; set; } = MainWindow.NullIcon;

		public virtual string GetTitle { get; set; } = "";

		[JsonIgnore]
		public double LogoPercent
		{
			get { return _logoPercent; }
			set
			{
				Visibility old = LogoVisible;
				_logoPercent = value;
				if (old != LogoVisible)
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogoVisible)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogoPercent)));
			}
		}

		[JsonIgnore]
		public Visibility LogoVisible
		{
			get => 0 < _logoPercent && _logoPercent < 100 ? Visibility.Visible : Visibility.Hidden;
		}

		protected void propertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		public int CompareTo(ViewObject? other)
		{
			if (other is WatchableObject w2)
				return CompareWith(w2);
			if (other is GroupObject g)
				return CompareWith(g);
			return 0;
		}

		public abstract int CompareWith(WatchableObject w);
		public abstract int CompareWith(GroupObject g);

		string? _logoFileName;
		public void notifyImageChanged()
		{
			propertyChanged(nameof(GetImage));
			propertyChanged(nameof(GetStretchValue));
		}

		[JsonIgnore]
		public string LogoFileName
		{
			get
			{
				if (_logoFileName == null)
				{
					using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
					{
						byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(Logo);
						byte[] hashBytes = md5.ComputeHash(inputBytes);
						_logoFileName = Convert.ToHexString(hashBytes) + ".jpg";
					}
				}
				return _logoFileName;
			}
		}

		public string FullPathOfLogo => IpTvConfig.GetImagePath(LogoFileName);

		[JsonIgnore]
		bool triedOnes = false;

		[JsonIgnore]
		public object GetImage
		{
			get
			{
				if (!string.IsNullOrEmpty(Logo))
				{
					if (File.Exists(FullPathOfLogo))
					{
						return FullPathOfLogo;
					}

					if (triedOnes == false)
					{
						MainWindow.coverDownloader.addDownload(this);
						triedOnes = true;
					}
				}
				return GetIcon;
			}
		}

		public Stretch GetStretchValue
		{
			get
			{
				return GetImage == GetIcon ? Stretch.Uniform : Stretch.UniformToFill;
			}
		}
	}
}
