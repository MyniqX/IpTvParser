using IpTvParser.Helpers;
using System;
using System.Text;

namespace IpTvParser.IpTvCatalog
{
	public class WatchableObject : ViewObject
	{
		public string Url = "";

		public string Group = "";

		public DateTime? AddedDate { get; set; } = null;
		public string DateDiff => AddedDate != null ? $"{DateTime.Now.Subtract((DateTime)AddedDate).Days} days ago." : "never";
		public enum EList { NONE, WATCH, WATCHED };

		public M3UObject GetM3UObject => new()
		{
			GroupTitle = Group,
			TvgLogo = Logo,
			TvgName = Name,
			UrlTvg = Url,
			Date = AddedDate
		};

		public override object GetListIcon
		{
			get => _elist switch
			{
				EList.WATCH => MainWindow.WatchIcon,
				EList.WATCHED => MainWindow.WatchedIcon,
				_ => MainWindow.NullIcon
			};
		}

		EList _elist = EList.NONE;
		public EList eList
		{
			get => _elist;
			set
			{
				_elist = value;
				propertyChanged
					(nameof(GetListIcon));
			}
		}

		public override object GetIcon => PossibleLiveStream ? MainWindow.LiveStreamIcon : MainWindow.FilmIcon;
		public override string GetTitle => PossibleLiveStream ? "LiveStream" : "Movie";

		bool? possibleLiveStream = null;

		public bool PossibleLiveStream
		{
			get
			{
				if (possibleLiveStream == null)
				{
					int i = Url.LastIndexOf('/');
					possibleLiveStream = (i >= 0 ? Url.IndexOf('.', i) == -1 : false);
				}
				return possibleLiveStream == true;
			}
		}

		public static string Tok = "$@$";

		public string ID { get; set; } = "";
		public override string ToString()
		{
			StringBuilder sb = new();
			sb.Append("Group : ").Append(Group).AppendLine();
			sb.Append("Name  : ").Append(Name).AppendLine();
			sb.Append("Url   : ").Append(Url).AppendLine();
			sb.Append("Logo  : ").Append(Logo).AppendLine();
			return sb.ToString();
		}
		public bool hasMatch(string txt)
		{
			int lB = txt.Length;
			int lA = Name.Length-lB;

			for (int i = 0; i < lA; i++)
			{
				bool found = true;
				for (int j = 0; j < lB; j++)
				{
					char cA = Name[i+j];
					char cB = txt[j];
					if (charMatch(cA, cB) == false)
					{
						found = false;
						break;
					}
				}
				if (found)
					return true;
			}
			return false;
		}

		public bool hasMatchex(string txt)
		{
			int lenA = Name.Length;
			int lenB = txt.Length;
			int indexA = 0;
			int indexB = 0;
			int found = 0;
			int miss  = 0;
			char cB = txt[indexB];
			for (int i = 0; ; i++)
			{
				if (i >= lenA || (found > 0 && i - indexA > 4))
				{
					if (++indexB >= lenB || found == 0)
						break;

					cB = txt[indexB];
					miss++;
					i = indexA;
					continue;
				}

				char cA = Name[i];
				if (charMatch(cA, cB))
				{
					//bulunan karakter sayısı txt ile eşitse tam eşleşme var
					if (++found == lenB)
						return true;

					//bulunan karakter ve kaçırılan karakter sayıları eşleşti
					if (found + miss == lenB)
						break;

					//bunun hiç çalışmaması gerek. txt'in tüm elemanlarına bakıldı
					if (++indexB >= lenB)
						break;

					//sonraki txt' elemanına geç
					cB = txt[indexB];

					//isim içindeki indexi işaretle, atlanan karakterden sonra buradan devam edilecek
					indexA = i;

					//ismin içindeki elemanlar tükendi.
					if (indexA >= lenA)
						break;
				}
			}
			return (float)found / (float)lenB >= 0.7f;
		}

		bool charMatch(char a, char b)
		{
			a = char.ToLower(a);
			b = char.ToLower(b);
			if (a == b)
				return true;
			a = toBasic(a);
			b = toBasic(b);
			return a == b;
		}

		char toBasic(char c)
		{
			switch (c)
			{
				case 'ş':
					return 's';
				case 'ç':
					return 'c';
				case 'ı':
					return 'i';
				case 'ğ':
					return 'g';
				case 'ü':
					return 'u';
				case 'ö':
					return 'o';
			}
			return c;
		}

		public override int CompareWith(WatchableObject w)
		{
			if (AddedDate != null)
			{
				if (w.AddedDate != null)
				{
					var comp = DateTime.Compare((DateTime)AddedDate, (DateTime)w.AddedDate);
					return comp == 0 ? Name.CompareTo(w.Name) : comp;
				}
				return -1;
			}
			if (w.AddedDate != null)
				return 1;

			return Name.CompareTo(w.Name);
		}

		public override int CompareWith(GroupObject g)
		{
			if (AddedDate != null)
			{
				return -1;
			}
			return 1;
		}
	}

}
