using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;
using System.Reflection;

namespace RogoDigital.Lipsync
{
	public class MarkerSettingsWindow : ModalWindow
	{
		private LipSyncClipSetup setup;

		private int markerType;

		// Single Marker
		private PhonemeMarker[] pMarker;
		private EmotionMarker[] eMarker;

		private Vector2 scrollPosition;
		private AnimBool modifierBool;

		private string[] phonemeNames;
		private int[] popupValues;

		private string[] emotionNames;

		private float time;
		private float startTime;
		private float endTime;
		private int phonemeNumber;
		private int emotionNumber;
		private float intensity;
		private bool modifierOn;
		private float maxVariationFrequency;
		private float intensityModifier;
		private float blendableModifier;
		private float bonePositionModifier;
		private float boneRotationModifier;

		private bool setTime = true;
		private bool setStartTime = true;
		private bool setEndTime = true;
		private bool setPhonemeNumber = true;
		private bool setEmotion = true;
		private bool setIntensity = true;
		private bool setModifierOn = true;
		private bool setMaxVariationFrequency = true;
		private bool setIntensityModifier = true;
		private bool setBlendableModifier = true;
		private bool setBonePositionModifier = true;
		private bool setBoneRotationModifier = true;

		void OnGUI()
		{
			GUILayout.Space(10);

			if (setup.settings.phonemeSet.isLegacyFormat)
			{
				// PhonemeSet needs updating
				EditorGUILayout.HelpBox("PhonemeSet needs updating! Go to 'Window > Rogo Digital > LipSync Pro > Update PhonemeSets' to fix.", MessageType.Error);
				return;
			}

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (markerType == 0)
			{
				if (pMarker.Length == 1)
				{
					if (pMarker[0].phonemeNumber > setup.settings.phonemeSet.phonemeList.Count)
					{
						// Marker is out of range
					}
					else
					{
						GUILayout.Label("Editing " + setup.settings.phonemeSet.phonemeList[pMarker[0].phonemeNumber].name + " Phoneme Marker at " + (pMarker[0].time * setup.FileLength).ToString() + "s.");
					}
				}
				else
				{
					GUILayout.Label("Editing " + pMarker.Length + " Phoneme markers between " + (pMarker[0].time * setup.FileLength) + "s and " + (pMarker[pMarker.Length - 1].time * setup.FileLength) + "s.");
				}
			}
			else
			{
				if (eMarker.Length == 1)
				{
					GUILayout.Label("Editing " + eMarker[0].emotion + " Emotion Marker at " + (eMarker[0].startTime * setup.FileLength).ToString() + "s.");
				}
				else
				{
					GUILayout.Label("Editing " + eMarker.Length + " Emotion markers between " + (eMarker[0].startTime * setup.FileLength) + "s and " + (eMarker[eMarker.Length - 1].endTime * setup.FileLength) + "s.");
				}
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			if (markerType == 0)
			{
				time = FlaggedFloatField("Marker Time", time, ref setTime);
				phonemeNumber = FlaggedIntPopup("Phoneme", phonemeNumber, phonemeNames, popupValues, ref setPhonemeNumber);
			}
			else
			{
				startTime = FlaggedFloatField("Start Time", startTime, ref setStartTime);
				endTime = FlaggedFloatField("End Time", endTime, ref setEndTime);
				emotionNumber = FlaggedIntPopup("Emotion", emotionNumber, emotionNames, popupValues, ref setEmotion);
			}
			GUILayout.Space(10);
			intensity = FlaggedSlider("Intensity", intensity, 0, 1, ref setIntensity);
			modifierOn = FlaggedToggle(markerType == 0 ? "Use Randomess" : "Use Continuous Variation", modifierOn, ref setModifierOn);
			modifierBool.target = modifierOn;
			if (EditorGUILayout.BeginFadeGroup(modifierBool.faded))
			{
				if (markerType == 1)
				{
					GUILayout.BeginHorizontal();
					maxVariationFrequency = FlaggedSlider("Vary every:", maxVariationFrequency, 0.2f, 3, ref setMaxVariationFrequency);
					GUILayout.Label(" seconds");
					GUILayout.EndHorizontal();
				}
				intensityModifier = FlaggedSlider(markerType == 0 ? "Intensity Randomness" : "Intensity Variation", intensityModifier, 0, 1, ref setIntensityModifier);
				blendableModifier = FlaggedSlider(markerType == 0 ? "Blendable Value Randomness" : "Blendable Value Variation", blendableModifier, 0, 1, ref setBlendableModifier);
				bonePositionModifier = FlaggedSlider(markerType == 0 ? "Bone Position Randomness" : "Bone Position Variation", bonePositionModifier, 0, 1, ref setBonePositionModifier);
				boneRotationModifier = FlaggedSlider(markerType == 0 ? "Bone Rotation Randomness" : "Bone Rotation Variation", boneRotationModifier, 0, 1, ref setBoneRotationModifier);
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.EndScrollView();
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Accept", GUILayout.MinWidth(100), GUILayout.Height(20)))
			{
				if (markerType == 0)
				{
					for (int i = 0; i < pMarker.Length; i++)
					{
						if (setTime)
							pMarker[i].time = time;
						if (setPhonemeNumber)
							pMarker[i].phonemeNumber = phonemeNumber;
						if (setIntensity)
							pMarker[i].intensity = intensity;

						if (setModifierOn)
							pMarker[i].useRandomness = modifierOn;
						if (setIntensityModifier)
							pMarker[i].intensityRandomness = intensityModifier;
						if (setBlendableModifier)
							pMarker[i].blendableRandomness = blendableModifier;
						if (setBonePositionModifier)
							pMarker[i].bonePositionRandomness = bonePositionModifier;
						if (setBoneRotationModifier)
							pMarker[i].boneRotationRandomness = boneRotationModifier;
					}
				}
				else
				{
					for (int i = 0; i < eMarker.Length; i++)
					{
						if (setStartTime)
							eMarker[i].startTime = startTime;
						if (setEndTime)
							eMarker[i].endTime = endTime;
						if (setEmotion)
							eMarker[i].emotion = setup.settings.emotions[emotionNumber];
						if (setIntensity)
							eMarker[i].intensity = intensity;

						if (setModifierOn)
							eMarker[i].continuousVariation = modifierOn;
						if (setMaxVariationFrequency)
							eMarker[i].variationFrequency = maxVariationFrequency;
						if (setIntensityModifier)
							eMarker[i].intensityVariation = intensityModifier;
						if (setBlendableModifier)
							eMarker[i].blendableVariation = blendableModifier;
						if (setBonePositionModifier)
							eMarker[i].bonePositionVariation = bonePositionModifier;
						if (setBoneRotationModifier)
							eMarker[i].boneRotationVariation = boneRotationModifier;
					}
				}
				setup.changed = true;
				setup.previewOutOfDate = true;
				Close();
			}
			GUILayout.Space(10);
			if (GUILayout.Button("Cancel", GUILayout.MinWidth(100), GUILayout.Height(20)))
			{
				Close();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
		}

		public static void CreateWindow(ModalParent parent, LipSyncClipSetup setup, PhonemeMarker marker)
		{
			MarkerSettingsWindow window = Create(parent, setup, 0);
			window.pMarker = new PhonemeMarker[] { marker };

			window.time = marker.time;
			window.phonemeNumber = marker.phonemeNumber;
			window.intensity = marker.intensity;
			window.modifierOn = marker.useRandomness;
			window.intensityModifier = marker.intensityRandomness;
			window.blendableModifier = marker.blendableRandomness;
			window.boneRotationModifier = marker.boneRotationRandomness;
			window.bonePositionModifier = marker.bonePositionRandomness;

			window.modifierBool = new AnimBool(window.modifierOn, window.Repaint);
		}

		public static void CreateWindow(ModalParent parent, LipSyncClipSetup setup, EmotionMarker marker)
		{
			MarkerSettingsWindow window = Create(parent, setup, 1);
			window.eMarker = new EmotionMarker[] { marker };

			for (int i = 0; i < setup.settings.emotions.Length; i++)
			{
				if (setup.settings.emotions[i] == marker.emotion)
				{
					window.emotionNumber = i;
					break;
				}
			}

			window.startTime = marker.startTime;
			window.endTime = marker.endTime;
			window.intensity = marker.intensity;
			window.modifierOn = marker.continuousVariation;
			window.maxVariationFrequency = marker.variationFrequency;
			window.intensityModifier = marker.intensityVariation;
			window.blendableModifier = marker.blendableVariation;
			window.boneRotationModifier = marker.boneRotationVariation;
			window.bonePositionModifier = marker.bonePositionVariation;

			window.modifierBool = new AnimBool(window.modifierOn, window.Repaint);
		}

		public static void CreateWindow(ModalParent parent, LipSyncClipSetup setup, List<PhonemeMarker> collection, List<int> selection)
		{
			MarkerSettingsWindow window = Create(parent, setup, 0);
			window.pMarker = new PhonemeMarker[selection.Count];

			for (int i = 0; i < selection.Count; i++)
			{
				window.pMarker[i] = collection[selection[i]];
			}

			SetProperty(GetFieldValues<float>(window.pMarker, "time"), out window.time, out window.setTime);
			SetProperty(GetFieldValues<int>(window.pMarker, "phonemeNumber"), out window.phonemeNumber, out window.setPhonemeNumber);
			SetProperty(GetFieldValues<float>(window.pMarker, "intensity"), out window.intensity, out window.setIntensity);
			SetProperty(GetFieldValues<bool>(window.pMarker, "useRandomness"), out window.modifierOn, out window.setModifierOn);
			SetProperty(GetFieldValues<float>(window.pMarker, "intensityRandomness"), out window.intensityModifier, out window.setIntensityModifier);
			SetProperty(GetFieldValues<float>(window.pMarker, "blendableRandomness"), out window.blendableModifier, out window.setBlendableModifier);
			SetProperty(GetFieldValues<float>(window.pMarker, "boneRotationRandomness"), out window.boneRotationModifier, out window.setBoneRotationModifier);
			SetProperty(GetFieldValues<float>(window.pMarker, "bonePositionRandomness"), out window.bonePositionModifier, out window.setBonePositionModifier);

			window.modifierBool = new AnimBool(window.modifierOn, window.Repaint);
		}

		public static void CreateWindow(ModalParent parent, LipSyncClipSetup setup, List<EmotionMarker> collection, List<int> selection)
		{
			MarkerSettingsWindow window = Create(parent, setup, 1);
			window.eMarker = new EmotionMarker[selection.Count];

			for (int i = 0; i < selection.Count; i++)
			{
				window.eMarker[i] = collection[selection[i]];
			}

			string[] emotions = GetFieldValues<string>(window.eMarker, "emotion");
			int[] emotionNumbers = new int[emotions.Length];

			for (int e = 0; e < emotions.Length; e++)
			{
				for (int i = 0; i < setup.settings.emotions.Length; i++)
				{
					if (string.Equals(setup.settings.emotions[i], emotions[e]))
					{
						emotionNumbers[e] = i;
						break;
					}
				}
			}

			SetProperty(emotionNumbers, out window.emotionNumber, out window.setEmotion);
			SetProperty(GetFieldValues<float>(window.eMarker, "startTime"), out window.startTime, out window.setStartTime);
			SetProperty(GetFieldValues<float>(window.eMarker, "endTime"), out window.endTime, out window.setEndTime);
			SetProperty(GetFieldValues<float>(window.eMarker, "intensity"), out window.intensity, out window.setIntensity);
			SetProperty(GetFieldValues<bool>(window.eMarker, "continuousVariation"), out window.modifierOn, out window.setModifierOn);
			SetProperty(GetFieldValues<float>(window.eMarker, "variationFrequency"), out window.maxVariationFrequency, out window.setMaxVariationFrequency);
			SetProperty(GetFieldValues<float>(window.eMarker, "intensityVariation"), out window.intensityModifier, out window.setIntensityModifier);
			SetProperty(GetFieldValues<float>(window.eMarker, "blendableVariation"), out window.blendableModifier, out window.setBlendableModifier);
			SetProperty(GetFieldValues<float>(window.eMarker, "boneRotationVariation"), out window.boneRotationModifier, out window.setBoneRotationModifier);
			SetProperty(GetFieldValues<float>(window.eMarker, "bonePositionVariation"), out window.bonePositionModifier, out window.setBonePositionModifier);

			window.modifierBool = new AnimBool(window.modifierOn, window.Repaint);
		}

		private static MarkerSettingsWindow Create(ModalParent parent, LipSyncClipSetup setup, int markerType)
		{
			MarkerSettingsWindow window = CreateInstance<MarkerSettingsWindow>();


			window.phonemeNames = new string[setup.settings.phonemeSet.phonemeList.Count];
			for (int i = 0; i < window.phonemeNames.Length; i++)
			{
				window.phonemeNames[i] = setup.settings.phonemeSet.phonemeList[i].name;
			}

			window.emotionNames = setup.settings.emotions;

			window.popupValues = new int[128];

			for (int i = 0; i < window.popupValues.Length; i++)
			{
				window.popupValues[i] = i;
			}

			window.position = new Rect(parent.center.x - 250, parent.center.y - 100, 500, 200);
			window.minSize = new Vector2(500, 200);
			window.titleContent = new GUIContent("Marker Settings");

			window.setup = setup;
			window.markerType = markerType;
			window.Show(parent);
			return window;
		}

		private static T[] GetFieldValues<T>(object[] sources, string fieldName)
		{
			FieldInfo field = sources[0].GetType().GetField(fieldName);
			T[] array = new T[sources.Length];

			for (int i = 0; i < sources.Length; i++)
			{
				array[i] = (T)field.GetValue(sources[i]);
			}

			return array;
		}

		private static void SetProperty<T>(T[] sources, out T destination, out bool flag) where T : struct
		{
			T comparer = sources[0];
			for (int i = 1; i < sources.Length; i++)
			{
				if (!Compare(sources[i], (comparer)))
				{
					flag = false;
					destination = default(T);
					return;
				}
			}

			flag = true;
			destination = comparer;
		}

		public static bool Compare<T>(T x, T y)
		{
			return EqualityComparer<T>.Default.Equals(x, y);
		}

		private static float FlaggedFloatField(string label, float value, ref bool flag)
		{
			EditorGUI.showMixedValue = !flag;
			EditorGUI.BeginChangeCheck();
			float outValue = EditorGUILayout.FloatField(label, value);

			if (EditorGUI.EndChangeCheck())
			{
				flag = true;
			}
			EditorGUI.showMixedValue = false;
			return outValue;
		}

		private static int FlaggedIntPopup(string label, int value, string[] optionLabels, int[] optionValues, ref bool flag)
		{
			EditorGUI.showMixedValue = !flag;
			EditorGUI.BeginChangeCheck();
			int outValue = EditorGUILayout.IntPopup(label, value, optionLabels, optionValues);

			if (EditorGUI.EndChangeCheck())
			{
				flag = true;
			}
			EditorGUI.showMixedValue = false;
			return outValue;
		}

		private static float FlaggedSlider(string label, float value, float leftValue, float rightValue, ref bool flag)
		{
			EditorGUI.showMixedValue = !flag;
			EditorGUI.BeginChangeCheck();
			float outValue = EditorGUILayout.Slider(label, value, leftValue, rightValue);

			if (EditorGUI.EndChangeCheck())
			{
				flag = true;
			}
			EditorGUI.showMixedValue = false;
			return outValue;
		}

		private static bool FlaggedToggle(string label, bool value, ref bool flag)
		{
			EditorGUI.showMixedValue = !flag;
			EditorGUI.BeginChangeCheck();
			bool outValue = EditorGUILayout.Toggle(label, value);

			if (EditorGUI.EndChangeCheck())
			{
				flag = true;
			}
			EditorGUI.showMixedValue = false;
			return outValue;
		}
	}
}