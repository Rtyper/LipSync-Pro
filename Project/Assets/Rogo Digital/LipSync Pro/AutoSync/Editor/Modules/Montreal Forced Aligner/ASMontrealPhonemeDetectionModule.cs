using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace RogoDigital.Lipsync.AutoSync
{
	[AutoSyncModuleInfo("Phoneme Detection/Montreal Forced Aligner (Phonemes) Module", "Gets phonemes from a transcript and audio clip using the Montreal Forced Aligner process.", "Rogo Digital", moduleSettingsType = typeof(ASMontrealPhonemeDetectionSettings))]
	public class ASMontrealPhonemeDetectionModule : AutoSyncModule
	{
		public int languageModel = 0;
		public bool useAudioConversion = true;
		public bool autoRetry = true;
		public int maxAttempts = 2;
		public double minLengthForSustain = 0.15d;
		public string configPath = "";

		private int attempts = 0;

		public override ClipFeatures GetCompatibilityRequirements()
		{
			return ClipFeatures.AudioClip | ClipFeatures.Transcript;
		}

		public override ClipFeatures GetOutputCompatibility()
		{
			return ClipFeatures.Phonemes;
		}

		private void OnEnable()
		{
			configPath = PlayerPrefs.GetString("ls_mfa_config_path", "");
			if (configPath == "")
			{
				var search = Directory.GetFiles(Application.dataPath, "default_mfa_config.yaml", SearchOption.AllDirectories);
				if (search.Length > 0)
				{
					configPath = search[0];
				}
			}
		}

		public override void ProcessWithTemplates(LipSyncData inputClip, AutoSync.ASProcessDelegate callback, PhonemeMarker phonemeTemplate, EmotionMarker emotionTemplate)
		{
			if (phonemeTemplate == null)
			{
				Process(inputClip, callback);
			}
			else
			{
				Process(inputClip, (outputClip, data) =>
				{
					// Apply template
					for (int i = 0; i < outputClip.phonemeData.Length; i++)
					{
						var marker = outputClip.phonemeData[i];

						marker.intensity = phonemeTemplate.intensity;

						marker.useRandomness = phonemeTemplate.useRandomness;
						marker.intensityRandomness = phonemeTemplate.intensityRandomness;
						marker.blendableRandomness = phonemeTemplate.blendableRandomness;
						marker.bonePositionRandomness = phonemeTemplate.bonePositionRandomness;
						marker.boneRotationRandomness = phonemeTemplate.boneRotationRandomness;
					}

					callback.Invoke(outputClip, data);
				});
			}
		}

		public override void Process(LipSyncData inputClip, AutoSync.ASProcessDelegate callback)
		{
			string mfaPath = EditorPrefs.GetString("as_montrealfa_application_path");
			string audioPath = AssetDatabase.GetAssetPath(inputClip.clip).Substring("/Assets".Length);

			if (audioPath == null)
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Audio path could not be found.", ClipFeatures.None));
				return;
			}

			if (!AutoSyncUtility.VerifyProgramAtPath(mfaPath, "align"))
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Montreal Forced Aligner application path is not verified.", ClipFeatures.None));
				return;
			}

			// Get absolute path
			audioPath = Application.dataPath + "/" + audioPath;

			// Check Path
			if (audioPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || Path.GetFileNameWithoutExtension(audioPath).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Audio path contains invalid characters.", ClipFeatures.None));
				return;
			}

			// Load Language Model
			ASMontrealLanguageModel model = ASMontrealLanguageModel.Load(languageModel);
			if (model == null)
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Language Model failed to load.", ClipFeatures.None));
				return;
			}

			string basePath = model.GetBasePath();
			string lexiconPath = "";
			if (model.usePredefinedLexicon)
			{
				lexiconPath = basePath + model.lexiconPath;
			}
			else
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Support for generated lexicons using a G2P model is coming soon.", ClipFeatures.None));
				return;
			}

			string adjustedName = Path.GetFileNameWithoutExtension(audioPath).Replace('.', '_').Replace(' ', '_').Replace('\\', '_').Replace('/', '_');

			string corpusPath = Application.temporaryCachePath + "/" + adjustedName + "_MFA_Corpus";
			string outputPath = Application.temporaryCachePath + "/" + adjustedName + "_MFA_Output";

			// Delete folders if they already exist
			try
			{
				if (Directory.Exists(corpusPath))
				{
					Directory.Delete(corpusPath, true);
				}
				if (Directory.Exists(outputPath))
				{
					Directory.Delete(outputPath, true);
				}
			}
			catch
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Attempt to clear temporary MFA folders failed. Are they open in another application?", ClipFeatures.None));
				return;
			}

			// Create temporary folders
			Directory.CreateDirectory(corpusPath);
			Directory.CreateDirectory(outputPath);

			// Copy or convert audio clip to corpus folder
			if (AutoSyncConversionUtility.IsConversionAvailable && useAudioConversion)
			{
				AutoSyncConversionUtility.StartConversion(audioPath, corpusPath + "/" + adjustedName + ".wav", AutoSyncConversionUtility.AudioFormat.WavPCM, 16000, 16, 1);
			}
			else
			{
				File.Copy(audioPath, corpusPath + "/" + adjustedName + Path.GetExtension(audioPath));
			}

			// Create transcript file in corpus folder
			StreamWriter transcriptWriter = File.CreateText(corpusPath + "/" + adjustedName + ".lab");
			transcriptWriter.Write(inputClip.transcript.Replace('-', ' '));
			transcriptWriter.Close();

			// Run aligner
			Directory.SetCurrentDirectory(Application.dataPath.Remove(Application.dataPath.Length - 6));
			mfaPath = Path.GetFullPath(mfaPath);

			System.Diagnostics.Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = mfaPath;

#if UNITY_EDITOR_WIN
			process.StartInfo.Arguments = "\"" + corpusPath + "\" \"" + lexiconPath + "\" \"" + basePath + model.acousticModelPath + "\" \"" + outputPath + "\" --quiet";
#elif UNITY_EDITOR_OSX
			process.StartInfo.Arguments = "\"" + corpusPath + "\" \"" + lexiconPath + "\" \"" + basePath + model.acousticModelPath + "\" \"" + outputPath + "\"";
#endif
			process.StartInfo.UseShellExecute = true;
			process.StartInfo.CreateNoWindow = true;

			process.Start();
			process.WaitForExit(15000);

			if (!process.HasExited)
			{
				process.Kill();
				process.Close();
			}

			var outputFiles = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
			string textGridPath = "";
			List<string> oovs = new List<string>();
			bool nothingFound = true;

			for (int i = 0; i < outputFiles.Length; i++)
			{
				if (Path.GetExtension(outputFiles[i]).ToLowerInvariant() == ".textgrid")
				{
					textGridPath = outputFiles[i];
					nothingFound = false;
				}
				else if (Path.GetExtension(outputFiles[i]).ToLowerInvariant() == ".txt")
				{
					string name = Path.GetFileNameWithoutExtension(outputFiles[i]);
					var reader = new StreamReader(outputFiles[i]);
					if (name == "oovs_found")
					{
						while (!reader.EndOfStream)
						{
							oovs.Add(reader.ReadLine());
						}
					}
					reader.Close();
				}
			}

			// Detect out-of-vocab words, filter and retry if enabled.
			if (oovs.Count > 0)
			{
				Debug.Log("Found out-of-vocabulary words:");
				for (int i = 0; i < oovs.Count; i++)
				{
					Debug.Log(oovs[i]);
				}
			}

			if (nothingFound)
			{
				if (autoRetry)
				{
					if (attempts < maxAttempts - 1)
					{
						attempts++;
						Process(inputClip, callback);
						return;
					}
				}

				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "MFA Application Failed. Check your audio encoding or enable conversion.", ClipFeatures.None));
				return;
			}

			// Load in TextGrid
			TextGridUtility.TextGridItem[] items = TextGridUtility.ParseTextGridFile(textGridPath);

			var settings = LipSyncEditorExtensions.GetProjectFile();
			bool needsMapping = true;
			Dictionary<string, string> phonemeMapper = null;

			if (model.sourcePhoneticAlphabetName == settings.phonemeSet.scriptingName)
			{
				needsMapping = false;
			}
			else
			{
				needsMapping = true;
				switch (model.mappingMode)
				{
					case AutoSyncPhonemeMap.MappingMode.InternalMap:
						phonemeMapper = model.phonemeMap.GenerateAtoBDictionary();
						break;
					case AutoSyncPhonemeMap.MappingMode.ExternalMap:
						if (model.externalMap)
						{
							phonemeMapper = model.externalMap.phonemeMap.GenerateAtoBDictionary();
						}
						else
						{
							callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Language Model specifies an external phoneme map, but no phoneme map was provided.", ClipFeatures.None));
							return;
						}
						break;
					default:
					case AutoSyncPhonemeMap.MappingMode.AutoDetect:
						phonemeMapper = AutoSyncUtility.FindBestFitPhonemeMap(model.sourcePhoneticAlphabetName, settings.phonemeSet.scriptingName);
						if (phonemeMapper == null)
						{
							callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, string.Format("No PhonemeMap could be found to map from '{0}' to the current PhonemeSet '{1}'.", model.sourcePhoneticAlphabetName, settings.phonemeSet.scriptingName), ClipFeatures.None));
							return;
						}
						break;
				}

				if (phonemeMapper.Count == 0)
				{
					Debug.LogWarning("PhonemeMap is empty - this may be due to the language model's mapping mode being set to 'InternalMap' but with no entries being added to the map. Phonemes may not be generated.");
				}
			}

			// Get Phones
			List<PhonemeMarker> data = new List<PhonemeMarker>();

			if (items != null && items.Length == 2)
			{
				for (int i = 0; i < items[1].intervals.Length; i++)
				{
					if (items[1].intervals[i] == null)
					{
						Debug.LogFormat("Interval {0} is null :o", i);
						continue;
					}

					string label = items[1].intervals[i].text.Split('"')[1];
					label = System.Text.RegularExpressions.Regex.Replace(label, "[0-9]", "");

					if (label != "sil")
					{
						if (phonemeMapper.ContainsKey(label))
						{
							string phonemeName = needsMapping ? phonemeMapper[label] : label;

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

								double start = items[1].intervals[i].xmin / inputClip.length;
								double end = items[1].intervals[i].xmax / inputClip.length;

								double length = end - start;
								if ((length * inputClip.length) < minLengthForSustain)
								{
									data.Add(new PhonemeMarker(phoneme, (float)(start + (length / 2))));
								}
								else
								{
									data.Add(new PhonemeMarker(phoneme, (float)start, 1, true));
									data.Add(new PhonemeMarker(phoneme, (float)end));
								}
							}
							else
							{
								Debug.LogWarning("Phoneme mapper returned '" + phonemeName + "' but this phoneme does not exist in the current set. Skipping this entry.");
							}
						}
						else
						{
							Debug.LogWarning("Phoneme mapper does not contain '" + label + "' Skipping this entry.");
						}
					}
				}
			}
			else
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, "Data loaded from MFA TextGrid file was invalid or incomplete.", ClipFeatures.None));
				return;
			}


			inputClip.phonemeData = data.ToArray();

			if (oovs.Count > 0)
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(true, "Completed, but some words were not found. Check the console.", GetOutputCompatibility()));
			}
			else
			{
				callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(true, "", GetOutputCompatibility()));
			}

			return;
		}
	}
}