using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AnimatedValues;
using UnityEditor;
using RogoDigital.Lipsync;

[CustomEditor(typeof(BlendshapeManager))]
public class BlendshapeManagerEditor : Editor {

	private BlendshapeManager bmTarget;
	private List<SkinnedMeshRenderer> tempRenderers = new List<SkinnedMeshRenderer>();
	private SkinnedMeshRenderer currentRenderer;

	void OnEnable () {
		bmTarget = (BlendshapeManager)target;
	}

	public override void OnInspectorGUI () {
		serializedObject.Update();
		GUILayout.Space(10);
		if (bmTarget.blendSystem == null) {
			EditorGUILayout.HelpBox("No AdvancedBlendshapeBlendSystem is using this manager. You can safely remove it.", MessageType.Error);
		}
		EditorGUILayout.HelpBox("Add a number of SkinnedMeshRenderers below and press \"Build From Names\" to set up all available blend shapes. Any matching names across multiple meshes will be grouped together.", MessageType.Info);
		GUILayout.Space(10);
		EditorGUILayout.BeginHorizontal();
		currentRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(currentRenderer, typeof(SkinnedMeshRenderer), true);
		if(GUILayout.Button("Add")) {
			if (currentRenderer) {
				tempRenderers.Add(currentRenderer);
				currentRenderer = null;
			}
		}
		EditorGUILayout.EndHorizontal();
		if (tempRenderers.Count == 0) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("List Empty", EditorStyles.centeredGreyMiniLabel);
			EditorGUILayout.EndHorizontal();
		} else {
			for (int i = 0; i < tempRenderers.Count; i++) {
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label(tempRenderers[i].name, EditorStyles.boldLabel);
				GUILayout.Label(string.Format("  {0} Blend Shapes", tempRenderers[i].sharedMesh.blendShapeCount));
				if(GUILayout.Button("Remove", GUILayout.Width(120))) {
					tempRenderers.RemoveAt(i);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Build From Names", GUILayout.Width(300))) {
			Undo.RecordObject(bmTarget, "Build Blendshape List");
			Dictionary<string, int> entryLookup = new Dictionary<string, int>();

			List<BlendshapeManager.AdvancedBlendShape> abList = new List<BlendshapeManager.AdvancedBlendShape>();
			int currentArrayIndex = 0;

			for (int i = 0; i < tempRenderers.Count; i++) {
				for (int b = 0; b < tempRenderers[i].sharedMesh.blendShapeCount; b++) {
					string name = tempRenderers[i].sharedMesh.GetBlendShapeName(b);
					BlendshapeManager.BlendShapeMapping mapping = new BlendshapeManager.BlendShapeMapping();
					mapping.skinnedMeshRenderer = tempRenderers[i];
					mapping.blendShapeIndex = b;

					if (entryLookup.ContainsKey(name)) {
						var abs = abList[entryLookup[name]];
						BlendshapeManager.BlendShapeMapping[] newMappings = new BlendshapeManager.BlendShapeMapping[abs.mappings.Length + 1];

						for (int m = 0; m < abs.mappings.Length; m++) {
							newMappings[m] = abs.mappings[m];
						}
						newMappings[newMappings.Length - 1] = mapping;

						abs.mappings = newMappings;
						abList[entryLookup[name]] = abs;
					} else {
						entryLookup.Add(name, currentArrayIndex);

						var abs = new BlendshapeManager.AdvancedBlendShape();
						abs.name = name;
						abs.mappings = new BlendshapeManager.BlendShapeMapping[] { mapping };

						abList.Add(abs);
						currentArrayIndex++;
					}
				}
			}

			bmTarget.blendShapes = abList.ToArray();
			tempRenderers.Clear();
			if (bmTarget.blendSystem.onBlendablesChanged != null) bmTarget.blendSystem.onBlendablesChanged.Invoke();
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(20);
		GUILayout.Label("Raw List", EditorStyles.boldLabel);
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("blendShapes"), true);
		if(EditorGUI.EndChangeCheck()) {
			if (bmTarget.blendSystem.onBlendablesChanged != null) bmTarget.blendSystem.onBlendablesChanged.Invoke();
		}
		serializedObject.ApplyModifiedProperties();
	}

}
