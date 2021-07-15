using System.Text;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

using RogoDigital.Lipsync;

namespace RogoDigital.Lipsync
{
	public static class LipSyncEditorExtensions
	{
		public static int currentToggle = -1;
		public static Object currentTarget;
		public static int selectedBone = 0;
		public static int oldToggle = -1;

		static int currentSearchBlendable = -1;
		static string searchString;

		static Dictionary<Object, AnimBool> showBoneOptions = new Dictionary<Object, AnimBool>();

		static Texture2D locked;
		static Texture2D unlocked;
		static Texture2D search;
		static Texture2D list;
		static Texture2D delete;
		static Texture2D lightToolbarTexture;
		static Texture2D markerLine;
		static Texture2D indicator;

		static GUIStyle lightToolbar;
		static GUIStyle miniLabelDark;

		static GUIStyle whiteMiniLabelBold;

		static LipSyncEditorExtensions()
		{
			locked = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Lipsync/Dark/locked.png");
			unlocked = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Lipsync/Dark/unlocked.png");
			search = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/search.png");
			list = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/list.png");
			delete = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/bin.png");
			lightToolbarTexture = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Lipsync/light-toolbar.png");
			markerLine = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/white.png");
			indicator = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/indicator.png");

			if (!EditorGUIUtility.isProSkin)
			{
				locked = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Lipsync/Light/locked.png");
				unlocked = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Lipsync/Light/unlocked.png");
				search = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/search.png");
				list = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/list.png");
			}
		}

		public static LipSyncProject GetProjectFile()
		{
			//Get Settings File
			string[] guids = AssetDatabase.FindAssets("ProjectSettings t:LipSyncProject");

			if (guids.Length > 0)
			{
				var spath = AssetDatabase.GUIDToAssetPath(guids[0]);

				if (guids.Length > 1)
					Debug.LogWarning("LipSync: Multiple LipSyncProject files found. Please ensure there is only one in your project to avoid ambiguity.");

				return (LipSyncProject)AssetDatabase.LoadAssetAtPath(spath, typeof(LipSyncProject));
			}
			else
			{
				LipSyncProject newSettings = ScriptableObject.CreateInstance<LipSyncProject>();
				newSettings.emotions = new string[]
				{
					"Happy",
					"Serious",
					"Eyebrows Up",
					"Sad"
				};

				newSettings.emotionColors = new Color[]
				{
					new Color(1f, 0.682352941f, 0f),
					new Color(0.317647059f, 0.317647059f, 0.317647059f),
					new Color(0.741176471f, 1, 0.396078431f),
					new Color(0f, 0.545098039f, 1f),
				};

				AssetDatabase.CreateAsset(newSettings, "Assets/Rogo Digital/LipSync Pro/ProjectSettings.asset");
				AssetDatabase.Refresh();

				return newSettings;
			}
		}

		public static bool DrawShapeEditor(this Editor target, BlendSystem blendSystem, string[] blendables, bool useBones, bool allowCreationFromAnimClip, Shape shape, string name, int id, string invalidError = "")
		{
			bool markedForDeletion = false;

			// Add names if missing
			if (shape.blendableNames == null || shape.blendableNames.Count < shape.blendShapes.Count)
			{
				shape.blendableNames = new List<string>();
				for (int i = 0; i < shape.blendShapes.Count; i++)
				{
					shape.blendableNames.Add(blendables[shape.blendShapes[i]]);
				}
			}

			// Create AnimBool if not defined
			if (!showBoneOptions.ContainsKey(target))
			{
				showBoneOptions.Add(target, new AnimBool(useBones, target.Repaint));
			}
			showBoneOptions[target].target = useBones;

			// Create styles if not defined
			if (lightToolbar == null)
			{
				lightToolbar = new GUIStyle(EditorStyles.toolbarDropDown);
				lightToolbar.normal.background = lightToolbarTexture;

				miniLabelDark = new GUIStyle(EditorStyles.miniLabel);
				miniLabelDark.normal.textColor = Color.black;
			}

			if (currentToggle == id && currentTarget == target.target)
			{
				Undo.RecordObject(target.target, "Change " + name + " Pose");
				Rect box = EditorGUILayout.BeginHorizontal(GUILayout.Height(17));

				if (shape.verified)
				{
					GUI.backgroundColor = new Color(1f, 0.77f, 0f);
				}
				else
				{
					GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
				}

				if (GUI.Button(box, "", lightToolbar))
				{
					currentSearchBlendable = -1;
					searchString = "";
					currentToggle = -1;
				}
				GUI.backgroundColor = Color.white;

				GUILayout.Box(name, miniLabelDark, GUILayout.Width(250));
				if (shape.weights.Count == 1)
				{
					GUILayout.Box("1 " + blendSystem.blendableDisplayName, miniLabelDark);
				}
				else if (shape.weights.Count > 1)
				{
					GUILayout.Box(shape.weights.Count.ToString() + " " + blendSystem.blendableDisplayNamePlural, miniLabelDark);
				}

				if (shape.bones.Count == 1 && useBones)
				{
					GUILayout.Box("1 Bone Transform", miniLabelDark);
				}
				else if (shape.bones.Count > 1 && useBones)
				{
					GUILayout.Box(shape.bones.Count.ToString() + " Bone Transforms", miniLabelDark);
				}
				if (!shape.verified)
				{
					GUILayout.FlexibleSpace();
					GUILayout.Box("Missing", miniLabelDark);
					GUILayout.FlexibleSpace();
				}

				EditorGUILayout.EndHorizontal();
				GUILayout.Space(8);
				if (!shape.verified)
				{
					if (invalidError != "")
					{
						EditorGUILayout.HelpBox(invalidError, MessageType.Warning);
					}
					else
					{
						EditorGUILayout.HelpBox("There is no matching " + shape.GetType().Name + " in the project settings. It will still function correctly, but it is advised you add it to the project settings for compatibility. Alternatively, you can delete it from here.", MessageType.Warning);
					}
					if (GUILayout.Button("Delete Pose"))
					{
						markedForDeletion = true;
					}
				}

				box = EditorGUILayout.BeginVertical();
				GUI.Box(new Rect(box.x + 4, box.y, box.width - 7, box.height), "", EditorStyles.helpBox);
				GUILayout.Space(20);
				for (int b = 0; b < shape.weights.Count; b++)
				{
					Rect newBox = EditorGUILayout.BeginHorizontal();
					GUI.Box(new Rect(newBox.x + 5, newBox.y, newBox.width - 11, newBox.height), "", EditorStyles.toolbar);
					GUILayout.Space(5);

					// Check if blendables have become out-of-sync
					// (blend system started reporting different blendables)
					if (blendables[shape.blendShapes[b]] != shape.blendableNames[b] && blendSystem.allowResyncing)
					{
						bool matched = false;
						for (int i = 0; i < blendables.Length; i++)
						{
							if (blendables[i] == shape.blendableNames[b])
							{
								shape.blendShapes[b] = i;
								matched = true;
							}
							else if (blendables[i].Remove(blendables[i].Length - 5).Contains(shape.blendableNames[b].Remove(shape.blendableNames[b].Length - 5)))
							{
								shape.blendShapes[b] = i;
								shape.blendableNames[b] = blendables[i];
								matched = true;
							}
						}
						if (!matched)
							shape.blendableNames[b] = blendables[shape.blendShapes[b]];
					}

					int oldShape = shape.blendShapes[b];

					if (currentSearchBlendable == b)
					{
						searchString = EditorGUILayout.TextField("Search", searchString, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));
						if (GUILayout.Button("Search", EditorStyles.toolbarButton, GUILayout.MaxWidth(45)))
						{
							GenericMenu menu = new GenericMenu();
							for (int i = 0; i < blendables.Length; i++)
							{
								if (blendables[i].ToLowerInvariant().Contains(searchString.ToLowerInvariant()))
								{
									int b2 = b;
									int i2 = i;
									menu.AddItem(new GUIContent(blendables[i2]), false, () =>
									{
										if (shape.blendShapes[b2] != i2)
										{
											oldShape = shape.blendShapes[b2];
											blendSystem.OnBlendableRemovedFromPose(shape.blendShapes[b2]);
											shape.blendShapes[b2] = i2;
											blendSystem.OnBlendableAddedToPose(i2);

											shape.blendableNames[b2] = blendables[b2];
											blendSystem.SetBlendableValue(oldShape, 0);
											blendSystem.SetBlendableValue(i2, shape.weights[b2]);
										}

										currentSearchBlendable = -1;
										searchString = "";
									});
								}
							}
							menu.ShowAsContext();
						}
					}
					else
					{
						int oldBlendable = shape.blendShapes[b];
						shape.blendShapes[b] = EditorGUILayout.Popup(blendSystem.blendableDisplayName + " " + b.ToString(), shape.blendShapes[b], blendables, EditorStyles.toolbarPopup);
						if (oldBlendable != shape.blendShapes[b])
						{
							blendSystem.OnBlendableRemovedFromPose(oldBlendable);
							blendSystem.OnBlendableAddedToPose(shape.blendShapes[b]);
						}
					}

					if (shape.blendShapes[b] != oldShape)
					{
						shape.blendableNames[b] = blendables[b];
						blendSystem.SetBlendableValue(oldShape, 0);
					}

					if (GUILayout.Button(currentSearchBlendable == b ? list : search, EditorStyles.toolbarButton, GUILayout.MaxWidth(30)))
					{
						currentSearchBlendable = currentSearchBlendable == b ? -1 : b;
					}

					GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
					if (GUILayout.Button(delete, EditorStyles.toolbarButton, GUILayout.MaxWidth(50)))
					{
						Undo.RecordObject(target.target, "Delete " + blendSystem.blendableDisplayName);

						shape.blendShapes.RemoveAt(b);
						blendSystem.SetBlendableValue(oldShape, 0);
						blendSystem.OnBlendableRemovedFromPose(oldShape);
						selectedBone = 0;
						shape.weights.RemoveAt(b);
						shape.blendableNames.RemoveAt(b);
						EditorUtility.SetDirty(target.target);
						break;
					}

					GUILayout.Space(4);
					GUI.backgroundColor = Color.white;
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(15);
					shape.weights[b] = EditorGUILayout.Slider(shape.weights[b], blendSystem.blendRangeLow, blendSystem.blendRangeHigh);
					GUILayout.Space(10);
					EditorGUILayout.EndHorizontal();
					GUILayout.Space(10);
				}

				if (FixedBeginFadeGroup(showBoneOptions[target].faded))
				{
					for (int b = 0; b < shape.bones.Count; b++)
					{
						Rect newBox = EditorGUILayout.BeginHorizontal();
						GUI.Box(new Rect(newBox.x + 5, newBox.y, newBox.width - 11, newBox.height), "", EditorStyles.toolbar);
						GUILayout.Space(10);
						bool selected = EditorGUILayout.ToggleLeft(new GUIContent("Bone Transform " + b.ToString(), EditorGUIUtility.FindTexture("Transform Icon"), "Show Transform Handles"), selectedBone == b, GUILayout.Width(170));
						selectedBone = selected ? b : selectedBone;

						Transform oldBone = shape.bones[b].bone;
						shape.bones[b].bone = (Transform)EditorGUILayout.ObjectField("", shape.bones[b].bone, typeof(Transform), true);

						if (oldBone != shape.bones[b].bone)
						{
							if (shape.bones[b].bone != null)
							{
								Transform newbone = shape.bones[b].bone;
								shape.bones[b].bone = oldBone;
								if (shape.bones[b].bone != null)
								{
									shape.bones[b].bone.localPosition = shape.bones[b].neutralPosition;
									shape.bones[b].bone.localScale = shape.bones[b].neutralScale;
									shape.bones[b].bone.localEulerAngles = shape.bones[b].neutralRotation;
								}

								shape.bones[b].bone = newbone;

								shape.bones[b].SetNeutral();

								shape.bones[b].endRotation = shape.bones[b].bone.localEulerAngles;
								shape.bones[b].endPosition = shape.bones[b].bone.localPosition;
								shape.bones[b].endScale = shape.bones[b].bone.localScale;

								shape.bones[b].bone.localPosition = shape.bones[b].endPosition;
								shape.bones[b].bone.localEulerAngles = shape.bones[b].endRotation;
								shape.bones[b].bone.localScale = shape.bones[b].endScale;
							}
						}

						GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
						if (GUILayout.Button(delete, EditorStyles.toolbarButton, GUILayout.MaxWidth(50)))
						{
							Undo.RecordObject(target.target, "Delete Bone Transform");
							if (shape.bones[b].bone != null)
							{
								shape.bones[b].bone.localPosition = shape.bones[b].neutralPosition;
								shape.bones[b].bone.localEulerAngles = shape.bones[b].neutralRotation;
								shape.bones[b].bone.localScale = shape.bones[b].neutralScale;
							}
							shape.bones.RemoveAt(b);
							if (selectedBone >= shape.bones.Count)
								selectedBone -= 1;
							EditorUtility.SetDirty(target.target);
							break;
						}
						GUILayout.Space(4);
						GUI.backgroundColor = Color.white;
						EditorGUILayout.EndHorizontal();
						GUILayout.Space(5);
						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(10);
						GUILayout.Box("Position", EditorStyles.label, GUILayout.MaxWidth(80));

						EditorGUI.BeginDisabledGroup(shape.bones[b].bone == null);
						EditorGUI.BeginDisabledGroup(shape.bones[b].lockPosition);
						Vector3 newBonePosition = EditorGUILayout.Vector3Field("", shape.bones[b].endPosition);
						EditorGUI.EndDisabledGroup();
						GUILayout.Space(10);
						if (GUILayout.Button(shape.bones[b].lockPosition ? locked : unlocked, GUILayout.Width(30), GUILayout.Height(16)))
						{
							shape.bones[b].lockPosition = !shape.bones[b].lockPosition;
						}
						EditorGUI.EndDisabledGroup();

						if (shape.bones[b].bone != null)
						{
							if (newBonePosition != shape.bones[b].endPosition)
							{
								Undo.RecordObject(shape.bones[b].bone, "Move");
								shape.bones[b].endPosition = newBonePosition;
								shape.bones[b].bone.localPosition = shape.bones[b].endPosition;
							}
							else if (shape.bones[b].bone.localPosition != shape.bones[b].endPosition)
							{
								shape.bones[b].endPosition = shape.bones[b].bone.localPosition;
							}
						}

						GUILayout.Space(10);
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(10);
						GUILayout.Box("Rotation", EditorStyles.label, GUILayout.MaxWidth(80));

						EditorGUI.BeginDisabledGroup(shape.bones[b].bone == null);
						EditorGUI.BeginDisabledGroup(shape.bones[b].lockRotation);
						Vector3 newBoneRotation = EditorGUILayout.Vector3Field("", shape.bones[b].endRotation);
						EditorGUI.EndDisabledGroup();
						GUILayout.Space(10);
						if (GUILayout.Button(shape.bones[b].lockRotation ? locked : unlocked, GUILayout.Width(30), GUILayout.Height(16)))
						{
							shape.bones[b].lockRotation = !shape.bones[b].lockRotation;
						}
						EditorGUI.EndDisabledGroup();
						if (shape.bones[b].bone != null)
						{
							if (newBoneRotation != shape.bones[b].endRotation)
							{
								Undo.RecordObject(shape.bones[b].bone, "Rotate");
								shape.bones[b].endRotation = newBoneRotation;
								shape.bones[b].bone.localEulerAngles = shape.bones[b].endRotation;
							}
							else if (shape.bones[b].bone.localEulerAngles != shape.bones[b].endRotation)
							{
								shape.bones[b].endRotation = shape.bones[b].bone.localEulerAngles;
							}
						}

						GUILayout.Space(10);
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(10);
						GUILayout.Box("Scale", EditorStyles.label, GUILayout.MaxWidth(80));

						EditorGUI.BeginDisabledGroup(shape.bones[b].bone == null);
						EditorGUI.BeginDisabledGroup(shape.bones[b].lockScale);
						Vector3 newBoneScale = EditorGUILayout.Vector3Field("", shape.bones[b].endScale);
						EditorGUI.EndDisabledGroup();
						GUILayout.Space(10);
						if (GUILayout.Button(shape.bones[b].lockScale ? locked : unlocked, GUILayout.Width(30), GUILayout.Height(16)))
						{
							shape.bones[b].lockScale = !shape.bones[b].lockScale;
						}
						EditorGUI.EndDisabledGroup();
						if (shape.bones[b].bone != null)
						{
							if (newBonePosition != shape.bones[b].endScale)
							{
								Undo.RecordObject(shape.bones[b].bone, "Scale");
								shape.bones[b].endScale = newBoneScale;
								shape.bones[b].bone.localScale = shape.bones[b].endScale;
							}
							else if (shape.bones[b].bone.localScale != shape.bones[b].endScale)
							{
								shape.bones[b].endScale = shape.bones[b].bone.localScale;
							}
						}

						GUILayout.Space(10);
						EditorGUILayout.EndHorizontal();
						GUILayout.Space(10);
					}
				}
				FixedEndFadeGroup(showBoneOptions[target].faded);

				EditorGUILayout.Space();

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (blendSystem.blendableCount > 0)
				{
					if (GUILayout.Button("Add " + blendSystem.blendableDisplayName, GUILayout.MaxWidth(200)))
					{
						Undo.RecordObject(target.target, "Add " + blendSystem.blendableDisplayName);
						shape.blendShapes.Add(0);
						shape.weights.Add(0);
						shape.blendableNames.Add(blendables[0]);
						blendSystem.OnBlendableAddedToPose(0);
						EditorUtility.SetDirty(target.target);
					}
					if (useBones)
						EditorGUILayout.Space();
				}

				if (useBones)
				{
					if (GUILayout.Button("Add Bone Transform", GUILayout.MaxWidth(240)))
					{
						Undo.RecordObject(target.target, "Add Bone Shape");
						shape.bones.Add(new BoneShape());
						selectedBone = shape.bones.Count - 1;
						EditorUtility.SetDirty(target.target);
					}
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					if (allowCreationFromAnimClip)
					{
						GUILayout.Space(5);
						GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Create Pose from AnimationClip", GUILayout.MaxWidth(240)))
						{
							PoseExtractorWizard.ShowWindow(blendSystem.transform, shape, name);
						}
						GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();
					}
				}
				else
				{
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
				}

				if (blendSystem.blendableCount == 0 && !useBones)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Space(10);
					EditorGUILayout.HelpBox(blendSystem.noBlendablesMessage, MessageType.Warning);
					GUILayout.Space(10);
					GUILayout.EndHorizontal();
				}
				GUILayout.Space(14);
				EditorGUILayout.EndVertical();
				GUILayout.Space(6);
			}
			else
			{
				Rect box = EditorGUILayout.BeginHorizontal(GUILayout.Height(17));
				if (shape.verified)
				{
					if (EditorGUIUtility.isProSkin)
					{
						GUI.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
					}
					else
					{
						GUI.backgroundColor = Color.white;
					}
				}
				else
				{
					GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);
				}

				if (GUI.Button(box, "", lightToolbar))
				{
					currentToggle = id;
					currentTarget = target.target;
					selectedBone = 0;

					searchString = "";
					currentSearchBlendable = -1;
				}
				GUI.backgroundColor = Color.white;

				GUILayout.Box(name, EditorStyles.miniLabel, GUILayout.Width(250));
				if (shape.weights.Count == 1)
				{
					GUILayout.Box("1 " + blendSystem.blendableDisplayName, EditorStyles.miniLabel);
				}
				else if (shape.weights.Count > 1)
				{
					GUILayout.Box(shape.weights.Count.ToString() + " " + blendSystem.blendableDisplayNamePlural, EditorStyles.miniLabel);
				}
				if (shape.bones.Count == 1 && useBones)
				{
					GUILayout.Box("1 Bone Transform", EditorStyles.miniLabel);
				}
				else if (shape.bones.Count > 1 && useBones)
				{
					GUILayout.Box(shape.bones.Count.ToString() + " Bone Transforms", EditorStyles.miniLabel);
				}

				if (!shape.verified)
				{
					GUILayout.FlexibleSpace();
					GUILayout.Box("Missing", EditorStyles.miniLabel);
					GUILayout.FlexibleSpace();
				}
				 
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(1);
			}

			return markedForDeletion;
		}

		public static void DrawTimeline(float y, float timeStart, float timeEnd, float width)
		{
			if (whiteMiniLabelBold == null)
			{
				whiteMiniLabelBold = new GUIStyle(EditorStyles.whiteMiniLabel);
				whiteMiniLabelBold.fontStyle = FontStyle.Bold;
			}

			float viewportSeconds = timeEnd - timeStart;
			float pixelsPerSecond = width / viewportSeconds;
			int pixelsPerMarker = (int)pixelsPerSecond;
			float timeOffset = (timeStart % 1) * pixelsPerSecond;
			int[] skipLevels = { 1, 2, 5, 10, 15, 30, 60, 120, 180, 240, 300 };
			int level = 0;
			int skip = 1;

			while (pixelsPerMarker < 50)
			{
				if (skip == skipLevels[skipLevels.Length - 1])
					break;

				skip = skipLevels[level++];
				pixelsPerMarker = Mathf.FloorToInt(width / (viewportSeconds / skip));
			}

			for (int a = 0; a < viewportSeconds + 1; a++)
			{
				int start = skip - (Mathf.FloorToInt(timeStart) % skip);
				if ((a - start) % skip != 0)
					continue;

				int s = a + Mathf.FloorToInt(timeStart);
				if (s > 0 && s % 60 == 0)
				{
					GUI.DrawTexture(new Rect((a * pixelsPerSecond) - timeOffset, y + 1, 2, 16), markerLine);
					GUI.Box(new Rect((a * pixelsPerSecond) - (timeOffset - 5), y, 30, 20), (s / 60).ToString() + "m", whiteMiniLabelBold);
				}
				else
				{
					GUI.DrawTexture(new Rect((a * pixelsPerSecond) - timeOffset, y + 1, 1, 12), markerLine);
					GUI.Box(new Rect((a * pixelsPerSecond) - (timeOffset - 5), y, 30, 20), (s % 60).ToString().PadLeft(2, '0') + "s", EditorStyles.whiteMiniLabel);
				}

			}
		}

		public static string DrawPathField(string label, string value, string pathPrefsName, string filePanelName)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(label);
			GUILayout.Space(10);
			string oldValue = value;
			value = EditorGUILayout.TextField(value);
			if (GUILayout.Button("Browse"))
			{
				try
				{
					string startDir = "";
					if (System.IO.Directory.Exists(value))
					{
						startDir = value;
					}
					else
					{
						startDir = string.IsNullOrEmpty(value) ? "" : System.IO.Path.GetDirectoryName(value);
					}

					string path = EditorUtility.OpenFilePanel(filePanelName, startDir, "");
					if (!string.IsNullOrEmpty(path))
						value = path;
				}
				catch
				{
					string path = EditorUtility.OpenFilePanel(filePanelName, "", "");
					if (!string.IsNullOrEmpty(path))
						value = path;
				}
			}
			if (!string.IsNullOrEmpty(pathPrefsName) && value != oldValue)
			{
				EditorPrefs.SetString(pathPrefsName, value);
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			return value;
		}

		public static string DrawVerifiedPathField(string label, string value, string editorPrefsPathKey, string editorPrefsVerifiedStatusKey, string filePanelName, System.Func<bool> verificationCallback, string verifiedMessage = "Verified", string notVerifiedMessage = "Not Verified")
		{
			bool verified = EditorPrefs.GetBool(editorPrefsVerifiedStatusKey, false);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(label);
			GUILayout.Space(5);
			GUI.color = verified ? Color.green : Color.red;
			GUILayout.Box(new GUIContent(indicator, verified ? verifiedMessage : notVerifiedMessage), GUIStyle.none);
			GUI.color = Color.white;
			GUILayout.Space(10);
			string oldValue = value;
			value = EditorGUILayout.TextField(value);
			if (GUILayout.Button("Browse"))
			{
				try
				{
					string startDir = "";
					if (System.IO.Directory.Exists(value))
					{
						startDir = value;
					}
					else
					{
						startDir = string.IsNullOrEmpty(value) ? "" : System.IO.Path.GetDirectoryName(value);
					}

					string path = EditorUtility.OpenFilePanel(filePanelName, startDir, "");
					if (!string.IsNullOrEmpty(path))
						value = path;
				}
				catch
				{
					string path = EditorUtility.OpenFilePanel(filePanelName, "", "");
					if (!string.IsNullOrEmpty(path))
						value = path;
				}
			}
			if (value != oldValue)
			{
				EditorPrefs.SetString(editorPrefsPathKey, value);
			}
			GUILayout.Space(5);
			if (GUILayout.Button("Verify"))
			{
				verified = verificationCallback();

				EditorPrefs.SetString(editorPrefsPathKey, value);
				EditorPrefs.SetBool(editorPrefsVerifiedStatusKey, verified);
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			return value;
		}

		public static void DrawVerifiedPathField(SerializedProperty property, string editorPrefsPathKey, string editorPrefsVerifiedStatusKey, string filePanelName, System.Func<bool> verificationCallback, string verifiedMessage = "Verified", string notVerifiedMessage = "Not Verified")
		{
			bool verified = EditorPrefs.GetBool(editorPrefsVerifiedStatusKey, false);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(property.displayName);
			GUILayout.Space(5);
			GUI.color = verified ? Color.green : Color.red;
			GUILayout.Box(new GUIContent(indicator, verified ? verifiedMessage : notVerifiedMessage), GUIStyle.none);
			GUI.color = Color.white;
			GUILayout.Space(10);
			property.stringValue = EditorGUILayout.TextField(property.stringValue);
			if (GUILayout.Button("Browse"))
			{
				try
				{
					string startDir = "";
					if (System.IO.Directory.Exists(property.stringValue))
					{
						startDir = property.stringValue;
					}
					else
					{
						startDir = string.IsNullOrEmpty(property.stringValue) ? "" : System.IO.Path.GetDirectoryName(property.stringValue);
					}

					string path = EditorUtility.OpenFilePanel(filePanelName, startDir, "");
					if (!string.IsNullOrEmpty(path))
						property.stringValue = path;
				}
				catch
				{
					string path = EditorUtility.OpenFilePanel(filePanelName, "", "");
					if (!string.IsNullOrEmpty(path))
						property.stringValue = path;
				}
			}
			GUILayout.Space(5);
			if (GUILayout.Button("Verify"))
			{
				verified = verificationCallback();

				EditorPrefs.SetString(editorPrefsPathKey, property.stringValue);
				EditorPrefs.SetBool(editorPrefsVerifiedStatusKey, verified);
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}

		public static Rect BeginPaddedHorizontal()
		{
			return BeginPaddedHorizontal(0);
		}

		public static void EndPaddedHorizontal()
		{
			EndPaddedHorizontal(0);
		}

		public static bool FixedBeginFadeGroup(float value)
		{
			if (value == 0f)
			{
				return false;
			}
			else if (value == 1f)
			{
				return true;
			}

			return EditorGUILayout.BeginFadeGroup(value);
		}

		public static void FixedEndFadeGroup(float value)
		{
			if (value == 0f || value == 1f)
			{
				return;
			}
			EditorGUILayout.EndFadeGroup();
		}

		public static Rect BeginPaddedHorizontal(int minPadding)
		{
			Rect r = EditorGUILayout.BeginHorizontal();
			GUILayout.Space(minPadding);
			GUILayout.FlexibleSpace();
			return r;
		}

		public static void EndPaddedHorizontal(int minPadding)
		{
			GUILayout.FlexibleSpace();
			GUILayout.Space(minPadding);
			EditorGUILayout.EndHorizontal();
		}

		public static string AddSpaces(string input)
		{
			if (string.IsNullOrEmpty(input))
				return "";

			StringBuilder newText = new StringBuilder(input.Length * 2);
			newText.Append(input[0]);
			for (int i = 1; i < input.Length; i++)
			{
				if (char.IsUpper(input[i]) && input[i - 1] != ' ')
				{
					if (i + 1 < input.Length)
					{
						if (!char.IsUpper(input[i - 1]) || !char.IsUpper(input[i + 1]))
						{
							newText.Append(' ');
						}
					}
					else
					{
						newText.Append(' ');
					}
				}
				newText.Append(input[i]);
			}
			return newText.ToString();
		}
	}
}
