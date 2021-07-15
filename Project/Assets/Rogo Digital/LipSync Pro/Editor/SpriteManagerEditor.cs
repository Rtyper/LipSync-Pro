using UnityEngine;
using UnityEditor;
using RogoDigital.Lipsync;

[CustomEditor(typeof(SpriteManager))]
public class SpriteManagerEditor : Editor {

	private SpriteManager smTarget;

	private bool groupToggle = false;
	private bool spritesToggle = false;
	private bool[] groupToggles;
	private bool[] spriteToggles;

	void OnEnable () {
		smTarget = (SpriteManager)target;
		groupToggles = new bool[smTarget.groups.Count];
	}

	public override void OnInspectorGUI () {
		EditorGUI.indentLevel++;
		GUILayout.Space(10);
		if(smTarget.blendSystem == null) {
			EditorGUILayout.HelpBox("No SpriteBlendSystem is using this manager. You can safely remove it.", MessageType.Error);
		}
		EditorGUILayout.HelpBox("Use this component to manage Sprites and Layers for use with the SpriteBlendSystem.", MessageType.Info);
		GUILayout.Space(10);
		if (groupToggle = EditorGUILayout.Foldout(groupToggle, "Layers")) {
			EditorGUI.indentLevel++;
			for (int a = 0; a < smTarget.groups.Count; a++) {
				if (groupToggles[a] = EditorGUILayout.Foldout(groupToggles[a], smTarget.groups[a].groupName)) {
					EditorGUI.BeginChangeCheck();
					smTarget.groups[a].groupName = EditorGUILayout.TextField("Layer Name", smTarget.groups[a].groupName);
					if(EditorGUI.EndChangeCheck() && smTarget.blendSystem.onBlendablesChanged != null) smTarget.blendSystem.onBlendablesChanged.Invoke();

					EditorGUILayout.BeginHorizontal();
					smTarget.groups[a].spriteRenderer = (SpriteRenderer)EditorGUILayout.ObjectField("Sprite Renderer", smTarget.groups[a].spriteRenderer, typeof(SpriteRenderer), true);
					if (GUILayout.Button("Create Renderer")) {
						GameObject go = new GameObject(smTarget.groups[a].groupName + " Renderer", typeof(SpriteRenderer));
						go.transform.SetParent(smTarget.transform);
						go.transform.position = Vector3.zero;
						smTarget.groups[a].spriteRenderer = go.GetComponent<SpriteRenderer>();
					}
					EditorGUILayout.EndHorizontal();
					smTarget.groups[a].defaultSprite = (Sprite)EditorGUILayout.ObjectField("Default Sprite", smTarget.groups[a].defaultSprite, typeof(Sprite), false);
					GUILayout.Space(10);
					if (GUILayout.Button("Delete Layer")) {
						DestroyImmediate(smTarget.groups[a].spriteRenderer.gameObject);
						smTarget.groups.RemoveAt(a);
						groupToggles = new bool[smTarget.groups.Count];
						if (smTarget.blendSystem.onBlendablesChanged != null) smTarget.blendSystem.onBlendablesChanged.Invoke();
						EditorUtility.SetDirty(smTarget);
						break;
					}
				}
				GUILayout.Space(5);
			}
			if (GUILayout.Button("Add Layer", GUILayout.MaxWidth(300))) {
				smTarget.groups.Add(new SpriteManager.SpriteGroup("New Sprite Layer"));
				groupToggles = new bool[smTarget.groups.Count];
				groupToggles[groupToggles.Length - 1] = true;
				if (smTarget.blendSystem.onBlendablesChanged != null) smTarget.blendSystem.onBlendablesChanged.Invoke();
				EditorUtility.SetDirty(smTarget);
			}

			EditorGUI.indentLevel--;
		}

		if (spritesToggle = EditorGUILayout.Foldout(spritesToggle, "Sprites")) {
			EditorGUI.indentLevel++;
			for (int a = 0; a < smTarget.availableSprites.Count; a++) {
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginChangeCheck();
				smTarget.availableSprites[a] = (Sprite)EditorGUILayout.ObjectField(smTarget.availableSprites[a], typeof(Sprite), false);
				if (EditorGUI.EndChangeCheck() && smTarget.blendSystem.onBlendablesChanged != null) smTarget.blendSystem.onBlendablesChanged.Invoke();
				if (GUILayout.Button("Remove Sprite")) {
					smTarget.availableSprites.RemoveAt(a);
					if(smTarget.blendSystem.onBlendablesChanged != null) smTarget.blendSystem.onBlendablesChanged.Invoke();
					EditorUtility.SetDirty(smTarget);
					break;
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(5);
			}
			if (GUILayout.Button("Add Sprite", GUILayout.MaxWidth(300))) {
				smTarget.availableSprites.Add(null);
				if (smTarget.blendSystem.onBlendablesChanged != null) smTarget.blendSystem.onBlendablesChanged.Invoke();
				EditorUtility.SetDirty(smTarget);
			}

			EditorGUI.indentLevel--;
		}

	}
}
