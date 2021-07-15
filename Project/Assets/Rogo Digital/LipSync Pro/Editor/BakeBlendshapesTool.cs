#if UNITY_5_3_OR_NEWER

// Functionality only available in Unity 5.3 or newer, as the Mesh API didn't previously support adding blend shapes.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class BakeBlendshapesTool : EditorWindow {
	private Mesh baseMesh;
	private List<Blendshape> blendshapes;
	private ReorderableList blendshapeList;

	private class Blendshape {
		public Mesh mesh;
		public string name;

		public Blendshape (Mesh mesh, string name) {
			this.mesh = mesh;
			this.name = name;
		}

		public Blendshape () {
		}
	}

	void OnGUI () {
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginVertical();
		GUILayout.Space(20);

		EditorGUILayout.HelpBox("Choose the original base mesh that will form the neutral pose, and as many meshes to become blend shapes as you wish. No changes will be made to your original meshes.", MessageType.Info);
		EditorGUILayout.Space();
		baseMesh = (Mesh)EditorGUILayout.ObjectField("Base Mesh", baseMesh, typeof(Mesh), false);
		EditorGUILayout.Space();
		blendshapeList.DoLayoutList();

		GUILayout.FlexibleSpace();

		if (baseMesh != null && blendshapes.Count > 0) {
			if (GUILayout.Button("Create", GUILayout.Height(30))) {
				string path = EditorUtility.SaveFilePanelInProject("Save New Mesh", baseMesh.name + "_new", "asset", "Save the new mesh to your project.");
				if (!string.IsNullOrEmpty(path)) {
					// Create new mesh
					Mesh newMesh = new Mesh();
					CombineInstance c = new CombineInstance();
					c.mesh = baseMesh;

					newMesh.CombineMeshes(new CombineInstance[] { c }, true, false);

					// Add blend shapes
					foreach(Blendshape b in blendshapes) {
						newMesh.AddBlendShapeFrame(b.name, 100, GetVectorDeltas(baseMesh.vertices, b.mesh.vertices), GetVectorDeltas(baseMesh.normals, b.mesh.normals), GetVectorDeltas(baseMesh.tangents, b.mesh.tangents));
					}
					newMesh.UploadMeshData(false);

					// Save mesh
					AssetDatabase.CreateAsset(newMesh, path);
					Close();
				}
			}
		} else {
			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button("Create", GUILayout.Height(30));
			EditorGUI.EndDisabledGroup();
		}

		GUILayout.Space(10);
		GUILayout.EndVertical();
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
	}

	Vector3[] GetVectorDeltas (Vector3[] baseVectors, Vector3[] frameVectors) {
		if(baseVectors.Length != frameVectors.Length) {
			Debug.LogError("Cannot calculate deltas. Vector3 arrays have non-equal length: "+baseVectors.Length.ToString() + " and " + frameVectors.Length.ToString());
			return null;
		}

		Vector3[] deltas = new Vector3[baseVectors.Length]; 
		for (int i = 0; i < baseVectors.Length; i++) {
			deltas[i] = frameVectors[i] - baseVectors[i];
		}

		return deltas;
	}

	Vector3[] GetVectorDeltas (Vector4[] baseVectors, Vector4[] frameVectors) {
		if (baseVectors.Length != frameVectors.Length) {
			Debug.LogError("Cannot calculate deltas. Vector3 arrays have non-equal length.");
			return null;
		}

		Vector3[] deltas = new Vector3[baseVectors.Length];
		for (int i = 0; i < baseVectors.Length; i++) {
			deltas[i] = frameVectors[i] - baseVectors[i];
		}

		return deltas;
	}

	[MenuItem("Window/Rogo Digital/LipSync Pro/Create Mesh with Blend Shapes")]
	public static void ShowWindow () {
		BakeBlendshapesTool window = EditorWindow.GetWindow<BakeBlendshapesTool>(true);

		window.titleContent = new GUIContent("Blend Shape Baker");
		window.blendshapes = new List<Blendshape>();

		window.blendshapeList = new ReorderableList(window.blendshapes, typeof(Blendshape));
		window.blendshapeList.drawHeaderCallback = (Rect rect) => {
			EditorGUI.LabelField(rect, "New Blend Shapes");
		};

		window.blendshapeList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
			Blendshape element = (Blendshape)window.blendshapeList.list[index];
			rect.y += 1;
			rect.height -= 4;
			EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width * 0.15f, rect.height), "Name");
			element.name = EditorGUI.TextField(new Rect(rect.x + rect.width * 0.15f, rect.y, rect.width * 0.3f, rect.height), element.name);
			EditorGUI.LabelField(new Rect(rect.x + rect.width * 0.5f, rect.y, rect.width * 0.2f, rect.height), "Mesh");
			element.mesh = (Mesh)EditorGUI.ObjectField(new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.3f, rect.height), element.mesh, typeof(Mesh), false);
		};
	}
}
#endif