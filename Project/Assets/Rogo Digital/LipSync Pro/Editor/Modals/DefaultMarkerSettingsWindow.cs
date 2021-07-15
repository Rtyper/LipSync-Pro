using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace RogoDigital.Lipsync {
	public class DefaultMarkerSettingsWindow : ModalWindow {
		private LipSyncClipSetup setup;
		private int markerType;
		private Vector2 scrollPosition;
		private AnimBool modifierBool;

		private float intensity;
		private bool modifierOn;
		private float maxVariationFrequency;
		private float intensityModifier;
		private float blendableModifier;
		private float bonePositionModifier;
		private float boneRotationModifier;

		void OnGUI () {
			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (markerType == 0) {
				GUILayout.Label("Editing Default Phoneme Marker");
			} else {
				GUILayout.Label("Editing Default Emotion Marker");
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			intensity = EditorGUILayout.Slider("Intensity", intensity, 0, 1);
			modifierOn = EditorGUILayout.Toggle(markerType == 0 ? "Use Randomess" : "Use Continuous Variation", modifierOn);
			modifierBool.target = modifierOn;
			if (EditorGUILayout.BeginFadeGroup(modifierBool.faded)) {
				if (markerType == 1) {
					GUILayout.BeginHorizontal();
					maxVariationFrequency = EditorGUILayout.Slider("Vary every:", maxVariationFrequency, 0.2f, 3);
					GUILayout.Label(" seconds");
					GUILayout.EndHorizontal();
				}
				intensityModifier = EditorGUILayout.Slider(markerType == 0 ? "Intensity Randomness" : "Intensity Variation", intensityModifier, 0, 1);
				blendableModifier = EditorGUILayout.Slider(markerType == 0 ? "Blendable Value Randomness" : "Blendable Value Variation", blendableModifier, 0, 1);
				bonePositionModifier = EditorGUILayout.Slider(markerType == 0 ? "Bone Position Randomness" : "Bone Position Variation", bonePositionModifier, 0, 1);
				boneRotationModifier = EditorGUILayout.Slider(markerType == 0 ? "Bone Rotation Randomness" : "Bone Rotation Variation", boneRotationModifier, 0, 1);
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.EndScrollView();
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Accept", GUILayout.MinWidth(100), GUILayout.Height(20))) {

				if (markerType == 0) {
					EditorPrefs.SetFloat("LipSync_DefaultPhonemeIntensity", intensity);
					EditorPrefs.SetBool("LipSync_DefaultUseRandomness", modifierOn);
					EditorPrefs.SetFloat("LipSync_DefaultIntensityRandomness", intensityModifier);
					EditorPrefs.SetFloat("LipSync_DefaultBlendableRandomness", blendableModifier);
					EditorPrefs.SetFloat("LipSync_DefaultBonePositionRandomness", bonePositionModifier);
					EditorPrefs.SetFloat("LipSync_DefaultBoneRotationRandomness", boneRotationModifier);

					setup.defaultPhonemeIntensity = intensity;
					setup.defaultUseRandomness = modifierOn;
					setup.defaultIntensityRandomness = intensityModifier;
					setup.defaultBlendableRandomness = blendableModifier;
					setup.defaultBonePositionRandomness = bonePositionModifier;
					setup.defaultBoneRotationRandomness = boneRotationModifier;
				} else {
					EditorPrefs.SetFloat("LipSync_DefaultEmotionIntensity", intensity);
					EditorPrefs.SetBool("LipSync_DefaultContinuousVariation", modifierOn);
					EditorPrefs.SetFloat("LipSync_DefaultVariationFrequency", maxVariationFrequency);
					EditorPrefs.SetFloat("LipSync_DefaultIntensityVariation", intensityModifier);
					EditorPrefs.SetFloat("LipSync_DefaultBlendableVariation", blendableModifier);
					EditorPrefs.SetFloat("LipSync_DefaultBonePositionVariation", bonePositionModifier);
					EditorPrefs.SetFloat("LipSync_DefaultBoneRotationVariation", boneRotationModifier);

					setup.defaultEmotionIntensity = intensity;
					setup.defaultUseRandomness = modifierOn;
					setup.defaultVariationFrequency = maxVariationFrequency;
					setup.defaultIntensityVariation = intensityModifier;
					setup.defaultBlendableVariation = blendableModifier;
					setup.defaultBonePositionVariation = bonePositionModifier;
					setup.defaultBoneRotationVariation = boneRotationModifier;
				}

				Close();
			}
			GUILayout.Space(10);
			if (GUILayout.Button("Cancel", GUILayout.MinWidth(100), GUILayout.Height(20))) {
				Close();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
		}

		public static void CreateWindow (ModalParent parent, LipSyncClipSetup setup, int markerType) {
			DefaultMarkerSettingsWindow window = Create(parent, setup, 0);

			window.markerType = markerType;
			if (markerType == 0) {
				window.intensity = setup.defaultPhonemeIntensity;
				window.modifierOn = setup.defaultUseRandomness;
				window.intensityModifier = setup.defaultIntensityRandomness;
				window.blendableModifier = setup.defaultBlendableRandomness;
				window.boneRotationModifier = setup.defaultBonePositionRandomness;
				window.bonePositionModifier = setup.defaultBoneRotationRandomness;
			} else if (markerType == 1) {
				window.intensity = setup.defaultEmotionIntensity;
				window.modifierOn = setup.defaultContinuousVariation;
				window.maxVariationFrequency = setup.defaultVariationFrequency;
				window.intensityModifier = setup.defaultIntensityVariation;
				window.blendableModifier = setup.defaultBlendableVariation;
				window.boneRotationModifier = setup.defaultBonePositionVariation;
				window.bonePositionModifier = setup.defaultBoneRotationVariation;
			}

			window.modifierBool = new AnimBool(window.modifierOn, window.Repaint);
		}

		private static DefaultMarkerSettingsWindow Create (ModalParent parent, LipSyncClipSetup setup, int markerType) {
			DefaultMarkerSettingsWindow window = CreateInstance<DefaultMarkerSettingsWindow>();

			window.position = new Rect(parent.center.x - 250, parent.center.y - 100, 500, 200);
			window.minSize = new Vector2(500, 200);
			window.titleContent = new GUIContent("Default Marker Settings");

			window.setup = setup;
			window.markerType = markerType;
			window.Show(parent);
			return window;
		}
	}
}