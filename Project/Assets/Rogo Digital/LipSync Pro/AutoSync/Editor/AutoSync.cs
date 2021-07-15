using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

namespace RogoDigital.Lipsync.AutoSync
{
	public class AutoSync
	{
		private int index;
		private AutoSyncModule[] moduleSequence;
		private ASProcessDelegate onFinishedCallback;
		private bool silentMode = false;
		private ASProcessDelegateData finalData;

		private PhonemeMarker phonemeTemplate;
		private EmotionMarker emotionTemplate;

		public void RunSequence(AutoSyncModule[] moduleSequence, ASProcessDelegate onFinishedCallback, LipSyncData inputData, bool silent = false)
		{
			RunSequence(moduleSequence, onFinishedCallback, inputData, null, null, silent);
		}

		public void RunSequence(AutoSyncModule[] moduleSequence, ASProcessDelegate onFinishedCallback, LipSyncData inputData, PhonemeMarker phonemeTemplate, EmotionMarker emotionTemplate, bool silent = false)
		{
			index = 0;
			this.moduleSequence = moduleSequence;
			this.onFinishedCallback = onFinishedCallback;
			silentMode = silent;

			this.phonemeTemplate = phonemeTemplate;
			this.emotionTemplate = emotionTemplate;

			finalData = new ASProcessDelegateData(true, "", ClipFeatures.None);

			if (moduleSequence.Length > 0)
			{
				RunModuleSafely(moduleSequence[index], inputData, ProcessNext, phonemeTemplate, emotionTemplate, silent);
			}
		}

		public void RunModuleSafely(AutoSyncModule module, LipSyncData data, ASProcessDelegate callback, bool silent = false)
		{
			RunModuleSafely(module, data, callback, null, null, silent);
		}

		public void RunModuleSafely(AutoSyncModule module, LipSyncData data, ASProcessDelegate callback, PhonemeMarker phonemeTemplate, EmotionMarker emotionTemplate, bool silent = false)
		{
			if (finalData == null)
			{
				finalData = new ASProcessDelegateData(true, "", ClipFeatures.None);
			}

			var missingFeatures = AutoSyncUtility.GetMissingClipFeatures(data, module);

			if (missingFeatures == ClipFeatures.None)
			{
				if (!silent)
				{
					if (moduleSequence != null)
					{
						EditorUtility.DisplayProgressBar("AutoSync", string.Format("Processing module {0}/{1}. Please Wait.", index + 1, moduleSequence.Length), index + 1f / (float)moduleSequence.Length);
					}
					else
					{
						EditorUtility.DisplayProgressBar("AutoSync", "Processing module, Please Wait.", 1f);
					}
				}

				if (phonemeTemplate != null || emotionTemplate != null)
				{
					module.ProcessWithTemplates(data, callback, phonemeTemplate, emotionTemplate);
				}
				else
				{
					module.Process(data, callback);
				}
			}
			else
			{
				if (!silent)
					EditorUtility.ClearProgressBar();

				callback.Invoke(data, new ASProcessDelegateData(false, string.Format("Failed: Missing {0}. See console for details.", missingFeatures.ToString()), ClipFeatures.None));

				if ((missingFeatures & ClipFeatures.AudioClip) == ClipFeatures.AudioClip)
				{
					Debug.Log("Missing AudioClip. This module requires an audioclip in order to work. Make sure you've assigned an AudioClip at the top of the Clip Editor before running AutoSync.");
				}

				if ((missingFeatures & ClipFeatures.Emotions) == ClipFeatures.Emotions)
				{
					Debug.Log("Missing Emotions. This module requires Emotion markers in order to work. You will either need to add some using the Emotions tab of the Clip Editor, or by adding a module first that will create them.");
				}

				if ((missingFeatures & ClipFeatures.Gestures) == ClipFeatures.Gestures)
				{
					Debug.Log("Missing Gestures. This module requires Gesture markers in order to work. You will either need to add some using the Gestures tab of the Clip Editor, or by adding a module first that will create them.");
				}

				if ((missingFeatures & ClipFeatures.Phonemes) == ClipFeatures.Phonemes)
				{
					Debug.Log("Missing Phonemes. This module requires Phoneme markers in order to work. You will either need to add some using the Phonemes tab of the Clip Editor, or by adding a module first that will create them.");
				}

				if ((missingFeatures & ClipFeatures.Transcript) == ClipFeatures.Transcript)
				{
					Debug.Log("Missing Transcript. This module requires a transcript in order to work. The Clip Editor can load one from a text file with the same name as your AudioClip, or you can either add one from Edit>Clip Settings or add a module first that will create them.");
				}

				moduleSequence = null;
				finalData = null;
			}
		}

		private void ProcessNext(LipSyncData inputData, ASProcessDelegateData data)
		{
			index++;

			if (finalData == null)
			{
				finalData = new ASProcessDelegateData(true, "", ClipFeatures.None);
			}

			finalData.addedFeatures |= data.addedFeatures;

			if (data.success == false)
			{
				finalData.success = data.success;
				finalData.message = data.message;

				if (onFinishedCallback != null)
					onFinishedCallback.Invoke(null, finalData);

				moduleSequence = null;
				finalData = null;
				EditorApplication.Beep();

				if (!silentMode)
					EditorUtility.ClearProgressBar();

				return;
			}

			if (index >= moduleSequence.Length)
			{
				if (!silentMode)
					EditorUtility.ClearProgressBar();

				if (onFinishedCallback != null)
					onFinishedCallback.Invoke(inputData, finalData);

				moduleSequence = null;
				finalData = null;
				return;
			}
			else
			{
				RunModuleSafely(moduleSequence[index], inputData, ProcessNext, phonemeTemplate, emotionTemplate, silentMode);
			}
		}

		/// <summary>
		/// Delegate function used for returning data from AutoSync
		/// </summary>
		/// <param name="outputClip"></param>
		/// <param name="success"></param>
		/// <param name="customData"></param>
		public delegate void ASProcessDelegate(LipSyncData outputClip, ASProcessDelegateData data);

		public class ASProcessDelegateData
		{
			public bool success;
			public string message;
			public ClipFeatures addedFeatures;

			public ASProcessDelegateData(bool success, string message, ClipFeatures addedFeatures)
			{
				this.success = success;
				this.message = message;
				this.addedFeatures = addedFeatures;
			}
		}

		#region Obsolete Members
		[Obsolete("Please read lipsync.rogodigital.com/documentation for info on how the new AutoSync methods work.", true)]
		public delegate void AutoSyncDataReadyDelegate(AudioClip clip, List<PhonemeMarker> markers);

		[Obsolete("Please read lipsync.rogodigital.com/documentation for info on how the new AutoSync methods work.", true)]
		public delegate void AutoSyncFailedDelegate(string error);

		[Obsolete("Use AutoSyncUtility.VerifyProgramAtPath instead.")]
		public static bool CheckSoX()
		{
			string soXPath = EditorPrefs.GetString("LipSync_SoXPath");
			return AutoSyncUtility.VerifyProgramAtPath(soXPath, "SoX");
		}

		[Obsolete("Please read lipsync.rogodigital.com/documentation for info on how the new AutoSync methods work.", true)]
		public static void ProcessAudio(AudioClip clip, AutoSyncDataReadyDelegate dataReadyCallback, AutoSyncFailedDelegate failedCallback, string progressPrefix, AutoSyncOptions options)
		{
		}

		[Obsolete("Please read lipsync.rogodigital.com/documentation for info on how the new AutoSync methods work.", true)]
		public static void ProcessAudio(AudioClip clip, AutoSyncDataReadyDelegate callback, AutoSyncFailedDelegate failedCallback, AutoSyncOptions options)
		{
		}

		[Obsolete("Please read lipsync.rogodigital.com/documentation for info on how the new AutoSync methods work.", true)]
		public static List<PhonemeMarker> CleanupOutput(List<PhonemeMarker> data, float aggressiveness)
		{
			throw new NotImplementedException();
		}

		[Obsolete("Use AutoSyncDataReadyDelegate instead.")]
		public delegate void AutoSyncDataReady(AudioClip clip, List<PhonemeMarker> markers);

		[Obsolete("Use ASPocketSphinxOptionsPreset instead.")]
		public enum AutoSyncOptionsPreset
		{
			Default,
			HighQuality,
		}

		[Obsolete("Use ASPocketSphinxOptions instead.")]
		public struct AutoSyncOptions
		{
			public string languageModel;
			public bool useAudioConversion;
			public bool allphone_ciEnabled;
			public bool backtraceEnabled;
			public int beamExponent;
			public int pbeamExponent;
			public float lwValue;
			public bool doCleanup;
			public float cleanupAggression;
		}
		#endregion
	}
}