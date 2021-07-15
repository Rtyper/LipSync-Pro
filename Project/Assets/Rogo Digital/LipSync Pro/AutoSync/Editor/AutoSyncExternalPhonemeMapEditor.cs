using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace RogoDigital.Lipsync.AutoSync
{
	[CustomEditor(typeof(AutoSyncExternalPhonemeMap))]
	public class AutoSyncExternalPhonemeMapEditor : Editor
	{
		private ReorderableList mapList;

		private void OnEnable ()
		{
			mapList = new ReorderableList(serializedObject, serializedObject.FindProperty("phonemeMap").FindPropertyRelative("map"));
			mapList.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Phoneme Map");
			};

			mapList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				SerializedProperty element = mapList.serializedProperty.GetArrayElementAtIndex(index);
				rect.y += 1;
				rect.height -= 4;
				EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.15f, rect.height), "A Label");
				EditorGUI.PropertyField(new Rect(rect.x + rect.width * 0.15f, rect.y, rect.width * 0.3f, rect.height), element.FindPropertyRelative("aLabel"), GUIContent.none);
				EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.2f, rect.height), "B Label");
				EditorGUI.PropertyField(new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.3f, rect.height), element.FindPropertyRelative("bLabel"), GUIContent.none);
			};
		}

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Phoneme Set A");
			GUILayout.Label("Phoneme Set B");
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("setAName"), GUIContent.none);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("setBName"), GUIContent.none);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			mapList.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}