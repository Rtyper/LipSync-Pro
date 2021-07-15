using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using RogoDigital.Lipsync;

[CustomEditor(typeof(PhonemeSet))]
public class PhonemeSetEditor : Editor
{
	private PhonemeSet typedTarget;

	private ReorderableList legacyPhonemeList;
	private ReorderableList phonemeList;

	private Texture2D errorIcon;

	void OnEnable()
	{
		typedTarget = (PhonemeSet)target;

		legacyPhonemeList = new ReorderableList(serializedObject, serializedObject.FindProperty("phonemes").FindPropertyRelative("phonemeNames"), true, true, true, true);
		legacyPhonemeList.drawHeaderCallback += (Rect rect) =>
		{
			GUI.Label(rect, "Phonemes");
		};
		legacyPhonemeList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
		{
			serializedObject.Update();
			rect = new Rect(rect.x, rect.y + 1, rect.width, rect.height - 4);
			SerializedProperty element = legacyPhonemeList.serializedProperty.GetArrayElementAtIndex(index);
			GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.2f, rect.height), index.ToString() + " (" + Mathf.Pow(2, index) + ")");
			element.stringValue = GUI.TextField(new Rect(rect.x + (rect.width * 0.2f), rect.y, rect.width * 0.8f, rect.height), element.stringValue);
			serializedObject.ApplyModifiedProperties();
		};

		phonemeList = new ReorderableList(serializedObject, serializedObject.FindProperty("phonemeList"), true, true, true, true);
		phonemeList.drawHeaderCallback += (Rect rect) =>
		{
			GUI.Label(rect, "Phonemes");
		};
		phonemeList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
		{
			serializedObject.Update();
			var element = phonemeList.serializedProperty.GetArrayElementAtIndex(index);
			var pName = element.FindPropertyRelative("name");
			var pNumber = element.FindPropertyRelative("number");
			var pFlag = element.FindPropertyRelative("flag");
			var pVisuallyImportant = element.FindPropertyRelative("visuallyImportant");
			var pGuideImage = element.FindPropertyRelative("guideImage");

			rect = new Rect(rect.x, rect.y + 1, rect.width, rect.height - 4);

			GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.2f, rect.height), pNumber.intValue.ToString() + " (" + pFlag.intValue + ")");
			pName.stringValue = GUI.TextField(new Rect(rect.x + (rect.width * 0.2f), rect.y, rect.width * 0.4f, rect.height), pName.stringValue);
			pGuideImage.objectReferenceValue = EditorGUI.ObjectField(new Rect(rect.x + (rect.width * 0.65f), rect.y, 25, rect.height), pGuideImage.objectReferenceValue, typeof(Texture2D), false);
			pVisuallyImportant.boolValue = GUI.Toggle(new Rect(rect.x + (rect.width * 0.8f), rect.y, rect.width * 0.2f, rect.height), pVisuallyImportant.boolValue, " Is Important");
			serializedObject.ApplyModifiedProperties();
		};
		phonemeList.onReorderCallback += (ReorderableList list) =>
		{
			serializedObject.Update();
			for (int i = 0; i < list.count; i++)
			{
				var item = list.serializedProperty.GetArrayElementAtIndex(i);

				item.FindPropertyRelative("number").intValue = i;
				item.FindPropertyRelative("flag").intValue = Mathf.RoundToInt(Mathf.Pow(2, i));
			}
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		};

		errorIcon = EditorGUIUtility.FindTexture("console.erroricon.sml");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		GUILayout.Space(10);
		if (typedTarget.isLegacyFormat)
		{
			Rect box = EditorGUILayout.BeginVertical();
			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();
			GUI.Box(box, "", EditorStyles.helpBox);
			GUILayout.Space(10);
			GUILayout.Box(errorIcon, GUIStyle.none);
			GUILayout.Label("This PhonemeSet is using the legacy format. This must be updated in order to continue using it.", EditorStyles.wordWrappedMiniLabel);
			GUILayout.Space(10);
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(10);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Update", GUILayout.Width(100)))
			{
				Undo.RecordObject(typedTarget, "Update PhonemeSet Format");
				typedTarget.UpdateFormat();
				PrefabUtility.RecordPrefabInstancePropertyModifications(typedTarget);
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(10);
			EditorGUILayout.EndVertical();
			GUILayout.Space(10);
			legacyPhonemeList.DoLayoutList();
			GUILayout.Space(10);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("scriptingName"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("guideImages"), true);
		}
		else
		{
			phonemeList.DoLayoutList();
			GUILayout.Space(10);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("scriptingName"));
		}

		serializedObject.ApplyModifiedProperties();
	}

	[MenuItem("Window/Rogo Digital/LipSync Pro/Update PhonemeSets")]
	public static void UpdatePhonemeSets()
	{
		var sets = AssetDatabase.FindAssets("t:PhonemeSet");
		for (int i = 0; i < sets.Length; i++)
		{
			var path = AssetDatabase.GUIDToAssetPath(sets[i]);
			var set = AssetDatabase.LoadAssetAtPath<PhonemeSet>(path);

			Undo.RecordObject(set, "Update PhonemeSet");
			set.UpdateFormat();
			PrefabUtility.RecordPrefabInstancePropertyModifications(set);
		}
		EditorUtility.DisplayDialog("Update Complete", string.Format("Updated {0} PhonemeSets in the project.", sets.Length), "OK");
	}
}
