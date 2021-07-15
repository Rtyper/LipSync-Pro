using RogoDigital;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Globalization;

public class RDExtensionWindow : EditorWindow
{
	public bool downloading = false;
	public int connectionsInProgress = 0;
	public string currentExtension = "";

	private int sortingMode;
	private int sortDirection;
	private int currentFilter = -1;
	private string currentCategory;

	private string baseAddress = "http://updates.rogodigital.com/AssetStore/extensions/";
	private string listFilename = "list-new.xml";

	private List<string> headerLinks = new List<string>();
	private List<string> categories = new List<string>();

	private List<ItemListing> itemsAlpha = new List<ItemListing>();
	private List<ItemListing> itemsCat = new List<ItemListing>();
	private List<ItemListing> itemsUpdate = new List<ItemListing>();
	private List<ItemListing>[] lists;

	private bool gotListing = false;
	private bool connectionFailed = false;

	private Vector2 headerScroll;
	private Vector2 bodyScroll;

#if UNITY_2018_3_OR_NEWER
	private UnityWebRequest downloadConnection;
#else
	private WWW downloadConnection;
#endif

	private GUIStyle headerLink;
	private GUIStyle headerLinkActive;

	private GUIStyle productTitle;
	private GUIStyle productDescription;

	private GUIStyle headerText;
	private GUIStyle headerTextActive;

	//Images
	Texture2D headerLogo;
	Texture2D headerBG;
	Texture2D headerButtonActive;
	Texture2D defaultIcon;
	Texture2D upArrow;
	Texture2D downArrow;

	Dictionary<string, Texture2D> productIcons = new Dictionary<string, Texture2D>();

	void OnEnable ()
	{
		sortingMode = EditorPrefs.GetInt("RogoDigital_ExtensionsSortingMode", 0);
		sortDirection = EditorPrefs.GetInt("RogoDigital_ExtensionsSortDirection", 0);

		headerLogo = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_header_left.png");
		headerBG = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_header_bg.png");
		headerButtonActive = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_header_button.png");
		defaultIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/default_icon.png");

		if (EditorGUIUtility.isProSkin)
		{
			upArrow = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/Dark/up.png");
			downArrow = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/Dark/down.png");
		}
		else
		{
			upArrow = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/Light/up.png");
			downArrow = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/Light/down.png");
		}

		ConnectToServer();
	}

	void ConnectToServer ()
	{
#if UNITY_2018_3_OR_NEWER
		UnityWebRequest listConnection = UnityWebRequest.Get(baseAddress + listFilename);
		listConnection.SendWebRequest();
#else
		WWW listConnection = new WWW(baseAddress + listFilename);
#endif
		connectionsInProgress++;

		ContinuationManager.Add(() => listConnection.isDone, () =>
		{
			connectionsInProgress--;
			if (connectionsInProgress == 0)
			{
				RemoveNotification();
			}

			if (string.IsNullOrEmpty(listConnection.error))
			{
#if UNITY_2018_3_OR_NEWER
				XmlReader reader = XmlReader.Create(new StringReader(listConnection.downloadHandler.text));
#else
				XmlReader reader = XmlReader.Create(new StringReader(listConnection.text));
#endif
				headerLinks = new List<string>();

				itemsAlpha = new List<ItemListing>();
				itemsCat = new List<ItemListing>();
				itemsUpdate = new List<ItemListing>();
				NumberStyles style = NumberStyles.Number;
				CultureInfo culture = CultureInfo.InvariantCulture;

				try
				{
					while (reader.Read())
					{
						if (reader.Name == "product")
						{
							if (reader.HasAttributes)
							{
								string name = reader.GetAttribute("name").Replace(" ", "_");
								headerLinks.Add(name);

								//Get Icon
#if UNITY_2018_3_OR_NEWER
								UnityWebRequest iconConnection = UnityWebRequestTexture.GetTexture(baseAddress + "icons/" + name + ".png");
								iconConnection.SendWebRequest();
#else
								WWW iconConnection = new WWW(baseAddress + "icons/" + name + ".png");
#endif
								connectionsInProgress++;
								ContinuationManager.Add(() => iconConnection.isDone, () =>
								{
									connectionsInProgress--;
									if (connectionsInProgress == 0)
									{
										RemoveNotification();
									}

									if (string.IsNullOrEmpty(iconConnection.error))
									{
#if UNITY_2018_3_OR_NEWER
										Texture2D icon = ((DownloadHandlerTexture)iconConnection.downloadHandler).texture;
#else
										Texture2D icon = iconConnection.texture;
#endif
										icon.hideFlags = HideFlags.DontSave;

										if (icon != null)
										{
											productIcons.Add(name, icon);
											Repaint();
										}

									}
									else
									{
										Debug.Log("Attempt to download icon reported error: " + iconConnection.error + " at " + iconConnection.url);
									}
								});
							}
						}
						else if (reader.Name == "item")
						{
							if (reader.HasAttributes)
							{
								//Get Icon
#if UNITY_2018_3_OR_NEWER
								UnityWebRequest iconDownload = UnityWebRequestTexture.GetTexture(baseAddress + reader.GetAttribute("icon"));
								iconDownload.SendWebRequest();
#else
								WWW iconDownload = new WWW(baseAddress + reader.GetAttribute("icon"));
#endif
								connectionsInProgress++;
								string itemName = reader.GetAttribute("name");
								string itemCategory = reader.GetAttribute("category");
								string[] itemProducts = reader.GetAttribute("products").Replace(" ", "_").Split(',');
								float versionNumber = 0;
								float.TryParse(reader.GetAttribute("version"), style, culture, out versionNumber);
								DateTime lastUpdated = DateTime.Parse(reader.GetAttribute("lastUpdated"), culture);
								float itemMinVersion = 0;
								float.TryParse(reader.GetAttribute("minVersion"), style, culture, out itemMinVersion);
								string itemURL = reader.GetAttribute("url");
								string itemDescription = reader.GetAttribute("description");


								if (!categories.Contains(itemCategory))
								{
									categories.Add(itemCategory);
								}

								ContinuationManager.Add(() => iconDownload.isDone || iconDownload.error != null, () =>
								{
									connectionsInProgress--;
									if (connectionsInProgress == 0)
									{
										RemoveNotification();
									}

									Texture2D icon = null;
									if (string.IsNullOrEmpty(iconDownload.error))
									{
#if UNITY_2018_3_OR_NEWER
										icon = ((DownloadHandlerTexture)iconDownload.downloadHandler).texture;
#else
										icon = iconDownload.texture;
#endif
										icon.hideFlags = HideFlags.DontSave;
									}
									else
									{
										Debug.LogWarning("Icon for " + itemName + " failed with error: " + iconDownload.error);
									}

									ItemListing item = new ItemListing(
											itemName,
											itemCategory,
											itemProducts,
											itemMinVersion,
											lastUpdated,
											versionNumber,
											itemURL,
											itemDescription,
											icon == null ? defaultIcon : icon
										);

									itemsAlpha.Add(item);
									itemsAlpha.Sort(SortItemsAlphaNumeric);
									itemsCat.Add(item);
									itemsCat.Sort(SortItemsCategory);
									itemsUpdate.Add(item);
									itemsUpdate.Sort(SortItemsLastUpdated);
									Repaint();
								});
							}
						}
					}

					lists = new List<ItemListing>[] { itemsAlpha, itemsCat, itemsUpdate };
					gotListing = true;

				}
				catch (Exception exception)
				{
					Debug.Log("Error loading extension list. Error: " + exception.StackTrace);
					connectionFailed = true;
				}
			}
			else
			{
				Debug.Log("Could not connect to extension server. Error: " + listConnection.error);
				connectionFailed = true;
			}

			Repaint();
		});
	}

	// Editor GUI
	public static void ShowWindowGeneric (object startProduct)
	{
		ShowWindow((string)startProduct);
	}

	[MenuItem("Window/Rogo Digital/Get Extensions", false, 0)]
	public static void ShowWindow ()
	{
		ShowWindow("");
	}

	public static void ShowWindow (string startProduct)
	{
		RDExtensionWindow window;

		window = GetWindow<RDExtensionWindow>();
		Texture2D icon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_Icon.png");

		window.titleContent = new GUIContent("Extensions", icon);

		ContinuationManager.Add(() => window.gotListing, () =>
		{
			if (window.headerLinks.Contains(startProduct))
			{
				window.currentFilter = window.headerLinks.IndexOf(startProduct);
			}
		});
	}

	public static void RequestInstall (string name)
	{
		var window = GetWindow<RDExtensionWindow>();
		Texture2D icon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/RogoDigital_Icon.png");

		window.titleContent = new GUIContent("Extensions", icon);

		ContinuationManager.Add(() => window.connectionsInProgress == 0, () =>
		{
			for (int i = 0; i < window.itemsAlpha.Count; i++)
			{
				if (window.itemsAlpha[i].name == name)
				{
					window.DownloadItem(window.itemsAlpha[i], true);

				}
			}
		});
	}

	void OnGUI ()
	{
		//Initialize GUIStyles if needed
		if (headerLink == null)
		{
			headerLink = new GUIStyle();
			headerLink.alignment = TextAnchor.MiddleCenter;
			headerLink.fontStyle = FontStyle.Normal;
			headerLink.fontSize = 16;

			headerLink.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
			headerLink.margin = new RectOffset(5, 5, 25, 25);
			headerLink.padding = new RectOffset(0, 0, 6, 12);

			headerLinkActive = new GUIStyle(headerLink);
			headerLinkActive.normal.background = headerButtonActive;
			headerLinkActive.onNormal.background = headerButtonActive;
			headerLinkActive.normal.textColor = Color.white;
		}
		if (headerText == null)
		{
			headerText = new GUIStyle((GUIStyle)"ControlLabel");
			headerText.alignment = TextAnchor.MiddleCenter;
			headerText.fontStyle = FontStyle.Normal;
			headerText.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
			headerText.margin = new RectOffset(5, 5, 30, 25);

			headerTextActive = new GUIStyle(headerText);
			headerTextActive.normal.textColor = Color.white;
		}
		if (productTitle == null)
		{
			productTitle = new GUIStyle();
			productTitle.alignment = TextAnchor.MiddleLeft;
			productTitle.fontStyle = FontStyle.Normal;
			productTitle.fontSize = 16;
			productTitle.margin = new RectOffset(0, 0, 15, 0);
			if (EditorGUIUtility.isProSkin)
			{
				productTitle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
			}
			else
			{
				productTitle.normal.textColor = new Color(0.1f, 0.1f, 0.1f);
			}

			productDescription = new GUIStyle(headerText);
			productDescription.margin = new RectOffset(0, 0, 5, 0);
			productDescription.alignment = TextAnchor.MiddleLeft;
			if (EditorGUIUtility.isProSkin)
			{
				productDescription.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
			}
			else
			{
				productDescription.normal.textColor = new Color(0.3f, 0.3f, 0.3f);
			}
		}

		EditorGUI.BeginDisabledGroup(connectionsInProgress > 0);

		GUILayout.BeginHorizontal();
		GUI.DrawTexture(new Rect(0, 0, this.position.width, headerBG.height), headerBG);
		GUILayout.Box(headerLogo, GUIStyle.none);

		if (gotListing)
		{
			GUILayout.Space(-170);
			GUILayout.Box("Products", headerText);

			headerScroll = GUILayout.BeginScrollView(headerScroll, false, false, GUILayout.MaxHeight(headerBG.height + 12));
			GUILayout.Space(0);
			int linkCount = 0;
			GUILayout.BeginHorizontal();
			foreach (string product in headerLinks)
			{
				Rect buttonRect = EditorGUILayout.BeginHorizontal();
				if (productIcons.ContainsKey(product))
				{
					if (GUILayout.Button(new GUIContent(productIcons[product], product.Replace("_", " ")), (currentFilter == linkCount ? headerLinkActive : headerLink), GUILayout.MaxHeight(75), GUILayout.MaxWidth(70)))
					{
						if (currentFilter == linkCount)
						{
							currentFilter = -1;
						}
						else
						{
							currentFilter = linkCount;
						}
					}
				}
				else
				{
					if (GUILayout.Button(new GUIContent(product), (currentFilter == linkCount ? headerLinkActive : headerLink), GUILayout.MaxHeight(50)))
					{
						if (currentFilter == linkCount)
						{
							currentFilter = -1;
						}
						else
						{
							currentFilter = linkCount;
						}
					}
				}
				EditorGUILayout.EndHorizontal();
				EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
				linkCount++;
				GUILayout.Space(10);
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndScrollView();
		}
		else
		{
			GUILayout.FlexibleSpace();
			if (connectionFailed)
			{
				GUILayout.Box("Connection Failed", headerText);
			}
			else
			{
				GUILayout.Box("Connecting...", headerText);
			}
			GUILayout.Space(30);
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(-86);
		GUILayout.BeginHorizontal();
		GUILayout.Space(220);
		GUILayout.Box("Categories", headerText);
		GUILayout.Space(30);
		foreach (string category in categories)
		{
			Rect buttonRect = EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent(category), category != currentCategory ? headerText : headerTextActive))
			{
				if (currentCategory == category)
				{
					currentCategory = null;
				}
				else
				{
					currentCategory = category;
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(-12);
		EditorGUILayout.BeginHorizontal();
		GUILayout.Space(20);
		GUILayout.Label("Sort by:");
		int oldSortingMode = sortingMode;
		sortingMode = GUILayout.Toolbar(sortingMode, new string[] { "Name", "Category", "Last Updated" }, EditorStyles.miniButton);
		if (oldSortingMode != sortingMode)
		{
			EditorPrefs.SetInt("RogoDigital_ExtensionsSortingMode", sortingMode);
		}
		GUILayout.Space(5);
		if (GUILayout.Button(sortDirection == 0 ? upArrow : downArrow, EditorStyles.miniButton))
		{
			sortDirection = 1 - sortDirection;
			EditorPrefs.SetInt("RogoDigital_ExtensionsSortDirection", sortDirection);
			itemsAlpha.Sort(SortItemsAlphaNumeric);
			itemsCat.Sort(SortItemsCategory);
			itemsUpdate.Sort(SortItemsLastUpdated);
		}
		GUILayout.FlexibleSpace();
		GUILayout.Space(20);
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(10);

		if (connectionFailed)
		{
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box("Could not connect to server.", headerText);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Retry"))
			{
				connectionFailed = false;
				ConnectToServer();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
		}
		else if (!gotListing)
		{
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box("Connecting", headerLink);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
		}
		else
		{
			EditorGUI.BeginDisabledGroup(downloading);
			bodyScroll = GUILayout.BeginScrollView(bodyScroll, false, false);
			if (lists != null && itemsAlpha != null && itemsCat != null && itemsUpdate != null)
			{
				foreach (ItemListing listing in lists[sortingMode])
				{
					bool show = false;

					if (currentFilter >= 0)
					{
						foreach (string product in listing.products)
						{
							if (headerLinks[currentFilter] == product)
							{
								show = true;
							}
						}
					}
					else
					{
						show = true;
					}

					if (!string.IsNullOrEmpty(currentCategory))
					{
						if (listing.category != currentCategory)
						{
							show = false;
						}
					}

					if (show)
					{
						GUILayout.BeginHorizontal();
						GUILayout.Space(20);
						GUILayout.Box(listing.icon, GUIStyle.none, GUILayout.Width(90), GUILayout.Height(90));

						GUILayout.Space(30);
						GUILayout.BeginVertical();
						//Hack: Prevents random vertical offset.
						GUILayout.Space(0);
						GUILayout.Box(new GUIContent(listing.name + " | v" + listing.versionNumber), productTitle, GUILayout.Height(12));
						GUILayout.BeginHorizontal();
						foreach (string product in listing.products)
						{
							if (productIcons.ContainsKey(product))
							{
								GUILayout.Box(new GUIContent(productIcons[product], product.Replace("_", " ")), productTitle, GUILayout.Height(32), GUILayout.Width(38));
							}
						} 
						GUILayout.EndHorizontal();
						GUILayout.Box(listing.description, productDescription);
						GUILayout.Box("Version " + listing.versionNumber + ". Updated: " + listing.lastUpdatedString + ". Works with LipSync version " + listing.minVersion.ToString() + " or higher.", productDescription);
						GUILayout.EndVertical();
						GUILayout.FlexibleSpace();
						GUILayout.BeginVertical();
						GUILayout.Space(30);
						if (GUILayout.Button("Download", GUILayout.Height(30), GUILayout.MaxWidth(100)))
						{
							DownloadItem(listing);
						}
						GUILayout.EndVertical();
						GUILayout.Space(20);
						GUILayout.EndHorizontal();
						GUILayout.Space(20);
					}
				}
			}

			GUILayout.Space(40);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box("More extensions coming soon. To request support for another asset, post in the forum thread, or send us an email.", productTitle);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Forum Thread"))
			{
				Application.OpenURL("http://forum.unity3d.com/threads/alpha-lipsync-a-phoneme-based-lipsyncing-system-for-unity.309324/");
			}
			if (GUILayout.Button("Email Support"))
			{
				Application.OpenURL("mailto:contact@rogodigital.com");
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
			GUILayout.EndScrollView();
			EditorGUI.EndDisabledGroup();
			EditorGUI.EndDisabledGroup();

			if (downloading)
			{
#if UNITY_2018_3_OR_NEWER
				float progress = downloadConnection.downloadProgress;
#else
				float progress = downloadConnection.progress;
#endif
				EditorGUI.ProgressBar(new Rect(10, position.height - 30, position.width - 110, 20), progress, "Downloading " + currentExtension + " - " + Mathf.Round(progress * 100).ToString() + "%");
				if (GUI.Button(new Rect(position.width - 90, position.height - 30, 80, 20), "Cancel"))
				{
					downloading = false;
				}
			}

			if (connectionsInProgress > 0)
			{
				ShowNotification(new GUIContent("Please Wait - Connecting."));
			}

		}

	}

	private void DownloadItem (ItemListing listing, bool silentInstall = false)
	{
		downloading = true;
#if UNITY_2018_3_OR_NEWER
						downloadConnection = UnityWebRequest.Get(baseAddress + listing.url);
						downloadConnection.SendWebRequest();
#else
		downloadConnection = new WWW(baseAddress + listing.url);
#endif
		currentExtension = listing.name;

		ContinuationManager.Add(() => downloadConnection.isDone, () =>
		{
			if (downloading)
			{
				downloading = false;
				if (!string.IsNullOrEmpty(downloadConnection.error))
				{
					Debug.LogError(downloadConnection.error);
					ShowNotification(new GUIContent(currentExtension + " - Download Failed"));
				}
				else if (downloadConnection.isDone)
				{
#if UNITY_2018_3_OR_NEWER
									File.WriteAllBytes(Application.dataPath + "/" + currentExtension + ".unitypackage", downloadConnection.downloadHandler.data);
#else
					File.WriteAllBytes(Application.dataPath + "/" + currentExtension + ".unitypackage", downloadConnection.bytes);
#endif
					if (!silentInstall)
						ShowNotification(new GUIContent(currentExtension + " Downloaded"));
					AssetDatabase.ImportPackage(Application.dataPath + "/" + currentExtension + ".unitypackage", !silentInstall);
					File.Delete(Application.dataPath + "/" + currentExtension + ".unitypackage");
					if (silentInstall)
					{
						Close();
					}
				}
				else
				{
					ShowNotification(new GUIContent(currentExtension + " Download Cancelled"));
				}
			}
		});
	}

	int SortItemsAlphaNumeric (ItemListing a, ItemListing b)
	{
		if (sortDirection == 0)
		{
			return AlphaSort(a.name, b.name);
		}
		else
		{
			return AlphaSort(b.name, a.name);
		}
	}

	int SortItemsCategory (ItemListing a, ItemListing b)
	{
		if (sortDirection == 0)
		{
			return AlphaSort(a.category, b.category);
		}
		else
		{
			return AlphaSort(b.category, a.category);
		}
	}

	int SortItemsLastUpdated (ItemListing a, ItemListing b)
	{
		if (sortDirection == 1)
		{
			return a.lastUpdatedRaw.CompareTo(b.lastUpdatedRaw);
		}
		else
		{
			return b.lastUpdatedRaw.CompareTo(a.lastUpdatedRaw);
		}
	}

	static int AlphaSort (string s1, string s2)
	{
		int len1 = s1.Length;
		int len2 = s2.Length;
		int marker1 = 0;
		int marker2 = 0;

		// Walk through two the strings with two markers.
		while (marker1 < len1 && marker2 < len2)
		{
			char ch1 = s1[marker1];
			char ch2 = s2[marker2];

			// Some buffers we can build up characters in for each chunk.
			char[] space1 = new char[len1];
			int loc1 = 0;
			char[] space2 = new char[len2];
			int loc2 = 0;

			// Walk through all following characters that are digits or
			// characters in BOTH strings starting at the appropriate marker.
			// Collect char arrays.
			do
			{
				space1[loc1++] = ch1;
				marker1++;

				if (marker1 < len1)
				{
					ch1 = s1[marker1];
				}
				else
				{
					break;
				}
			} while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

			do
			{
				space2[loc2++] = ch2;
				marker2++;

				if (marker2 < len2)
				{
					ch2 = s2[marker2];
				}
				else
				{
					break;
				}
			} while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

			// If we have collected numbers, compare them numerically.
			// Otherwise, if we have strings, compare them alphabetically.
			string str1 = new string(space1);
			string str2 = new string(space2);

			int result;

			if (char.IsDigit(space1[0]) && char.IsDigit(space2[0]))
			{
				int thisNumericChunk = int.Parse(str1);
				int thatNumericChunk = int.Parse(str2);
				result = thisNumericChunk.CompareTo(thatNumericChunk);
			}
			else
			{
				result = str1.CompareTo(str2);
			}

			if (result != 0)
			{
				return result;
			}
		}
		return len1 - len2;
	}

	void Update ()
	{
		if (downloadConnection != null)
		{
			if (!downloadConnection.isDone)
			{
				Repaint();
			}
		}
	}

	public class ItemListing
	{
		public string name;
		public string category;
		public string[] products;
		public float minVersion;
		public DateTime lastUpdatedRaw;
		public string lastUpdatedString;
		public float versionNumber;
		public string url;
		public string description;
		public Texture2D icon;

		public ItemListing (string name, string category, string[] products, float minVersion, DateTime lastUpdated, float versionNumber, string url, string description, Texture2D icon)
		{
			this.name = name;
			this.category = category;
			this.products = products;
			this.minVersion = minVersion;
			lastUpdatedRaw = lastUpdated;
			lastUpdatedString = lastUpdated.ToLongDateString();
			this.versionNumber = versionNumber;
			this.url = url;
			this.description = description;
			this.icon = icon;
		}
	}
}
