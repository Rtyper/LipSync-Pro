using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace RogoDigital.Lipsync.AutoSync
{
	[CustomEditor(typeof(ASPocketSphinxPhonemeDetectionModule))]
	public class ASPocketSphinxPhonemeDetectionModuleEditor : Editor
	{
		private bool showAdvancedOptions;
		private string[] languageModelNames;

		private void OnEnable ()
		{
			languageModelNames = ASPocketSphinxLanguageModel.FindModels();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var lmProp = serializedObject.FindProperty("languageModel");
			lmProp.intValue = EditorGUILayout.Popup(lmProp.displayName, lmProp.intValue, languageModelNames);
			GUILayout.Space(5);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("useAudioConversion"));
			showAdvancedOptions = EditorGUILayout.Toggle("Show Advanced Options", showAdvancedOptions);
			if (showAdvancedOptions)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("allphone_ciEnabled"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("backtraceEnabled"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("beamExponent"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("pbeamExponent"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("lwValue"));
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}