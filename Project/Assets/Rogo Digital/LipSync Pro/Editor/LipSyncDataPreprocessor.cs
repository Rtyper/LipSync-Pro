using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RogoDigital.Lipsync
{
	public class LipSyncDataPreprocessor : EditorWindow
	{
		private List<LipSyncData> dataFiles = new List<LipSyncData>();
		private LipSync character;

		private List<int> indexBlendables;
		private List<AnimationCurve> animCurves;

		private List<Transform> bones;
		private List<TransformAnimationCurve> boneCurves;

		private List<Vector3> boneNeutralPositions;
		private List<Vector3> boneNeutralScales;
		private List<Quaternion> boneNeutralRotations;

		private void OnEnable()
		{

		}

		private void OnGUI()
		{
			EditorGUILayout.HelpBox("This utility lets you improve runtime performance by pre-processing the animation for a LipSyncData clip and a specific LipSync character, and storing it in the clip.\nThis can help remove any pauses or frame drops when starting to play a LipSyncData clip, but will tie the clip to a specific character. Attempting to play a pre-processed clip on a different character may not work correctly.", MessageType.None);
			EditorGUILayout.Space();
			character = (LipSync)EditorGUILayout.ObjectField("LipSync Character", character, typeof(LipSync), true);
			EditorGUILayout.Space();
			GUILayout.Label("LipSyncData Clips");
			GUILayout.Space(2);
			for (int i = 0; i < dataFiles.Count; i++)
			{
				GUILayout.BeginHorizontal();
				dataFiles[i] = (LipSyncData)EditorGUILayout.ObjectField(dataFiles[i], typeof(LipSyncData), true);
				if (GUILayout.Button("Remove", GUILayout.Width(100)))
				{
					dataFiles.RemoveAt(i);
					return;
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.Space(2);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Add Empty Slot"))
			{
				dataFiles.Add(null);
			}
			if (GUILayout.Button("Add Selected Clip(s)"))
			{
				var select = Selection.GetFiltered<LipSyncData>(SelectionMode.Assets);
				for (int i = 0; i < select.Length; i++)
				{
					if (!dataFiles.Contains(select[i]))
						dataFiles.Add(select[i]);
				}
			}
			EditorGUILayout.EndHorizontal();
			if (GUILayout.Button("Add All Clips In Project"))
			{
				dataFiles.Clear();

				var all = AssetDatabase.FindAssets("t:LipSyncData");
				Resources.FindObjectsOfTypeAll<LipSyncData>();
				for (int i = 0; i < all.Length; i++)
				{
					dataFiles.Add(AssetDatabase.LoadAssetAtPath<LipSyncData>(AssetDatabase.GUIDToAssetPath(all[i])));
				}
			}
			EditorGUILayout.Space();

			EditorGUI.BeginDisabledGroup(!character || dataFiles.Count == 0);
			if (GUILayout.Button("Process Data"))
			{
				bool overwrite = false;
				int actualCount = 0;
				for (int i = 0; i < dataFiles.Count; i++)
				{
					var path = AssetDatabase.GetAssetPath(dataFiles[i]);
					if (string.IsNullOrEmpty(path))
						continue;

					if (dataFiles[i].isPreprocessed && !overwrite)
					{
						var answer = EditorUtility.DisplayDialogComplex("Overwrite Data?", "The clip '" + dataFiles[i].name + "' already has pre-processed data. Are you sure you want to continue and overwrite this data?", "Yes", "No", "Yes To All");
						if (answer == 1)
						{
							continue;
						}
						else if (answer == 2)
						{
							overwrite = true;
						}
					}

					EditorUtility.DisplayProgressBar("Processing Data", "Processing Clip " + i + " of " + dataFiles.Count, dataFiles.Count / (float)i);

					character.TempLoad(dataFiles[i].phonemeData, dataFiles[i].emotionData, dataFiles[i].clip, dataFiles[i].length);
					character.ProcessData();

					dataFiles[i].isPreprocessed = true;
					character.GetCurveDataOut(out dataFiles[i].indexBlendables, out dataFiles[i].animCurves, out dataFiles[i].bones, out dataFiles[i].boneCurves, out dataFiles[i].boneNeutralPositions, out dataFiles[i].boneNeutralRotations, out dataFiles[i].boneNeutralScales);
					dataFiles[i].targetComponentID = character.GetInstanceID();

					LipSyncData outputFile = AssetDatabase.LoadAssetAtPath<LipSyncData>(path);

					if (outputFile != null)
					{
						EditorUtility.CopySerialized(dataFiles[i], outputFile);
					}
					else
					{
						outputFile = CreateInstance<LipSyncData>();
						EditorUtility.CopySerialized(dataFiles[i], outputFile);
						AssetDatabase.CreateAsset(outputFile, path);
					}

					actualCount++;
				}

				EditorUtility.ClearProgressBar();
				EditorUtility.DisplayDialog("Processing Complete", "Finished processing " + actualCount + " clip(s).", "Ok");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				Close();
			}
			EditorGUI.EndDisabledGroup();
		}

		[MenuItem("Window/Rogo Digital/LipSync Pro/Preprocess Data")]
		public static void ShowWindow()
		{
			GetWindow<LipSyncDataPreprocessor>(true, "Preprocessor");
		}
	}
}