using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RogoDigital.Lipsync;

[CustomEditor(typeof(BlendshapeBlendSystem))]
public class BlendshapeBlendSystemEditor : Editor
{
	private bool validRenderer = false;
	private bool initialValidation = false;

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();
		EditorGUI.BeginChangeCheck();
		SerializedProperty meshRendererProperty = serializedObject.FindProperty("characterMesh");
		EditorGUILayout.PropertyField(meshRendererProperty);
		if (EditorGUI.EndChangeCheck() || !initialValidation)
		{
			ValidateChoice(meshRendererProperty.objectReferenceValue);
		}

		if (!validRenderer)
		{
			EditorGUILayout.HelpBox("The referenced Skinned Mesh Renderer is part of a prefab. If this object is in the scene, you should reference the in-scene version instead.", MessageType.Warning);
		}

		EditorGUILayout.PropertyField(serializedObject.FindProperty("optionalOtherMeshes"), true);
		serializedObject.ApplyModifiedProperties();
	}

	private void ValidateChoice (Object renderer)
	{
		initialValidation = true;
		if (!renderer)
		{
			validRenderer = true;
			return;
		}

#if UNITY_2018_3_OR_NEWER
		if (PrefabUtility.IsPartOfAnyPrefab(renderer))
		{
			validRenderer = false;
		}
		else
		{
			validRenderer = true;
		}
#else
		PrefabType t = PrefabUtility.GetPrefabType(renderer);
		if (t == PrefabType.Prefab || t == PrefabType.ModelPrefab) {
			validRenderer = false;
		} else {
			validRenderer = true;
		}
#endif
	}
}
