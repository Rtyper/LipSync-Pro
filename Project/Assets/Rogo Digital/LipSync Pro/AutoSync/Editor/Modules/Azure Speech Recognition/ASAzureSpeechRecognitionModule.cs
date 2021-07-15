using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;
using UnityEditor;

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace RogoDigital.Lipsync.AutoSync
{
	[AutoSyncModuleInfo("Transcription/Microsoft Azure Speech Recognition", "Provides automatic transcription on Windows, using Microsoft Azure speech recognition.", "Rogo Digital", moduleSettingsType = typeof(ASAzureSpeechRecognitionSettings))]
	public class ASAzureSpeechRecognitionModule : AutoSyncModule
	{
		public static readonly string[] languageCategoryNames = { "Arabic", "Catalan", "Chinese", "Danish", "Dutch", "English", "Finnish", "French", "German", "Gujarati", "Hindi", "Italian", "Japanese", "Korean", "Marathi", "Norwegian", "Polish", "Portuguese", "Russian", "Spanish", "Swedish", "Tamil", "Telugu", "Thai", "Turkish" };
		public static readonly string[][] languageDisplayNames = {
			new string[]{ "UAE", "Bahrain (Modern Standard)", "Egypt", "Israel", "Jordan", "Kuwait", "Lebanon", "Palestine", "Qatar", "Saudi Arabia", "Syria"},
			new string[]{ "Catalan" },
			new string[]{ "Mandarin (Simplified)", "Cantonese (Traditional)", "Taiwanese Mandarin" },
			new string[]{ "Denmark" },
			new string[]{ "Netherlands" },
			new string[]{ "Australia", "Canada", "United Kingdom", "India", "New Zealand", "United States" },
			new string[]{ "Finland" },
			new string[]{ "Canada", "France" },
			new string[]{ "Germany" },
			new string[]{ "Indian" },
			new string[]{ "India" },
			new string[]{ "Italy" },
			new string[]{ "Japan" },
			new string[]{ "Korea" },
			new string[]{ "India" },
			new string[]{ "Norway (Bokmål)" },
			new string[]{ "Poland" },
			new string[]{ "Portugal" },
			new string[]{ "Russia" },
			new string[]{ "Spain", "Mexico" },
			new string[]{ "Sweden" },
			new string[]{ "India" },
			new string[]{ "India" },
			new string[]{ "Thailand" },
			new string[]{ "Turkey" },
		};
		public static readonly string[][] languageValueNames = {
			new string[]{ "ar-AE", "ar-BH", "ar-EG", "ar-IL", "ar-JO", "ar-KW", "ar-LB", "ar-PS", "ar-QA", "ar-SA", "ar-SY"},
			new string[]{ "ca-ES" },
			new string[]{ "zh-CN", "zh-HK", "zh-TW" },
			new string[]{ "da-DK" },
			new string[]{ "nl-NL" },
			new string[]{ "en-AU", "en-CA", "en-GB", "en-IN", "en-NZ", "en-US" },
			new string[]{ "fi-FI" },
			new string[]{ "fr-CA", "fr-FR" },
			new string[]{ "de-DE" },
			new string[]{ "gu-IN" },
			new string[]{ "hi-IN" },
			new string[]{ "it-IT" },
			new string[]{ "ja-JP" },
			new string[]{ "ko-KR" },
			new string[]{ "mr-IN" },
			new string[]{ "nb-NO" },
			new string[]{ "pl-PL" },
			new string[]{ "pt-PT" },
			new string[]{ "ru-RU" },
			new string[]{ "es-ES", "es-MX" },
			new string[]{ "sv-SE" },
			new string[]{ "ta-IN" },
			new string[]{ "te-IN" },
			new string[]{ "th-TH" },
			new string[]{ "tr-TR" },
		};

		public int langaugeCatChoice = 5;
		public int languageChoice = 5;

		public override ClipFeatures GetCompatibilityRequirements()
		{
			return ClipFeatures.AudioClip;
		}

		public override ClipFeatures GetOutputCompatibility()
		{
			return ClipFeatures.Transcript;
		}

		public override async void Process(LipSyncData inputClip, AutoSync.ASProcessDelegate callback)
		{
			var config = SpeechConfig.FromSubscription(EditorPrefs.GetString("as_azurespeech_subscription_key"), EditorPrefs.GetString("as_azurespeech_region_name"));
			config.SpeechRecognitionLanguage = languageValueNames[langaugeCatChoice][languageChoice];
			config.OutputFormat = OutputFormat.Detailed;
			config.SetProfanity(ProfanityOption.Raw);

			string audioPath = Application.dataPath + "/" + AssetDatabase.GetAssetPath(inputClip.clip).Substring("/Assets".Length);
			string convertedAudioPath = Application.temporaryCachePath + "/" + Path.GetFileNameWithoutExtension(audioPath) + Random.Range(1000, 9999) + "_AZConverted.wav";

			if (AutoSyncConversionUtility.IsConversionAvailable)
			{
				AutoSyncConversionUtility.StartConversion(audioPath, convertedAudioPath, AutoSyncConversionUtility.AudioFormat.WavPCM, 16000, 16, 1);
			}
			audioPath = convertedAudioPath;

			using (var audioInput = AudioConfig.FromWavFileInput(audioPath))
			{
				using (var recognizer = new SpeechRecognizer(config, audioInput))
				{
					var finalText = "";
					var stopRecognition = new TaskCompletionSource<int>();

					recognizer.Recognized += (context, args) =>
					{
						if (args.Result.Reason == ResultReason.RecognizedSpeech)
						{
							double confidence = 0;
							string text = "";

							foreach (var item in args.Result.Best())
							{
								if (item.Confidence > confidence)
								{
									confidence = item.Confidence;
									text = item.LexicalForm;
								}
							}

							finalText += " " + text;
						}
						else if (args.Result.Reason == ResultReason.NoMatch)
						{
							Debug.Log("NOMATCH: Speech could not be recognised.");
						}
					};

					recognizer.SessionStopped += (context, args) =>
					{
						stopRecognition.TrySetResult(0);
					};

					recognizer.Canceled += (context, args) =>
					{
						var cancellation = CancellationDetails.FromResult(args.Result);

						stopRecognition.TrySetResult(0);
						if (cancellation.Reason == CancellationReason.Error)
						{
							Debug.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
							Debug.LogError($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
							Debug.LogError($"CANCELED: Did you update the subscription info?");

							callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(false, $"Detection cancelled: {cancellation.Reason}", ClipFeatures.None));
							return;
						}
					};

					await recognizer.StartContinuousRecognitionAsync();
					Task.WaitAny(new[] { stopRecognition.Task });
					await recognizer.StopContinuousRecognitionAsync();

					inputClip.transcript = finalText;
					callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(true, "", GetOutputCompatibility()));
				}
			}
		}
	}
}