using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace RogoDigital.Lipsync.AutoSync
{
	[CustomEditor(typeof(ASMontrealPhonemeDetectionModule))]
	public class ASMontrealPhonemeDetectionModuleEditor : Editor
	{
		private string[] languageModelNames;

		private void OnEnable ()
		{
			languageModelNames = ASMontrealLanguageModel.FindModels();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var lmProp = serializedObject.FindProperty("languageModel");
			lmProp.intValue = EditorGUILayout.Popup(lmProp.displayName, lmProp.intValue, languageModelNames);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("useAudioConversion"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("minLengthForSustain"));
			EditorGUILayout.Space();
			var retry = serializedObject.FindProperty("autoRetry");
			EditorGUILayout.PropertyField(retry);
			if (retry.boolValue)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAttempts"));
			}

			//EditorGUILayout.PropertyField(serializedObject.FindProperty("configPath"));

			serializedObject.ApplyModifiedProperties();
		}
	}
}