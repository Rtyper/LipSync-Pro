using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace RogoDigital.Lipsync
{
	[System.Obsolete("Replaced by ASPocketSphinxLanguageModel. Run the converter from Window/Rogo Digital/LipSync Pro/Convert Old Language Models.")]
	public class AutoSyncLanguageModel : ScriptableObject
	{

		[SerializeField]
		public string language;
		[SerializeField]
		public string recommendedPhonemeSet;

		[SerializeField]
		public string hmmDir;
		[SerializeField]
		public string dictFile;
		[SerializeField]
		public string allphoneFile;
		[SerializeField]
		public string lmFile;
		[SerializeField]
		public PhonemeMapping[] phonemeMapper = new PhonemeMapping[0];

		public string GetBasePath ()
		{
			string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this).Substring("/Assets".Length));
			return Application.dataPath + "/" + path + "/";
		}

		public static AutoSyncLanguageModel Load (string languageName)
		{
			string[] assets = AssetDatabase.FindAssets("t:AutoSyncLanguageModel");

			var settings = LipSyncEditorExtensions.GetProjectFile();

			if (settings == null)
				return null;
			if (settings.phonemeSet == null)
				return null;

			if (assets.Length > 0)
			{
				foreach (string guid in assets)
				{
					AutoSyncLanguageModel model = AssetDatabase.LoadAssetAtPath<AutoSyncLanguageModel>(AssetDatabase.GUIDToAssetPath(guid));
					if (model.language == languageName)
					{
						if (model.recommendedPhonemeSet != settings.phonemeSet.scriptingName && !string.IsNullOrEmpty(model.recommendedPhonemeSet))
						{
							if (!EditorUtility.DisplayDialog("Wrong Phoneme Set", "Warning: You are using the '" + settings.phonemeSet.scriptingName + "' Phoneme Set, and this language model is designed for use with '" + model.recommendedPhonemeSet + "'. This may not provide usable results, are you sure you want to continue?", "Yes", "No"))
							{
								return null;
							}
						}
						return model;
					}
				}
			}

			return null;
		}

		public static string[] FindModels ()
		{
			return FindModels("");
		}

		public static string[] FindModels (string filter)
		{
			string[] assets = AssetDatabase.FindAssets("t:AutoSyncLanguageModel " + filter);

			for (int s = 0; s < assets.Length; s++)
			{
				AutoSyncLanguageModel model = AssetDatabase.LoadAssetAtPath<AutoSyncLanguageModel>(AssetDatabase.GUIDToAssetPath(assets[s]));
				assets[s] = model.language;
			}

			return assets;
		}

		[System.Serializable]
		public struct PhonemeMapping
		{
			public string label;
			[System.Obsolete("Use AutoSyncLanguageModel.PhonemeMapping.phonemeName instead.")]
			public Phoneme phoneme;
			public string phonemeName;

			public PhonemeMapping (string label, string phonemeName)
			{
				this.label = label;
				this.phonemeName = phonemeName;
				this.phoneme = Phoneme.AI;
			}

			[System.Obsolete("Use string constructor instead.")]
			public PhonemeMapping (string label, Phoneme phoneme)
			{
				this.label = label;
				this.phoneme = phoneme;
				this.phonemeName = phoneme.ToString();
			}
		}
	}
}