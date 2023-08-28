using IpTvParser.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static IpTvParser.IpTvCatalog.WatchableObject;

namespace IpTvParser.IpTvCatalog
{

	internal class Catalog : GroupObject
	{
		public GroupObject recentlyAdded;
		public GroupObject watchedList;
		public GroupObject watchList;
		public GroupObject movieList;
		Dictionary<string,M3UObject>? m3uMap;
		public Profile profile { get; set; }

		public Catalog(Profile p)
		{
			profile = p;
			Name = "Catalog";
			recentlyAdded = AddGroup("Recently");
			watchList = AddGroup("Watch List");
			watchedList = AddGroup("Watched List");
			movieList = AddGroup("Iptv List");
			recentlyAdded.GetIcon = MainWindow.HotIcon;
			watchedList.GetIcon = MainWindow.WatchedIcon;
			watchList.GetIcon = MainWindow.WatchIcon;
			movieList.GetIcon = MainWindow.FilmIcon;
		}

		protected override bool shouldFilter(ViewObject v)
		{
			return v is GroupObject g && profile.bannedGroups.Contains(g.Name) == false;
		}

		public void AddM3UList(List<M3UObject> list)
		{
			if (m3uMap != null)
			{
				var addedlist = AddWithFilter(list);
				if (addedlist == null || addedlist.Count == 0)
					return;
				var date = DateTime.Now;
				foreach (var item in addedlist)
				{
					item.AddedDate = date;
					recentlyAdded.Add(item.GetM3UObject);
				}
				recentlyAdded.lastCheck();
				profile.latelyAdded[date] = addedlist.OrderBy(a => a.Name).Select(a => a.ID).ToList();
			}
			else
			{
				AddAndBuildFilter(list);
			}
		}

		private void AddAndBuildFilter(List<M3UObject> list)
		{
			var sb = new StringBuilder(128);
			m3uMap = new Dictionary<string, M3UObject>(list.Count + 5);
			foreach (var item in list)
			{
				m3uMap[item.getID(sb)] = item;
				Add(item);
			}

			foreach (var pair in profile.listedItems)
			{
				var n = pair.Key.Split(WatchableObject.Tok);
				var wObj = findObject(n[0],n[1]);
				if (wObj == null)
					continue;
				wObj.eList = pair.Value;
				if (pair.Value == EList.WATCH)
					watchList.justAdd(wObj);
				else
					watchedList.justAdd(wObj);
			}

			var remove = profile.latelyAdded.Where(a => DateTime.Now.Subtract(a.Key).Days > 31).Select(a => a.Key).ToList();
			foreach (var key in remove)
				profile.latelyAdded.Remove(key);

			foreach (var pair in profile.latelyAdded)
			{
				var date = pair.Key;
				foreach (var line in pair.Value)
				{
					var n = line.Split(WatchableObject.Tok);
					var wObj = findObject(n[0],n[1]);
					if (wObj == null)
						continue;
					wObj.AddedDate = date;
					recentlyAdded.Add(wObj.GetM3UObject);
				}
			}
			lastCheck();
		}

		private List<WatchableObject>? AddWithFilter(List<M3UObject> list)
		{
			if (m3uMap == null)
				return null;
			var sb = new StringBuilder(128);
			List<WatchableObject> addedlist = new();
			foreach (var item in list)
			{
				if (m3uMap.ContainsKey(item.getID(sb)))
					continue;
				var added = Add(item);
				addedlist.Add(added);
			}
			return addedlist;
		}

		public override WatchableObject Add(M3UObject m3u)
		{
			return movieList.AddGroup(m3u.GroupTitle).Add(m3u);
		}

		public override void lastCheck()
		{
			recentlyAdded.lastCheck();
			watchedList.lastCheck();
			watchList.lastCheck();
			movieList.lastCheck();
		}

		public GroupObject getSearchObject(int index)
		{
			if (index <= 0)
				return movieList;
			if (--index >= movieList.Count)
				return movieList;
			var obj = movieList.Members[index] as GroupObject;
			return obj ?? movieList;
		}

		public WatchableObject? findObject(string group, string name)
		{
			GroupObject? groupObject = movieList.Members.Find(m => string.Compare(m.Name,group) == 0) as GroupObject;
			return groupObject?.findObject(name);
		}

	}
}
