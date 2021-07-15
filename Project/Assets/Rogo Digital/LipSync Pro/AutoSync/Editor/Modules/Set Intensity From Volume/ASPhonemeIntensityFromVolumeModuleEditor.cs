using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RogoDigital.Lipsync.AutoSync;

namespace RogoDigital.Lipsync.AutoSync
{
	[CustomEditor(typeof(ASPhonemeIntensityFromVolumeModule))]
	public class ASPhonemeIntensityFromVolumeModuleEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("applyCurveRelative"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("remapCurve"));
			serializedObject.ApplyModifiedProperties();
		}
	}
}