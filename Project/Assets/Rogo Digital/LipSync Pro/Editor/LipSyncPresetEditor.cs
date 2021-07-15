using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RogoDigital.Lipsync;

[CustomEditor(typeof(LipSyncPreset))]
public class LipSyncPresetEditor : Editor
{
	private new LipSyncPreset target;

	public void OnEnable()
	{
		target = (LipSyncPreset)base.target;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		GUILayout.Space(10);
		GUILayout.Box("Settings", EditorStyles.boldLabel);
		GUILayout.Space(5);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("displayPath"), new GUIContent("Display Name", "Name used to display this preset in lists. Can contain '/' characters to organise into folders."));
		GUILayout.Space(10);
		GUILayout.Box("Preset Contents", EditorStyles.boldLabel);
		GUILayout.Space(5);
		GUILayout.Box("Phonemes", EditorStyles.miniBoldLabel);
		for (int i = 0; i < target.phonemeShapes.Length; i++)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(10);
			GUILayout.Box(target.phonemeShapes[i].phonemeName, EditorStyles.miniLabel);
			GUILayout.Space(20);
			GUILayout.Box(target.phonemeShapes[i].blendables.Length.ToString() + " Blendables", EditorStyles.miniLabel);
			GUILayout.Space(10);
			GUILayout.Box(target.phonemeShapes[i].bones.Length.ToString() + " Transforms", EditorStyles.miniLabel);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}
		GUILayout.Box("Emotions", EditorStyles.miniBoldLabel);
		for (int i = 0; i < target.emotionShapes.Length; i++)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(10);
			GUILayout.Box(target.emotionShapes[i].emotion, EditorStyles.miniLabel);
			GUILayout.Space(20);
			GUILayout.Box(target.emotionShapes[i].blendables.Length.ToString() +" Blendables", EditorStyles.miniLabel);
			GUILayout.Space(10);
			GUILayout.Box(target.emotionShapes[i].bones.Length.ToString() + " Transforms", EditorStyles.miniLabel);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		serializedObject.ApplyModifiedProperties();
	}
}
