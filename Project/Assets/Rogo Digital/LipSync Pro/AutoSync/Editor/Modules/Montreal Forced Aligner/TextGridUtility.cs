using System.IO;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.AutoSync
{
	/// <summary>
	/// Provides a simple way to parse data from a Prosidy Labs .TextGrid alignment file
	/// </summary>
	public static class TextGridUtility
	{
		private const int TOP_LEVEL = 0, ITEM = 1, INTERVALS = 2;

		public static TextGridItem[] ParseTextGridFile (string path)
		{
			TextGridItem[] items = null;

			int readMode = TOP_LEVEL;
			int itemIndex = 0;
			int intervalIndex = 0;

			NumberStyles style = NumberStyles.Number;
			CultureInfo culture = CultureInfo.InvariantCulture;

			StreamReader reader = new StreamReader(path);

			while (!reader.EndOfStream)
			{
				string line = reader.ReadLine();

				if (line.Contains("item"))
				{
					string sub = line.Split('[')[1].Split(']')[0];
					if (string.IsNullOrEmpty(sub))
					{
						continue;
					}
					else
					{
						if (int.TryParse(sub, out itemIndex))
						{
							itemIndex--;
							readMode = ITEM;
							items[itemIndex] = new TextGridItem();
							continue;
						}
						else
						{
							return null;
						}
					}
				}
				else if (line.Contains("intervals ["))
				{
					string sub = line.Split('[')[1].Split(']')[0];
					if (string.IsNullOrEmpty(sub))
					{
						continue;
					}
					else
					{
						if (int.TryParse(sub, out intervalIndex))
						{
							intervalIndex--;
							readMode = INTERVALS;
							items[itemIndex].intervals[intervalIndex] = new TextGridInterval();
							continue;
						}
						else
						{
							return null;
						}
					}
				}

				switch (readMode)
				{
					default:
					case TOP_LEVEL:
						if (line.Contains("size"))
						{
							int itemCount = -1;
							if (int.TryParse(line.Split('=')[1], out itemCount))
							{
								items = new TextGridItem[itemCount];
							}
							else
							{
								return null;
							}
						}
						break;
					case ITEM:
						if (line.Contains("name"))
						{
							items[itemIndex].name = line.Split('=')[1];
						}
						else if (line.Contains("xmin"))
						{
							double.TryParse(line.Split('=')[1], style, culture, out items[itemIndex].xmin);
						}
						else if (line.Contains("xmax"))
						{
							double.TryParse(line.Split('=')[1], style, culture, out items[itemIndex].xmax);
						}
						else if (line.Contains("size"))
						{
							int intervalCount = -1;
							if (int.TryParse(line.Split('=')[1], out intervalCount))
							{
								items[itemIndex].intervals = new TextGridInterval[intervalCount];
							}
							else
							{
								return null;
							}
						}
						break;
					case INTERVALS:
						if (line.Contains("text"))
						{
							items[itemIndex].intervals[intervalIndex].text = line.Split('=')[1];
						}
						else if (line.Contains("xmin"))
						{
							double.TryParse(line.Split('=')[1], style, culture, out items[itemIndex].intervals[intervalIndex].xmin);
						}
						else if (line.Contains("xmax"))
						{
							double.TryParse(line.Split('=')[1], style, culture, out items[itemIndex].intervals[intervalIndex].xmax);
						}
						break;
				}
			}
			reader.Close();

			return items;
		}

		public class TextGridInterval
		{
			public string text;
			public double xmin, xmax;
		}

		public class TextGridItem
		{
			public string name;
			public double xmin, xmax;
			public TextGridInterval[] intervals;
		}
	}
}