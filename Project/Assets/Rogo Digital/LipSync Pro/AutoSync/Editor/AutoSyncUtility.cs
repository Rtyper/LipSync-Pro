using System.IO;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace RogoDigital.Lipsync.AutoSync
{
	public static class AutoSyncUtility
	{
		public static AutoSyncPreset[] GetPresets ()
		{
			string[] presetGUIDs = AssetDatabase.FindAssets("t:AutoSyncPreset");
			AutoSyncPreset[] presets = new AutoSyncPreset[presetGUIDs.Length];
			for (int i = 0; i < presets.Length; i++)
			{
				presets[i] = AssetDatabase.LoadAssetAtPath<AutoSyncPreset>(AssetDatabase.GUIDToAssetPath(presetGUIDs[i]));
			}

			return presets;
		}

		/// <summary>
		/// Return any features required by
		/// </summary>
		/// <param name="data"></param>
		/// <param name="module"></param>
		/// <returns></returns>
		public static ClipFeatures GetMissingClipFeatures (LipSyncData data, AutoSyncModule module)
		{
			var req = module.GetCompatibilityRequirements();
			ClipFeatures metCriteria = 0;

			if (data.clip)
				metCriteria |= ClipFeatures.AudioClip;

			if (!string.IsNullOrEmpty(data.transcript))
				metCriteria |= ClipFeatures.Transcript;

			if (data.phonemeData != null && data.phonemeData.Length > 0)
				metCriteria |= ClipFeatures.Phonemes;

			if (data.emotionData != null && data.emotionData.Length > 0)
				metCriteria |= ClipFeatures.Emotions;

			if (data.gestureData != null && data.gestureData.Length > 0)
				metCriteria |= ClipFeatures.Gestures;


			// Compare masks
			return ((req & metCriteria) ^ req);
		}

		/// <summary>
		/// Check if the supplied LipSyncData clip is compatible with a particular module.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="module"></param>
		/// <returns></returns>
		public static bool CheckIsClipCompatible (LipSyncData data, AutoSyncModule module)
		{
			return GetMissingClipFeatures(data, module) == ClipFeatures.None;
		}

		/// <summary>
		/// Finds and returns the AutoSyncModuleInfoAttribute on the supplied type.
		/// moduleType must be a direct or indirect subclass of AutoSyncModule.
		/// </summary>
		/// <param name="moduleType"></param>
		/// <returns></returns>
		public static AutoSyncModuleInfoAttribute GetModuleInfo (Type moduleType)
		{
			if (moduleType.IsSubclassOf(typeof(AutoSyncModule)))
			{
				var attributes = moduleType.GetCustomAttributes(typeof(AutoSyncModuleInfoAttribute), true);
				if (attributes == null || attributes.Length == 0)
				{
					Debug.LogWarning("Call to GetModuleInfo on an AutoSyncModule type that has no AutoSyncModuleInfoAttribute anywhere in its inheritance hierarchy. This is not serious, but implies the base class has had its default info removed.");
					return null;
				}
				return (AutoSyncModuleInfoAttribute)attributes[0];
			}
			else
			{
				throw new ArgumentException("Supplied moduleType is not a subclass of AutoSyncModule.");
			}
		}

		/// <summary>
		/// Returns a List<Type> of all AutoSync modules that exist in the current project.
		/// This method uses reflection, and doesn't cache any results, so call it sparingly.
		/// </summary>
		/// <returns></returns>
		public static List<Type> GetModuleTypes ()
		{
			var modules = new List<Type>();

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int a = 0; a < assemblies.Length; a++)
			{
				Type[] types = assemblies[a].GetTypes();
				for (int t = 0; t < types.Length; t++)
				{
					if (types[t].IsSubclassOf(typeof(AutoSyncModule)) && !types[t].IsAbstract)
					{
						modules.Add(types[t]);
					}
				}
			}

			return modules;
		}

		/// <summary>
		/// Used to confirm a command line application at a certain path responds as expected.
		/// Can be used to  check that the program is the correct one, or if it is set-up correctly.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="responseContains"></param>
		/// <returns></returns>
		public static bool VerifyProgramAtPath (string path, string responseContains)
		{
			// Get path from settings.
			bool gotOutput = false;

			if (string.IsNullOrEmpty(path))
				return false;

			Directory.SetCurrentDirectory(Application.dataPath.Remove(Application.dataPath.Length - 6));
			path = Path.GetFullPath(path);

			// Attempt to start application.
			System.Diagnostics.Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = path;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;


			// Only verify if process outputs a string containing the correct response.
			process.OutputDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) =>
			{
				if (!string.IsNullOrEmpty(outLine.Data))
				{
					if (outLine.Data.Contains(responseContains))
						gotOutput = true;
				}
			};

			process.ErrorDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) =>
			{
				if (!string.IsNullOrEmpty(outLine.Data))
				{
					if (outLine.Data.Contains(responseContains))
						gotOutput = true;
				}
			};

			// Fail if anything goes wrong.
			try
			{
				process.Start();
				process.BeginErrorReadLine();
				process.BeginOutputReadLine();
			}
			catch
			{
				return false;
			}

			process.WaitForExit();

			return gotOutput;
		}

		/// <summary>
		/// Attempt to load a transcript from an identically-named text file next to the AudioClip, if one exists.
		/// </summary>
		/// <param name="clip"></param>
		/// <returns></returns>
		public static string TryGetTranscript (AudioClip clip)
		{
			string path = AssetDatabase.GetAssetPath(clip);
			TextAsset text = AssetDatabase.LoadAssetAtPath<TextAsset>(Path.ChangeExtension(path, "txt"));
			if (text)
			{
				return text.text;
			}

			return null;
		}

		/// <summary>
		/// Checks available external Phoneme Maps to find the best match for the given phoneme sets.
		/// </summary>
		/// <param name="sourceSet"></param>
		/// <param name="destinationSet"></param>
		/// <returns></returns>
		public static Dictionary<string, string> FindBestFitPhonemeMap (string sourceSet, string destinationSet)
		{
			string[] mapGUIs = AssetDatabase.FindAssets("t:AutoSyncExternalPhonemeMap");
			for (int i = 0; i < mapGUIs.Length; i++)
			{
				var map = AssetDatabase.LoadAssetAtPath<AutoSyncExternalPhonemeMap>(AssetDatabase.GUIDToAssetPath(mapGUIs[i]));
				if (map.setAName == sourceSet && map.setBName == destinationSet)
				{
					return map.phonemeMap.GenerateAtoBDictionary();
				}
				else if (map.setBName == sourceSet && map.setAName == destinationSet)
				{
					return map.phonemeMap.GenerateBtoADictionary();
				}
			}
			return null;
		}
	}
}