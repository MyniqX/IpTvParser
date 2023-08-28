using IpTvParser.IpTvCatalog;
using System;
using System.Text;

namespace IpTvParser.Helpers
{
	public class M3UObject
	{
		public string GroupTitle = "";
		public string TvgName = "";
		public string TvgLogo = "";
		public string TvgId = "";
		public string UrlTvg = "";
		public DateTime? Date = null;

		public string getID(StringBuilder? sb = null)
		{
			if (string.IsNullOrEmpty(TvgId) == false)
				return TvgId;
			if (sb == null)
			{
				return TvgId = $"{GroupTitle}{WatchableObject.Tok}{TvgName}";
			}
			sb.Clear();
			sb.Append(GroupTitle);
			sb.Append(WatchableObject.Tok);
			sb.Append(TvgName);
			return TvgId = sb.ToString();
		}

		public override string ToString()
		{
			return TvgName;
		}
	}
}
