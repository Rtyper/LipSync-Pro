using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace RogoDigital.Lipsync.AutoSync
{
	[CustomEditor(typeof(ASPocketSphinxLanguageModel))]
	public class ASPocketSphinxLanguageModelEditor : Editor
	{

		private SerializedProperty language;
		private SerializedProperty sourcePhoneticAlphabetName;
		private SerializedProperty hmmDir;
		private SerializedProperty dictFile;
		private SerializedProperty allphoneFile;
		private SerializedProperty lmFile;
		private SerializedProperty mappingMode;
		private SerializedProperty externalMap;
		private SerializedProperty phonemeSet;
		private ReorderableList phonemeMapper;

		void OnEnable ()
		{
			language = serializedObject.FindProperty("language");
			sourcePhoneticAlphabetName = serializedObject.FindProperty("sourcePhoneticAlphabetName");
			hmmDir = serializedObject.FindProperty("hmmDir");
			dictFile = serializedObject.FindProperty("dictFile");
			allphoneFile = serializedObject.FindProperty("allphoneFile");
			lmFile = serializedObject.FindProperty("lmFile");
			mappingMode = serializedObject.FindProperty("mappingMode");
			phonemeSet = serializedObject.FindProperty("recommendedPhonemeSet");
			externalMap = serializedObject.FindProperty("externalMap");

			phonemeMapper = new ReorderableList(serializedObject, serializedObject.FindProperty("phonemeMap").FindPropertyRelative("map"));
			phonemeMapper.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, "Phoneme Mapper");
			};

			phonemeMapper.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				SerializedProperty element = phonemeMapper.serializedProperty.GetArrayElementAtIndex(index);
				rect.y += 1;
				rect.height -= 4;
				EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.15f, rect.height), "Label");
				EditorGUI.PropertyField(new Rect(rect.x + rect.width * 0.15f, rect.y, rect.width * 0.3f, rect.height), element.FindPropertyRelative("aLabel"), GUIContent.none);
				EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.2f, rect.height), "Phoneme");
				EditorGUI.PropertyField(new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.3f, rect.height), element.FindPropertyRelative("bLabel"), GUIContent.none);
			};
		}

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();
			var typedTarget = (ASPocketSphinxLanguageModel)target;

			EditorGUILayout.PropertyField(language);
			EditorGUILayout.PropertyField(sourcePhoneticAlphabetName);

			GUILayout.Space(20);
			GUILayout.Label("Paths", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(hmmDir);
			EditorGUILayout.PropertyField(dictFile);
			EditorGUILayout.PropertyField(allphoneFile);
			EditorGUILayout.PropertyField(lmFile);
			GUILayout.Space(20);
			GUILayout.Label("Mapping", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(mappingMode);
			if (typedTarget.mappingMode == AutoSyncPhonemeMap.MappingMode.AutoDetect)
			{
				EditorGUILayout.HelpBox("'AutoDetect' - Automatically finds an appropriate external phoneme map based on current PhonemeSet when used.", MessageType.Info);
			}
			else if (typedTarget.mappingMode == AutoSyncPhonemeMap.MappingMode.ExternalMap)
			{
				EditorGUILayout.HelpBox("'ExternalMap' - You must specify an External Phoneme Map.", MessageType.Info);
				EditorGUILayout.PropertyField(externalMap);
			}
			else
			{
				EditorGUILayout.HelpBox("'InternalMap' - Allows the defining of a Phoneme Map specific to this Language Model. Mainly provided to simplify upgrading from older versions.", MessageType.Warning);
				EditorGUILayout.PropertyField(phonemeSet);
				phonemeMapper.DoLayoutList();
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}