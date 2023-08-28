using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IpTvParser.Helpers
{
	public class M3UFileParser
	{
		private FileInfo _fileInfo { get; set; }

		long _fileSize;
		long _fileCursor;
		double _lastPercent;

		public M3UFileParser(FileInfo fi)
		{
			_fileInfo = fi;
			_fileSize = fi.Length;
			_lastPercent = 0;
		}

		string line1st="",line2nd="";

		public event EventHandler<double>? ProgressChanged;

		private async Task<bool> readElement(StreamReader _stream)
		{
			var line = await _stream.ReadLineAsync().ConfigureAwait(false);
			if (line == null)
				return false;
			line1st = line;
			line = await _stream.ReadLineAsync().ConfigureAwait(false);
			if (line == null)
				return false;
			line2nd = line;
			_fileCursor += line1st.Length + line2nd.Length + 2;
			double percent = ((double)_fileCursor / _fileSize) * 100.0;
			if (percent - _lastPercent > 1.123)
			{
				_lastPercent = percent;
				ProgressChanged?.Invoke(this, _lastPercent);
			}
			return true;
		}

		private async Task<bool> readHeader(StreamReader _stream)
		{
			var head = await _stream.ReadLineAsync().ConfigureAwait(false);
			if (head == null)
				return false;
			_fileCursor += head.Length + 1;
			return head.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase);
		}

		int lineStop = 0;
		int lineCursor = 0;
		string name = "";
		StringBuilder _key = new (128);
		StringBuilder _value = new(128);

		void skipWhitespace()
		{
			for (; lineCursor < lineStop; lineCursor++)
			{
				char ch = line1st[lineCursor];

				if (char.IsWhiteSpace(ch) == false)
					return;
			}
		}

		bool getString(StringBuilder sb)
		{
			sb.Clear();
			char ch = line1st[lineCursor++];
			if (ch != '"')
			{
				sb.Append(ch);
				while (true)
				{
					ch = line1st[lineCursor++];
					if (ch == '"')
						break;
					sb.Append(ch);
				}
			}
			return true;
		}

		bool getToken(StringBuilder sb)
		{
			sb.Clear();
			skipWhitespace();
			if (lineCursor == lineStop)
				return false;
			char ch = line1st[lineCursor++];
			if (ch == '"')
			{
				return getString(sb);
			}
			while (true)
			{
				sb.Append(ch);
				ch = line1st[lineCursor];
				if (ch == '=')
				{
					lineCursor++;
					getToken(_value);
					break;
				}
				if (char.IsWhiteSpace(ch) || ch == '"')
				{
					break;
				}
				ch = line1st[lineCursor++];
			}
			return true;
		}

		private bool BeginLine()
		{
			bool inqu = false;
			for (lineStop = 0; lineStop < line1st.Length; lineStop++)
			{
				char c = line1st[lineStop];
				if (c == '"')
				{
					inqu = !inqu;
					continue;
				}
				if (inqu)
					continue;
				if (c == ',')
					break;
			}
			if (lineStop == -1 || line1st.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase) == false)
				return false;
			name = line1st.Substring(lineStop + 1);
			for (lineCursor = 7 /* "#EXTINF" */; lineCursor < lineStop; lineCursor++)
			{
				if (char.IsWhiteSpace(line1st[lineCursor]) == true)
				{
					break;
				}
			}
			return true;
		}

		public async Task<List<M3UObject>> LoadM3U()
		{
			List<M3UObject> objects = new(30000);
			try
			{
				using (var fileStream = _fileInfo.OpenRead())
				using (var buffered = new BufferedStream(fileStream))
				using (var reader = new StreamReader(buffered))
				{
					if (await readHeader(reader).ConfigureAwait(false) == false)
						return objects;

					M3UObject? o = null;
					while (await readElement(reader).ConfigureAwait(false))
					{
						if (BeginLine() == false)
						{
							Debug.WriteLine($"Error parsing with;{Environment.NewLine}{line1st}{Environment.NewLine}{line2nd}");
							continue;
						}
						o = new M3UObject
						{
							TvgName = name,
							UrlTvg = line2nd
						};
						while (getToken(_key))
						{
							string token = _key.ToString().ToLower();
							switch (token)
							{
								case "tvg-logo":
									o.TvgLogo = _value.ToString();
									break;
								case "group-title":
									o.GroupTitle = _value.ToString();
									break;
							}
						}
						objects.Add(o);
					}
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Parsing error : {e.Message}");
			}
			return objects;
		}
	}
}
