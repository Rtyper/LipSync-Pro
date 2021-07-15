using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace RogoDigital.Lipsync.AutoSync
{
	[AutoSyncModuleInfo("Phoneme Detection/PocketSphinx Phoneme Detection Module", "Equivalent to AutoSync 2. Uses Carnegie Mellon University's PocketSphinx library to attempt to detect phonemes from audio.", "Rogo Digital")]
	public class ASPocketSphinxPhonemeDetectionModule : AutoSyncModule
	{
		public int languageModel = 0;
		public bool useAudioConversion = true;
		public bool allphone_ciEnabled = true;
		public bool backtraceEnabled = false;
		public int beamExponent = -20;
		public int pbeamExponent = -20;
		public float lwValue = 2.5f;

		public override ClipFeatures GetCompatibilityRequirements ()
		{
			return ClipFeatures.AudioClip;
		}

		public override ClipFeatures GetOutputCompatibility ()
		{
			return ClipFeatures.Phonemes;
		}

		public override void Process (LipSyncData inputClip, AutoSync.ASProcessDelegate callback)
		{
			bool converted = false;
			string audioPath = AssetDatabase.GetAssetPath(inputClip.clip).Substring("/Assets".Length);

			if (audioPath != null)
			{
				// Get absolute path
				audioPath = Application.dataPath + "/" + audioPath;

				// Check Path
				if (audioPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || Path.GetFileNameWithoutExtension(audioPath).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				{
					callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Audio path contains invalid characters.", ClipFeatures.None));
					return;
				}

				bool failed = false;

				if (AutoSyncConversionUtility.IsConversionAvailable && useAudioConversion)
				{
					converted = true;
					string newPath = Path.ChangeExtension(audioPath, ".converted.wav");
					if (!AutoSyncConversionUtility.StartConversion(audioPath, newPath, AutoSyncConversionUtility.AudioFormat.WavPCM, 16000, 16, 1))
						failed = true;
					audioPath = newPath;
				}

				if (!File.Exists(audioPath) || failed)
				{
					if (converted)
					{
						if (File.Exists(audioPath))
						{
							File.Delete(audioPath);
							AssetDatabase.Refresh();
						}
					}

					callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Audio conversion failed or file was deleted.", ClipFeatures.None));
					return;
				}

				// Load Language Model
				ASPocketSphinxLanguageModel model = ASPocketSphinxLanguageModel.Load(languageModel);
				if (model == null)
				{
					if (converted)
					{
						if (File.Exists(audioPath))
						{
							File.Delete(audioPath);
							AssetDatabase.Refresh();
						}
					}
					callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Language Model failed to load.", ClipFeatures.None));
					return;
				}
				string basePath = model.GetBasePath();

				List<string> args = new List<string>();
				args.Add("-infile");
				args.Add(audioPath);
				args.Add("-hmm");
				args.Add(basePath + model.hmmDir);
				args.Add("-allphone");
				args.Add(basePath + model.allphoneFile);
				if (allphone_ciEnabled)
				{ args.Add("-allphone_ci"); args.Add("yes"); }
				if (backtraceEnabled)
				{ args.Add("-backtrace"); args.Add("yes"); }
				args.Add("-time");
				args.Add("yes");
				args.Add("-beam");
				args.Add("1e" + beamExponent);
				args.Add("-pbeam");
				args.Add("1e" + pbeamExponent);
				args.Add("-lw");
				args.Add(lwValue.ToString());

				SphinxWrapper.Recognize(args.ToArray());

				ContinuationManager.Add(() => SphinxWrapper.isFinished, () =>
				{
					if (SphinxWrapper.error != null)
					{
						callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, SphinxWrapper.error, ClipFeatures.None));
						return;
					}

					List<PhonemeMarker> data = ParseOutput(
							SphinxWrapper.result.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
							model,
							inputClip.clip
						);

					inputClip.phonemeData = data.ToArray();
					callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(true, "", GetOutputCompatibility()));

					if (converted)
					{
						if (File.Exists(audioPath))
						{
							File.Delete(audioPath);
							AssetDatabase.Refresh();
						}
					}
				});
			}
		}

		private List<PhonemeMarker> ParseOutput (string[] lines, ASPocketSphinxLanguageModel lm, AudioClip clip)
		{
			List<PhonemeMarker> results = new List<PhonemeMarker>();

			Dictionary<string, string> phonemeMapper = null;

			var settings = LipSyncEditorExtensions.GetProjectFile();
			bool needsMapping = true;

			if (lm.sourcePhoneticAlphabetName == settings.phonemeSet.scriptingName)
			{
				needsMapping = false;
			}
			else
			{
				needsMapping = true;
				switch (lm.mappingMode)
				{
					case AutoSyncPhonemeMap.MappingMode.InternalMap:
						phonemeMapper = lm.phonemeMap.GenerateAtoBDictionary();
						break;
					case AutoSyncPhonemeMap.MappingMode.ExternalMap:
						if (lm.externalMap)
						{
							phonemeMapper = lm.externalMap.phonemeMap.GenerateAtoBDictionary();
						}
						else
						{
							Debug.LogError("Language Model specifies an external phoneme map, but no phoneme map was provided.");
							return null;
						}
						break;
					default:
					case AutoSyncPhonemeMap.MappingMode.AutoDetect:
						phonemeMapper = AutoSyncUtility.FindBestFitPhonemeMap(lm.sourcePhoneticAlphabetName, settings.phonemeSet.scriptingName);
						if (phonemeMapper == null)
						{
							Debug.LogErrorFormat("No PhonemeMap could be found to map from '{0}' to the current PhonemeSet '{1}'.", lm.sourcePhoneticAlphabetName, settings.phonemeSet.scriptingName);
							return null;
						}
						break;
				}

				if (phonemeMapper.Count == 0)
				{
					Debug.LogWarning("PhonemeMap is empty - this may be due to the language model's mapping mode being set to 'InternalMap' but with no entries being added to the map. Phonemes may not be generated.");
				}
			}

			NumberStyles style = NumberStyles.Number;
			CultureInfo culture = CultureInfo.InvariantCulture;

			foreach (string line in lines)
			{
				if (string.IsNullOrEmpty(line))
					break;
				string[] tokens = line.Split(' ');

				try
				{
					if (tokens[0] != "SIL")
					{
						string phonemeName = needsMapping ? phonemeMapper[tokens[0]] : tokens[0];
						float startTime = -1;
						float.TryParse(tokens[1], style, culture, out startTime);

						if(startTime > -1)
						{
							startTime /= clip.length;
						}
						else
						{
							Debug.LogWarning("Phoneme mapper returned invalid timestamp. Skipping this entry.");
							continue;
						}

						bool found = false;
						int phoneme;
						for (phoneme = 0; phoneme < settings.phonemeSet.phonemeList.Count; phoneme++)
						{
							if (settings.phonemeSet.phonemeList[phoneme].name == phonemeName)
							{
								found = true;
								break;
							}
						}

						if (found)
						{
							results.Add(new PhonemeMarker(phoneme, startTime));
						}
						else
						{
							Debug.LogWarning("Phoneme mapper returned '" + phonemeName + "' but this phoneme does not exist in the current set. Skipping this entry.");
						}
					}
				}
				catch (ArgumentOutOfRangeException)
				{
					Debug.LogWarning("Phoneme Label missing from return data. Skipping this entry.");
				}
				catch (KeyNotFoundException)
				{
					Debug.LogWarning("Phoneme Label '" + tokens[0] + "' not found in phoneme mapper. Skipping this entry.");
				}
			}

			return results;
		}
	}
}