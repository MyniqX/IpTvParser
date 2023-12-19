using IpTvParser.Controls;
using IpTvParser.Helpers;
using IpTvParser.IpTvCatalog;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Timer = System.Timers.Timer;

namespace IpTvParser
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{

		public static CoverDownloader coverDownloader = new();
		public DownloadManager downloadManager { get; private set; }
		public MainWindow()
		{
			PlayIcon = (DrawingImage)TryFindResource("playDrawingImage");
			FilmIcon = (DrawingImage)TryFindResource("filmDrawingImage");
			GroupIcon = (DrawingImage)TryFindResource("folderDrawingImage");
			TvShowIcon = (DrawingImage)TryFindResource("tvshowDrawingImage");
			WatchIcon = (DrawingImage)TryFindResource("heartDrawingImage");
			WatchedIcon = (DrawingImage)TryFindResource("checkDrawingImage");
			LiveStreamIcon = (DrawingImage)TryFindResource("livestreamDrawingImage");
			UnmarkIcon = (DrawingImage)TryFindResource("crossDrawingImage");
			SearchIcon = (DrawingImage)TryFindResource("searchDrawingImage");
			DownloadIcon = (DrawingImage)TryFindResource("downloadDrawingImage");
			CopyIcon = (DrawingImage)TryFindResource("copyDrawingImage");
			InfoIcon = (DrawingImage)TryFindResource("infoDrawingImage");
			RefreshIcon = (DrawingImage)TryFindResource("refreshDrawingImage");
			DeleteIcon = (DrawingImage)TryFindResource("trashDrawingImage");
			PauseIcon = (DrawingImage)TryFindResource("pauseDrawingImage");
			StopIcon = (DrawingImage)TryFindResource("stopDrawingImage");
			UserIcon = (DrawingImage)TryFindResource("users_altDrawingImage");
			HotIcon = (DrawingImage)TryFindResource("flameDrawingImage");
			NullIcon = (DrawingImage)TryFindResource("");

			Console.SetOut(new ConsoleOut(this));
			Config = IpTvConfig.Init();
			downloadManager = Config.downloadManager;
			InitializeComponent();

			toastTimer = new();
			toastTimer.Enabled = false;
			toastTimer.Elapsed += ToastTimer_Elapsed;
		}

		private void ToastTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
		{
			toastTimer.Enabled = false;
			toast_panel.Dispatcher.Invoke(() => toast_panel.Visibility = Visibility.Collapsed);
		}

		private IpTvConfig Config { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public static DrawingImage PlayIcon, PauseIcon, HotIcon, StopIcon, UserIcon, FilmIcon, GroupIcon, LiveStreamIcon, TvShowIcon, WatchIcon, WatchedIcon, NullIcon = new(), UnmarkIcon, CopyIcon, RefreshIcon, DownloadIcon, InfoIcon, DeleteIcon, SearchIcon;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		private void Window_Initialized(object sender, EventArgs e)
		{
			profilesList.ItemsSource = Config.Profiles;
			ui_cfg_cache_size.DataContext = coverDownloader;
			toast_panel.Visibility = Visibility.Collapsed;
			downloadList.ItemsSource = downloadManager.downloadList;
			chunk_count.Value = downloadManager.MaxChuck;
			chunk_count.ValueChanged += download_chunk_changed;
			download_count.Value = downloadManager.MaxDownload;
			download_count.ValueChanged += download_count_changed;
			tabControl.SelectedIndex = 1;

			var timer = new Timer();
			var lastSpeed = 0.0;
			timer.Interval = 500;
			timer.Enabled = true;
			timer.Elapsed += (o, e) =>
			{
				var speed = downloadManager.getTotalSpeed();
				if (speed != lastSpeed)
				{
					try
					{
						download_speed.Dispatcher.Invoke(() => download_speed.Text = speed.toSize());
						lastSpeed = speed;
					}
					catch { }
				}
			};
		}

		private void viewlist_context_menu(object sender, EventArgs e)
		{
			var item = listView.SelectedItem as ViewObject;
			if (item == null)
			{
				return;
			}
			if (listView.ContextMenu == null)
				listView.ContextMenu = new ContextMenu();
			var menu = listView.ContextMenu;
			menu.Items.Clear();

			var open = new MenuItem()
			{
				Icon = new Image{ Source = PlayIcon },
				Header = item is WatchableObject ? "Open" : "Expand"
			};
			open.Click += (o, e) =>
			{
				if (item is GroupObject group)
				{
					setView(group);
				}
				else if (item is WatchableObject watchable)
				{
					if (IpTvConfig.PlayerFile == null)
					{
						toast("we couldnt find any movie player registered to system.");
						return;
					}
					Process.Start(IpTvConfig.PlayerFile, watchable.Url);
				}
			};
			menu.Items.Add(open);
			menu.Items.Add(new Separator());

			if (item is WatchableObject wobj)
			{
				var watch = new MenuItem()
				{
					Icon =new Image { Source = WatchIcon } ,
					Header = "Mark as Watch",
					IsEnabled = wobj.eList != WatchableObject.EList.WATCH
				};
				var watched = new MenuItem()
				{
					Icon =new Image{ Source = WatchedIcon } ,
					Header = "Mark as Watched",
					IsEnabled = wobj.eList !=WatchableObject.EList.WATCHED
				};
				var unmark = new MenuItem()
				{
					Icon =new Image{ Source = UnmarkIcon } ,
					Header = "Unmark",
					IsEnabled = wobj.eList != WatchableObject.EList.NONE
				};
				watch.Click += (o, e) => currentProfile?.WacthList(wobj);
				watched.Click += (o, e) => currentProfile?.WacthedList(wobj);
				unmark.Click += (o, e) => currentProfile?.RemoveFromList(wobj);

				menu.Items.Add(watch);
				menu.Items.Add(watched);
				menu.Items.Add(unmark);
				menu.Items.Add(new Separator());
			}
			if (item is WatchableObject or TvShowGroupObject or TvShowSeasonGroupObject)
			{
				var download = new MenuItem()
				{
					Icon =new Image{ Source = DownloadIcon } ,
					Header = item is WatchableObject ? "Download" : "Download All"
				};
				download.Click += (o, e) =>
				{
					int count = 0;
					if (item is WatchableObject w)
					{
						if (downloadManager.AddQueue(w))
							count++;
					}
					else if (item is GroupObject g)
						foreach (var i in g.Iterator)
							if (downloadManager.AddQueue(i))
								count++;
					toast($"{count} item(s) added to download queue");
				};
				menu.Items.Add(download);
				menu.Items.Add(new Separator());

				var google = new MenuItem()
				{
					Icon = new Image{ Source = SearchIcon },
					Header = "Search on Google"
				};
				var imdb = new MenuItem()
				{
					Icon =new Image{ Source = SearchIcon } ,
					Header = "Search on IMDB"
				};
				google.Click += (o, e) => SearchOn("https://www.google.com/search?q={0}", item.Name);
				imdb.Click += (o, e) => SearchOn("https://www.imdb.com/find?q={0}&s=all", item.Name);

				menu.Items.Add(google);
				menu.Items.Add(imdb);
				menu.Items.Add(new Separator());
			}
			if (item is WatchableObject w)
			{
				var copy = new MenuItem()
				{
					Icon = new Image {Source = CopyIcon },
					Header = "Copy URL"
				};
				copy.Click += (o, e) =>
				{
					Clipboard.SetText(w.Url);
					toast("Url copied to clipboard.");
				};
				menu.Items.Add(copy);
			}
			var details = new MenuItem()
			{
				Icon = new Image{ Source = InfoIcon },
				Header = "Details"
			};
			details.Click += (e, o) => MessageBox.Show(item.ToString());
			menu.Items.Add(details);
		}

		private void SearchOn(string format, string text)
		{
			var searchUrl = string.Format(format,Uri.EscapeDataString(text));
			Process.Start(new ProcessStartInfo(searchUrl)
			{
				UseShellExecute = true,
				Verb = "open"
			});
		}

		private void profile_right_click_menu(object sender, EventArgs e)
		{
			var item = profilesList.SelectedItem as Profile;
			if (item == null)
				return;
			if (profilesList.ContextMenu == null)
				profilesList.ContextMenu = new ContextMenu();
			var menu = profilesList.ContextMenu;
			menu.Items.Clear();
			var load = new MenuItem()
			{
				Icon = new Image { Source = PlayIcon },
				Header = "Load"
			};
			load.Click += (o, e) => loadProfile(item);
			menu.Items.Add(load);
			menu.Items.Add(new Separator());
			if (item.isLocal == false)
			{
				var upload = new MenuItem()
				{
					Icon = new Image{Source = RefreshIcon},
					Header="Upload"
				};
				upload.Click += async (o, e) => await item.loadProfile().ConfigureAwait(false);
				menu.Items.Add(upload);
				menu.Items.Add(new Separator());
			}
			var remove = new MenuItem()
			{
				Icon = new Image {Source = DeleteIcon},
				Header="Remove"
			};
			remove.Click += (o, e) =>
			{
				if (MessageBox.Show("Choosen profile ll be deleted!", "Remove this profile!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
				{
					Config.removeProfile(item);
				}
			};
			menu.Items.Add(remove);
		}

		Timer toastTimer;
		public void toast(string msg, int timeout = 5000)
		{
			Console.WriteLine($"[toast] {msg}");
			if (toastTimer.Enabled == true)
				msg = $"{toast_text.Text}{Environment.NewLine}{Environment.NewLine}{msg}";
			else
				toastTimer.Enabled = true;
			toastTimer.Interval = timeout;
			toast_text.Text = msg;
			toast_panel.Visibility = Visibility.Visible;
		}

		private void treeView_left_click(object sender, MouseButtonEventArgs e)
		{
			TreeViewItem? clickedItem = null;
			DependencyObject current = (DependencyObject)e.OriginalSource;
			do
			{
				if (current is TreeViewItem found)
				{
					clickedItem = found;
					break;
				}
				current = VisualTreeHelper.GetParent(current);
			} while (current != null);

			if (treeView.ContextMenu == null)
				treeView.ContextMenu = new ContextMenu();
			var menu = treeView.ContextMenu;
			menu.Items.Clear();
			if (clickedItem?.DataContext is GroupObject group)
			{
				var expand = new MenuItem
				{
					Icon = new Image() {Source = PlayIcon},
					Header="Expand"
				};
				var hide = new MenuItem
				{
					Icon = new Image() {Source = UnmarkIcon},
					Header= $"Hide {group.Name}"
				};
				expand.Click += (_o, _e) =>
				{
					setView(group);
				};
				hide.Click += (_o, _e) =>
				{
					currentProfile?.bannedGroups.Add(group.Name);
					clickedItem.Visibility = Visibility.Collapsed;
				};
				menu.Items.Add(expand);
				if (currentProfile?.catalog.movieList.Members.Contains(group) == true)
					menu.Items.Add(hide);
			}
			if (currentProfile != null && currentProfile.bannedGroups.Count > 0)
			{
				if (menu.Items.Count != 0)
					menu.Items.Add(new Separator());
				var menuGroup = new MenuItem
				{
					Icon = new Image {Source = (ImageSource?)UnmarkIcon},
					Header = "UnHide"
				};
				foreach (var banitem in currentProfile.bannedGroups)
				{
					var item = new MenuItem
					{
						Icon = new Image { Source = (ImageSource?)UnmarkIcon},
						Header = banitem.ToString()
					};
					item.Click += (_, _) =>
					{
						currentProfile.bannedGroups.Remove(banitem);
						currentProfile.catalog.movieList.notifyGMember();
					};
					menuGroup.Items.Add(item);
				}
				if (currentProfile.bannedGroups.Count > 1)
				{
					menuGroup.Items.Add(new Separator());
					var  item = new MenuItem
					{
						Icon = new Image {Source = (ImageSource?)UnmarkIcon},
						Header = "Unhide All"
					};
					item.Click += (_, _) =>
					{
						currentProfile.bannedGroups.Clear();
						currentProfile.catalog.movieList.notifyGMember();
					};
					menuGroup.Items.Add(item);
				}
				menu.Items.Add(menuGroup);
			}
			if (menu.Items.Count == 0)
			{
				var item = new MenuItem
				{
					Icon = new Image{Source = UserIcon},
					Header= "Show Profiles to Load"
				};
				item.Click += (_, _) =>
				{
					tabControl.SelectedIndex = 1;
				};
				menu.Items.Add(item);
			}
		}

		private void download_content_menu(object sender, EventArgs e)
		{
			if (downloadList.SelectedItem == null)
				return;
			if (downloadList.ContextMenu == null)
				downloadList.ContextMenu = new ContextMenu();
			var menu = downloadList.ContextMenu;
			menu.Items.Clear();

			var start = new MenuItem()
			{
				Icon = new Image {Source = PlayIcon},
				Header="Start"
			};
			var pause = new MenuItem()
			{
				Icon = new Image {Source = PauseIcon},
				Header="Pause"
			};
			var stop = new MenuItem()
			{
				Icon = new Image {Source = StopIcon},
				Header="Stop"
			};
			var remove = new MenuItem()
			{
				Icon = new Image {Source = DeleteIcon },
				Header = "Remove"
			};

			menu.Items.Add(start);
			menu.Items.Add(pause);
			menu.Items.Add(stop);
			menu.Items.Add(new Separator());
			menu.Items.Add(remove);
			menu.Items.Add(new Separator());

			var startall = new MenuItem()
			{
				Icon = new Image {Source = PlayIcon},
				Header="Start All"
			};
			var pauseall = new MenuItem()
			{
				Icon = new Image {Source = PauseIcon},
				Header="Pause All"
			};
			var stopall = new MenuItem()
			{
				Icon = new Image {Source = StopIcon},
				Header="Stop All"
			};
			var removeall = new MenuItem()
			{
				Icon = new Image {Source =    DeleteIcon },
				Header="Remove All"
			};

			menu.Items.Add(startall);
			menu.Items.Add(pauseall);
			menu.Items.Add(stopall);
			menu.Items.Add(new Separator());
			menu.Items.Add(removeall);
		}

		class ConsoleOut : TextWriter
		{
			readonly MainWindow mw;
			public
			ConsoleOut(MainWindow mw)
			{
				this.mw = mw;
			}

			public override Encoding Encoding => Encoding.UTF8;

			public override void WriteLine(string? value)
			{
				mw.Dispatcher.BeginInvoke(() =>
				{
					mw.outPut.AppendText(value);
					mw.outPut.AppendText(Environment.NewLine);
					mw.outPut.ScrollToEnd();
				});
			}

			public override void Write(string? value)
			{
				if (string.IsNullOrEmpty(value) == false)
					mw.Dispatcher.BeginInvoke(() =>
						mw.toast(value));
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			downloadManager.StopAll();
			Config.Save();
		}

		void CatalogsUpdate(Profile? profile)
		{
			treeView.ItemsSource = null;
			searchList.Items.Clear();
			if (profile != null)
			{
				treeView.ItemsSource = profile.catalog.GMembers;
				try
				{
					if (treeView.ItemContainerGenerator.ContainerFromIndex(2) is TreeViewItem ti)
						ti.IsExpanded = true;
				}
				catch { }
				searchList.Items.Clear();
				searchList.Items.Add("All");
				foreach (var item in profile.catalog.GMembers)
				{
					searchList.Items.Add(item.Name);
				}
				searchList.SelectedIndex = 0;
				toast($"{profile.Name} is loaded.");
				tabControl.SelectedItem = tabCategories;
			}
			viewPanel.IsEnabled = profile != null;
			upperLevel = null;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBackAvailable)));
		}

		Profile? currentProfile = null;
		GroupObject? upperLevel = null;

		public bool IsBackAvailable => upperLevel != null;

		private void btn_add_Click(object sender, MouseButtonEventArgs e)
		{
			string path = btn_add_text.Text;
			if (string.IsNullOrEmpty(path))
				return;
			btn_add_text.Text = "";
			loadProfile(Config.GetProfileFrom(path));
		}

		async void loadProfile(Profile p, bool force = false)
		{
			Dispatcher.Invoke(() =>
			{
				btn_add.IsEnabled = false;
				profilesList.IsEnabled = false;
			});
			if (await p.loadProfile(force).ConfigureAwait(false) == true)
			{
				currentProfile = p;
				treeView.Dispatcher.Invoke(() => CatalogsUpdate(p));
			}
			Dispatcher.Invoke(() =>
			{
				btn_add.IsEnabled = true;
				profilesList.IsEnabled = true;
			});
		}

		private void btn_browse_Click(object sender, MouseButtonEventArgs e)
		{
			OpenFileDialog openFileDialog = new();
			openFileDialog.Multiselect = false;
			openFileDialog.InitialDirectory = SpecialDirectories.MyDocuments;

			var result = openFileDialog.ShowDialog();
			if (result == false)
				return;

			btn_add_text.Text = openFileDialog.FileName;
		}

		private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (treeView.SelectedItem is GroupObject item)
			{
				setView(item);
			}
		}

		void setView(GroupObject? group)
		{
			upperLevel = group?.UpperLevel ?? null;
			listView.ItemsSource = group?.Members ?? null;
			coverDownloader.clearList();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBackAvailable)));
			StringBuilder title = new(128);
			title.Append("IPTv Catalog ");
			if (group != null && currentProfile != null)
			{
				int insetPos = title.Length;
				for (var item = group; item != null && item != currentProfile.catalog.movieList; item = item.UpperLevel)
				{
					title.Insert(insetPos, item.Name);
					title.Insert(insetPos, " > ");
				}
			}
			this.Title = title.ToString();
		}

		private void profiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (profilesList.SelectedItem is Profile selected)
			{
				loadProfile(selected);
			}
		}

		private void ProfileItem_Load_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is Profile selected)
			{
				profilesList.SelectedItem = selected;
				loadProfile(selected);
			}
		}

		private void ProfileItem_Reload_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is Profile selected)
			{
				profilesList.SelectedItem = selected;
				loadProfile(selected, true);
			}
		}

		private void ProfileItem_Delete_Click(object sender, RoutedEventArgs e)
		{
			if (profilesList.SelectedItem is Profile selected)
			{
				if (MessageBox.Show("Seçmiş olduğunuz profil silinecek!", "Dikkat!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
				{
					Config.removeProfile(selected);
				}
			}
		}

		private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (listView.SelectedItem is GroupObject group)
			{
				setView(group);
			}
			else if (listView.SelectedItem is WatchableObject watchable)
			{
				IpTvConfig.StartPlayer(watchable.Url);
			}
		}

		Thread? searchThread = null;
		CancellationTokenSource ? cancellationToken = null;

		public event PropertyChangedEventHandler? PropertyChanged;

		private void searchText_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (currentProfile == null)
				return;
			string text = searchText.Text;
			if (text.Length < 4)
			{
				setView(null);
				return;
			}
			cancellationToken?.Cancel();
			searchThread?.Join();
			cancellationToken?.Dispose();
			var catalog = currentProfile.catalog;
			var group = catalog.getSearchObject(searchList.SelectedIndex);
			cancellationToken = new CancellationTokenSource();
			searchThread = new Thread(() => searchProcess(cancellationToken, text, group));
			searchThread.Start();
		}

		private void searchProcess(CancellationTokenSource cancellationToken, string text, GroupObject group)
		{
			List<ViewObject> watchableObjects = new();
			var param = text.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			group.SearchThrough(param, watchableObjects, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
				return;
			var temp = new GroupObject
			{
				Name = "Search Result",
				UpperLevel = null,
				Members = watchableObjects
			};
			searchText.Dispatcher.Invoke(() => setView(temp));
		}

		private void ui_cfg_open_settings_folder(object sender, RoutedEventArgs e)
		{
			IpTvConfig.OpenFolder(IpTvConfig.DataFolder);
		}
		private void ui_cfg_cleancache(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("All cache files ll be removed!", "Clean Cache", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				coverDownloader.cleanCache();
			}
		}

		private void download_start_button(object sender, MouseButtonEventArgs e)
		{
			if (sender is IconButton button && button.DataContext is DownloadWatchable item)
			{
				listView.SelectedItem = item;
				downloadManager.Start(item);
			}
		}

		private void download_pause_button(object sender, MouseButtonEventArgs e)
		{
			if (sender is IconButton button && button.DataContext is DownloadWatchable item)
			{
				listView.SelectedItem = item;
				downloadManager.Pause(item);
			}
		}
		private void download_stop_button(object sender, MouseButtonEventArgs e)
		{
			if (sender is IconButton button && button.DataContext is DownloadWatchable item)
			{
				listView.SelectedItem = item;
				downloadManager.Stop(item);
			}
		}
		private void download_remove_button(object sender, MouseButtonEventArgs e)
		{
			if (sender is IconButton button && button.DataContext is DownloadWatchable item)
			{
				listView.SelectedItem = item;
				downloadManager.Remove(item);
			}
		}

		private void download_open_folder_button(object sender, MouseButtonEventArgs e)
		{
			if (sender is IconButton button && button.DataContext is DownloadWatchable item)
			{
				listView.SelectedItem = item;
				IpTvConfig.OpenFolder(item.Destination);
			}
		}
		private void download_open_file_button(object sender, MouseButtonEventArgs e)
		{
			if (sender is IconButton button && button.DataContext is DownloadWatchable item)
			{
				listView.SelectedItem = item;
				IpTvConfig.StartPlayer(item.Destination);
			}
		}

		private void view_back_button(object sender, MouseButtonEventArgs e)
		{
			if (upperLevel == null)
				return;
			setView(upperLevel);
		}
		private void download_remove_if_completed_button(object sender, MouseButtonEventArgs e)
		{
			downloadManager.RemoveIfCompleted();
		}
		private void download_start_all_button(object sender, MouseButtonEventArgs e)
		{
			downloadManager.StartQueue();
		}
		private void download_pause_all_button(object sender, MouseButtonEventArgs e)
		{
			downloadManager.PauseAll();
		}
		private void download_stop_all_button(object sender, MouseButtonEventArgs e)
		{
			downloadManager.StopAll();
		}
		private void download_remove_all_button(object sender, MouseButtonEventArgs e)
		{
			downloadManager.RemoveAll();
		}
		private void download_count_changed(NumberPickerValueChangedEventArg e)
		{
			downloadManager.setDownloadCount(e.Value);
		}
		private void download_chunk_changed(NumberPickerValueChangedEventArg e)
		{
			downloadManager.setChunkCount(e.Value);
		}
	}
}
