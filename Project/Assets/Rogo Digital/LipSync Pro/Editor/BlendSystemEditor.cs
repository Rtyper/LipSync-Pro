using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using RogoDigital;
using RogoDigital.Lipsync;

[CustomEditor(typeof(BlendSystem), true)]
public class BlendSystemEditor : Editor
{

	private SerializedObject serializedTarget;
	private SerializedProperty[] properties;
	private BlendSystem myTarget;

	private string sharedMessage;

	private static Dictionary<BlendSystem, BlendSystemButton.Reference[]> blendSystemButtons = new Dictionary<BlendSystem, BlendSystemButton.Reference[]>();
	private static Dictionary<BlendSystemUser, Editor> blendSystemEditors = new Dictionary<BlendSystemUser, Editor>();

	private static List<Type> blendSystems;
	private static List<string> blendSystemNames;

	void Init ()
	{
		Type sysType = target.GetType();
		MemberInfo[] propInfo = sysType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

		myTarget = (BlendSystem)target;
		serializedTarget = new SerializedObject(myTarget);

		List<SerializedProperty> propertiesList = new List<SerializedProperty>();

		for (int a = 0; a < propInfo.Length; a++)
		{
			SerializedProperty property = serializedTarget.FindProperty(propInfo[a].Name);
			if (property != null)
				propertiesList.Add(property);
		}

		properties = propertiesList.ToArray();

		sharedMessage = "The following components are using this BlendSystem:\n\n";
		for (int u = 0; u < myTarget.users.Length; u++)
		{
			sharedMessage += "• " + myTarget.users[u].GetType().Name + "\n";
		}
		sharedMessage += "\nThe BlendSystem and its settings will be shared.";
	}

	public override void OnInspectorGUI ()
	{
		BlendSystem bsTarget = (BlendSystem)target;

		if (properties == null)
		{
			Init();
		}

		if (serializedTarget != null && target != null)
		{
			serializedTarget.Update();

			if (bsTarget.users != null)
			{
				if (bsTarget.users.Length > 1)
				{
					EditorGUILayout.HelpBox(sharedMessage, MessageType.Info);
				}
			}

			EditorGUI.BeginChangeCheck();
			foreach (SerializedProperty property in properties)
			{
				if (property != null)
				{
					EditorGUILayout.PropertyField(property, true);
				}
			}
			if (EditorGUI.EndChangeCheck())
			{
				myTarget.SendMessage("OnVariableChanged", SendMessageOptions.DontRequireReceiver);
			}
			serializedTarget.ApplyModifiedProperties();
		}

	}

	public static void DrawBlendSystemButtons (BlendSystem blendSystem)
	{
		if (blendSystemButtons.ContainsKey(blendSystem))
		{
			if (blendSystemButtons[blendSystem].Length > 0 && blendSystemButtons[blendSystem].Length < 3)
			{
				Rect buttonPanel = EditorGUILayout.BeginHorizontal();
				EditorGUI.HelpBox(new Rect(buttonPanel.x, buttonPanel.y - 4, buttonPanel.width, buttonPanel.height + 8), "BlendSystem Commands:", MessageType.Info);
				GUILayout.FlexibleSpace();
				foreach (BlendSystemButton.Reference button in blendSystemButtons[blendSystem])
				{
					if (GUILayout.Button(button.displayName, GUILayout.Height(20), GUILayout.MinWidth(120)))
					{
						button.method.Invoke(blendSystem, null);
					}
				}
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}
			else if (blendSystemButtons[blendSystem].Length >= 3)
			{
				Rect buttonPanel = EditorGUILayout.BeginHorizontal();
				EditorGUI.HelpBox(new Rect(buttonPanel.x, buttonPanel.y - 4, buttonPanel.width, buttonPanel.height + 8), "BlendSystem Commands:", MessageType.Info);
				GUILayout.FlexibleSpace();
				foreach (BlendSystemButton.Reference button in blendSystemButtons[blendSystem])
				{
					if (GUILayout.Button(button.displayName, GUILayout.Height(20), GUILayout.MinWidth(120)))
					{
						button.method.Invoke(blendSystem, null);
					}
				}
				GUILayout.Space(5);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}
		}
	}

	public static int DrawBlendSystemEditor (BlendSystemUser component, int blendSystemNumber, string notReadyMessage)
	{
		int number = blendSystemNumber;

		if (blendSystems == null)
		{
			number = FindBlendSystems(component);
		}

		if (blendSystems.Count == 0)
		{
			EditorGUILayout.Popup("Blend System", 0, new string[] { "No BlendSystems Found" });
		}
		else
		{
			if (component.blendSystem == null)
			{
				EditorGUI.BeginChangeCheck();
				number = EditorGUILayout.Popup("Blend System", number, blendSystemNames.ToArray(), GUIStyle.none);
				if (EditorGUI.EndChangeCheck())
				{
					if (Event.current.type != EventType.Layout)
						ChangeBlendSystem(component, number);
				}
				GUI.Box(new Rect(EditorGUIUtility.labelWidth + GUILayoutUtility.GetLastRect().x, GUILayoutUtility.GetLastRect().y, GUILayoutUtility.GetLastRect().width, GUILayoutUtility.GetLastRect().height), "Select a BlendSystem", EditorStyles.popup);
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				number = EditorGUILayout.Popup("Blend System", number, blendSystemNames.ToArray());
				if (EditorGUI.EndChangeCheck())
				{
					if (Event.current.type != EventType.Layout)
						ChangeBlendSystem(component, number);
				}
			}
		}
		if (component.blendSystem == null)
		{
			GUILayout.Label("No BlendSystem Selected");
			EditorGUILayout.HelpBox(notReadyMessage, MessageType.Info);
		}

		EditorGUILayout.Space();
		if (component.blendSystem != null)
		{

			if (!blendSystemEditors.ContainsKey(component))
			{
				CreateBlendSystemEditor(component);
			}

			// Recreate this editor if the target has been lost
			if (blendSystemEditors[component].target == null && Event.current.type == EventType.Repaint)
			{
				if (blendSystemEditors.ContainsKey(component))
				{
					Editor e = blendSystemEditors[component];
					blendSystemEditors.Remove(component);
					DestroyImmediate(e);
				}
			}
			else
			{
				blendSystemEditors[component].OnInspectorGUI();
			}

			if (!component.blendSystem.isReady)
			{
				GUILayout.Space(10);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Continue", GUILayout.MaxWidth(200)))
				{
					component.blendSystem.OnVariableChanged();
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(10);
			}

		}

		return number;
	}

	public static void ChangeBlendSystem (BlendSystemUser component, int blendSystem)
	{
		Undo.RecordObject(component, "Change Blend System");

		// Unregister from existing system if one exists
		if (component.blendSystem != null)
		{
			// Remove existing editor if one exists
			if (blendSystemEditors.ContainsKey(component))
			{
				DestroyImmediate(blendSystemEditors[component]);
				blendSystemEditors.Remove(component);
			}

			component.blendSystem.Unregister(component);
		}

		// Only attempt to create new system if a new system was actually chosen
		if (blendSystem != 0)
		{
			// Attempt to find existing instance of new system
			BlendSystem system = (BlendSystem)component.GetComponent(blendSystems[blendSystem]);

			if (system == null)
			{
				// Blend System doesn't exist - must be created first
				system = (BlendSystem)Undo.AddComponent(component.gameObject, blendSystems[blendSystem]);
			}

			// Register with the new system
			system.Register(component);
		}

		CreateBlendSystemEditor(component);
	}

	static void CreateBlendSystemEditor (BlendSystemUser component)
	{
		if (component.blendSystem != null)
		{
			if (!blendSystemEditors.ContainsKey(component))
			{
				blendSystemEditors.Add(component, CreateEditor(component.blendSystem));
				blendSystemEditors[component].Repaint();
			}
		}
	}

	public static int FindBlendSystems (BlendSystemUser component)
	{
		blendSystems = new List<Type>();
		blendSystemNames = new List<string>();

		blendSystems.Add(null);
		blendSystemNames.Add("None");

		foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (Type t in a.GetTypes())
			{
				if (t.IsSubclassOf(typeof(BlendSystem)))
				{
					blendSystems.Add(t);
					blendSystemNames.Add(LipSyncEditorExtensions.AddSpaces(t.Name));
				}
			}
		}

		if (component.blendSystem != null)
		{
			return blendSystems.IndexOf(component.blendSystem.GetType());
		}

		return 0;
	}

	public static void GetBlendSystemButtons (BlendSystem blendSystem)
	{
		if (blendSystemButtons.ContainsKey(blendSystem))
			blendSystemButtons.Remove(blendSystem);

		MethodInfo[] methods = blendSystem.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
		BlendSystemButton.Reference[] buttons = new BlendSystemButton.Reference[0];

		int buttonLength = 0;
		for (int m = 0; m < methods.Length; m++)
		{
			BlendSystemButton[] button = (BlendSystemButton[])methods[m].GetCustomAttributes(typeof(BlendSystemButton), false);
			if (button.Length > 0)
			{
				buttonLength++;
			}
		}

		if (buttonLength > 0)
		{
			buttons = new BlendSystemButton.Reference[buttonLength];
			int b = 0;
			for (int m = 0; m < methods.Length; m++)
			{
				BlendSystemButton[] button = (BlendSystemButton[])methods[m].GetCustomAttributes(typeof(BlendSystemButton), false);
				if (button.Length > 0)
				{
					buttons[b] = new BlendSystemButton.Reference(button[0].displayName, methods[m]);
					b++;
				}
			}
		}

		blendSystemButtons.Add(blendSystem, buttons);
	}

	[MenuItem("Assets/Create/LipSync Pro/Empty BlendSystem")]
	public static void CreateNewBlendSystem ()
	{
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);

		if (path == "")
		{
			path = "Assets";
		}
		else if (Path.GetExtension(path) != "")
		{
			path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
		}
		else
		{
			path += "/";
		}

		string[] guids = AssetDatabase.FindAssets("NewBlendSystemTemplate t:TextAsset");
		string textpath = "";

		if (guids.Length > 0)
		{
			textpath = AssetDatabase.GUIDToAssetPath(guids[0]);
		}

		StreamWriter writer = File.CreateText(Path.GetFullPath(path) + "MyNewBlendSystem.cs");
		StreamReader reader = File.OpenText(Path.GetFullPath(textpath));

		string line;
		while ((line = reader.ReadLine()) != null)
		{
			writer.WriteLine(line);
		}

		writer.Close();
		reader.Close();

		AssetDatabase.Refresh();
		Selection.activeObject = AssetDatabase.LoadAssetAtPath(path + "MyNewBlendSystem.cs", typeof(object));
	}
}
