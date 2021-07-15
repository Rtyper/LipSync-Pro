using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace RogoDigital.Lipsync.AutoSync
{
	[CreateAssetMenu(fileName = "PocketSphinx Language Model", menuName = "LipSync Pro/AutoSync/PocketSphinx Language Model")]
	public class ASPocketSphinxLanguageModel : ScriptableObject
	{
		public string language;
		public string sourcePhoneticAlphabetName;

		public string hmmDir;
		public string dictFile;
		public string allphoneFile;
		public string lmFile;

		public AutoSyncPhonemeMap.MappingMode mappingMode;
		public AutoSyncPhonemeMap phonemeMap = new AutoSyncPhonemeMap();
		public string recommendedPhonemeSet;
		public AutoSyncExternalPhonemeMap externalMap;

		public string GetBasePath ()
		{
			string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this).Substring("/Assets".Length));
			return Application.dataPath + "/" + path + "/";
		}

		public static ASPocketSphinxLanguageModel Load (int index)
		{
			string[] languageModelGUIDs = AssetDatabase.FindAssets("t:ASPocketSphinxLanguageModel");

			var settings = LipSyncEditorExtensions.GetProjectFile();
			if (settings == null)
				return null;
			if (settings.phonemeSet == null)
				return null;

			if (languageModelGUIDs.Length > 0 && languageModelGUIDs.Length > index)
			{
				ASPocketSphinxLanguageModel model = AssetDatabase.LoadAssetAtPath<ASPocketSphinxLanguageModel>(AssetDatabase.GUIDToAssetPath(languageModelGUIDs[index]));
				if(model != null)
				{
					if (model.mappingMode == AutoSyncPhonemeMap.MappingMode.InternalMap && !string.IsNullOrEmpty(model.recommendedPhonemeSet) && model.recommendedPhonemeSet != settings.phonemeSet.scriptingName)
					{
						if (!EditorUtility.DisplayDialog("Wrong Phoneme Set", "Warning: You are using the '" + settings.phonemeSet.scriptingName + "' Phoneme Set, and this language model is designed for use with '" + model.recommendedPhonemeSet + "'. This may not provide usable results, are you sure you want to continue?", "Yes", "No"))
						{
							return null;
						}
					}
					return model;
				}
			}
			else
			{
				Debug.LogError("LipSync: Invalid PocketSphinx language model index provided.");
			}

			return null;
		}

		public static string[] FindModels ()
		{
			return FindModels("");
		}

		public static string[] FindModels (string filter)
		{
			string[] assets = AssetDatabase.FindAssets("t:ASPocketSphinxLanguageModel " + filter);

			for (int s = 0; s < assets.Length; s++)
			{
				ASPocketSphinxLanguageModel model = AssetDatabase.LoadAssetAtPath<ASPocketSphinxLanguageModel>(AssetDatabase.GUIDToAssetPath(assets[s]));
				assets[s] = model.language;
			}

			return assets;
		}
	}
}