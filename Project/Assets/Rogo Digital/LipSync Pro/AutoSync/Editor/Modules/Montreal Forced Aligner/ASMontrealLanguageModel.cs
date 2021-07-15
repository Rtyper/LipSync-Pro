using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace RogoDigital.Lipsync.AutoSync
{
	[CreateAssetMenu(fileName = "Montreal Forced Aligner Language Model", menuName = "LipSync Pro/AutoSync/Montreal Aligner Language Model")]
	public class ASMontrealLanguageModel : ScriptableObject
	{
		public string language;
		public string sourcePhoneticAlphabetName;

		public string acousticModelPath;
		public bool usePredefinedLexicon = true;
		public string lexiconPath;
		public string g2pModelPath;

		public AutoSyncPhonemeMap.MappingMode mappingMode;
		public AutoSyncPhonemeMap phonemeMap = new AutoSyncPhonemeMap();
		public string recommendedPhonemeSet;
		public AutoSyncExternalPhonemeMap externalMap;

		public string GetBasePath ()
		{
			string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this).Substring("/Assets".Length));
			return Application.dataPath + "/" + path + "/";
		}

		public static ASMontrealLanguageModel Load (int index)
		{
			string[] languageModelGUIDs = AssetDatabase.FindAssets("t:ASMontrealLanguageModel");

			var settings = LipSyncEditorExtensions.GetProjectFile();
			if (settings == null)
				return null;
			if (settings.phonemeSet == null)
				return null;

			if (languageModelGUIDs.Length > 0 && languageModelGUIDs.Length > index)
			{
				ASMontrealLanguageModel model = AssetDatabase.LoadAssetAtPath<ASMontrealLanguageModel>(AssetDatabase.GUIDToAssetPath(languageModelGUIDs[index]));
				if (model != null)
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
				Debug.LogError("LipSync: Invalid Montreal language model index provided.");
			}

			return null;
		}

		public static string[] FindModels ()
		{
			return FindModels("");
		}

		public static string[] FindModels (string filter)
		{
			string[] assets = AssetDatabase.FindAssets("t:ASMontrealLanguageModel " + filter);

			for (int s = 0; s < assets.Length; s++)
			{
				ASMontrealLanguageModel model = AssetDatabase.LoadAssetAtPath<ASMontrealLanguageModel>(AssetDatabase.GUIDToAssetPath(assets[s]));
				assets[s] = model.language;
			}

			return assets;
		}
	}
}