using IpTvParser.IpTvCatalog;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace IpTvParser.Helpers
{
	internal class IpTvConfig
	{
		public static readonly string DataFolder =  SpecialDirectories.AllUsersApplicationData.Remove(SpecialDirectories.AllUsersApplicationData.LastIndexOf('\\'));
		public static readonly string ImageFolder = Path.Combine(DataFolder, "images");
		public static readonly string DownloadPackFolder = Path.Combine(DataFolder, "dwnlds");
		public static readonly string M3UFolder = Path.Combine(DataFolder, "m3us");
		public static readonly string ConfigFile = Path.Combine(DataFolder, "config.json");
		public static readonly string DefIconFile = Path.Combine(ImageFolder, "def.png");
		public static string? PlayerFile;
		public static IpTvConfig Config = new();

		public static void OpenFolder(string path)
		{
			try
			{
				var root = Path.GetDirectoryName(path);
				if (root != null)
					path = root;
				Process.Start(new ProcessStartInfo
				{
					FileName = path,
					UseShellExecute = true,
					Verb = "open"
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred: {ex.Message}");
			}
		}

		public static IpTvConfig Init()
		{
			Directory.CreateDirectory(DataFolder);
			Directory.CreateDirectory(ImageFolder);
			Directory.CreateDirectory(DownloadPackFolder);
			Directory.CreateDirectory(M3UFolder);

			// https://stackoverflow.com/questions/1283584/how-do-i-launch-files-in-c-sharp
			const string extPathTemplate = @"HKEY_CLASSES_ROOT\{0}";
			const string cmdPathTemplate = @"HKEY_CLASSES_ROOT\{0}\shell\open\command";
			string ext = ".mpg";
			var extPath = string.Format(extPathTemplate, ext);

			var docName = Registry.GetValue(extPath, string.Empty, string.Empty) as string;
			if (!string.IsNullOrEmpty(docName))
			{
				var associatedCmdPath = string.Format(cmdPathTemplate, docName);
				var associatedCmd =
					Registry.GetValue(associatedCmdPath, string.Empty, string.Empty) as string;

				if (!string.IsNullOrEmpty(associatedCmd))
				{
					PlayerFile = associatedCmd.Replace("%1", "");
				}
			}

			IpTvConfig? tmp = null;
			try
			{
				tmp =
				JsonSerializer.Deserialize<IpTvConfig>(File.ReadAllText(ConfigFile));
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return Config = tmp ?? new IpTvConfig()
			{
				DownloadFolder = SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0)
			};
		}
		public DownloadManager downloadManager { get; set; } = new();
		public ObservableCollection<Profile> Profiles { get; set; } = new();
		public string DownloadFolder { get; set; } = "";

		public IpTvConfig()
		{

		}

		public static string GetImagePath(string path)
		{
			return Path.Combine(ImageFolder, path);
		}

		public static string GetFilePath(string file) => Path.Combine(DataFolder, file);

		internal static string GetM3UFilePath(string m3UFile) => Path.Combine(M3UFolder, m3UFile);

		public static string GetDownloadPackFile(string file) => Path.Combine(DownloadPackFolder, file);

		public void Save()
		{
			var json = JsonSerializer.Serialize(Config,
				new JsonSerializerOptions
				{
					WriteIndented = true
				});
			lock (DataFolder)
			{
				try
				{
					File.WriteAllText(ConfigFile, json);
				}
				catch { }
			}
		}

		public Profile GetProfileFrom(string path)
		{
			FileInfo fi = new(path);
			Profile? p = null;
			if (fi.Exists)
			{
				p = Profiles.FirstOrDefault(p => string.Compare(p.Name, fi.Name) == 0);
				if (p == null)
				{
					int id = (Profiles.Count > 0 ? Profiles.Max(p => p.ID) : 0) + 1;
					p = new Profile(fi, id);
					Profiles.Add(p);
					return p;
				}
				return p;
			}

			p = Profiles.FirstOrDefault(p => string.Compare(p.Name, path) == 0 || string.Compare(p.Url, path) == 0);
			if (p == null)
			{
				int id = (Profiles.Count > 0 ? Profiles.Max(p => p.ID) : 0) + 1;
				p = new Profile(path, id);
				Profiles.Add(p);
				return p;
			}
			return p;
		}

		internal void removeProfile(Profile profile)
		{
			Profiles.Remove(profile);
		}

		[DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
		private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = default);

		public static void StartPlayer(string url)
		{
			if (IpTvConfig.PlayerFile == null)
			{
				Console.Write("mediaplayer is not set.");
				return;
			}
			Process.Start(IpTvConfig.PlayerFile, url);
			Console.Write($"Player started for : {url}");
		}

	}
}
