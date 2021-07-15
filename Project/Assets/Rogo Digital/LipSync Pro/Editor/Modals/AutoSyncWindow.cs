using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using RogoDigital;
using RogoDigital.Lipsync;
using RogoDigital.Lipsync.AutoSync;
using System.Collections.Generic;
using System.IO;

public class AutoSyncWindow : ModalWindow
{
	private LipSyncClipSetup setup;
	private LipSyncData convertedSetupData;
	private List<ClipItem> clips = new List<ClipItem>();

	// AS Settings
	private ReorderableList clipList, moduleList;

	private int currentPreset = -1;
	private bool presetChanged = false;

	private List<AutoSyncModule> currentModules = new List<AutoSyncModule>();

	private bool batchIncomplete;
	private Vector2 mainScroll, outputScroll;

	private int currentIndex = -1;
	private Editor currentEditor;

	private Texture2D infoIcon, warningIcon, errorIcon, plusIcon, saveIcon, lipSyncIcon, audioIcon;
	private List<Type> autoSyncModuleTypes;
	private Dictionary<Type, AutoSyncModuleInfoAttribute> moduleInfos;
	private AutoSync autoSyncInstance;
	private AutoSyncPreset[] presets;

	private string overviewString = "Nothing To Show";
	private bool skipOnFail, closeOnFinish;
	private bool autoLoadTranscript;
	private bool overwriteOutput;
	private int selectedClip, currentClip = 0;

	private PhonemeMarker phonemeTemplate;
	private EmotionMarker emotionTemplate;

	private void OnEnable()
	{
		if (setup == null)
			return;

		autoSyncModuleTypes = AutoSyncUtility.GetModuleTypes();
		presets = AutoSyncUtility.GetPresets();

		moduleInfos = new Dictionary<Type, AutoSyncModuleInfoAttribute>();
		for (int i = 0; i < autoSyncModuleTypes.Count; i++)
		{
			moduleInfos.Add(autoSyncModuleTypes[i], AutoSyncUtility.GetModuleInfo(autoSyncModuleTypes[i]));
		}

		infoIcon = EditorGUIUtility.FindTexture("console.infoicon.sml");
		warningIcon = EditorGUIUtility.FindTexture("console.warnicon.sml");
		errorIcon = EditorGUIUtility.FindTexture("console.erroricon.sml");

		lipSyncIcon = EditorGUIUtility.FindTexture("LipSyncData Icon");
		audioIcon = EditorGUIUtility.FindTexture("AudioSource Gizmo");

		skipOnFail = EditorPrefs.GetBool("LipSync_AS_SkipOnFail", true);
		closeOnFinish = EditorPrefs.GetBool("LipSync_AS_CloseOnFinish", true);
		autoLoadTranscript = EditorPrefs.GetBool("LipSync_LoadTranscriptFromTxt", true);
		overwriteOutput = EditorPrefs.GetBool("LipSync_AS_OverwriteOutput", false);

		if (EditorGUIUtility.isProSkin)
		{
			plusIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/plus.png");
			saveIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/save.png");
		}
		else
		{
			plusIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/plus.png");
			saveIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Light/save.png");
		}

		clipList = new ReorderableList(clips, typeof(ClipItem));

		clipList.onSelectCallback += (list) => { selectedClip = list.index; };
		clipList.onChangedCallback += (list) => { UpdateOverviewString(); };
		clipList.drawHeaderCallback = (Rect r) =>
		{
			GUI.Label(r, "Clips");
		};
		clipList.elementHeightCallback = (int index) =>
		{
			return (EditorGUIUtility.singleLineHeight * 2) + 12;
		};
		clipList.drawElementCallback = (Rect r, int index, bool active, bool focused) =>
		{
			var features = ClipFeatures.None;
			if (currentModules.Count > 0)
			{
				for (int i = 0; i < currentModules.Count; i++)
				{
					features |= GetMissingClipFeaturesInClipEditor(clips[index], currentModules, i);
				}
			}

			var errors = "";
			var hasErrors = false;
			var leftPadding = 0;

			if (features != ClipFeatures.None)
			{
				errors += "Missing features (" + features + ")";
				hasErrors = true;
				leftPadding = 14;
			}

			if (clips[index].outputMode == OutputMode.AppendToPrevious && index == 0)
			{
				if (hasErrors)
					errors += ", ";

				errors += "No clip to append to";
				hasErrors = true;
				leftPadding = 14;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUI.BeginDisabledGroup(clips[index].isLocked);
			var objFieldWidth = Mathf.Clamp(r.width - 150 - 45 - leftPadding, 50, 800);
			var objFieldRect = new Rect(r.x + 115 + leftPadding, r.y + 3, objFieldWidth - 60, EditorGUIUtility.singleLineHeight);
			GUI.Label(new Rect(r.x + 45 + leftPadding, r.y + 3, 70, EditorGUIUtility.singleLineHeight), "Input");
			if (GUI.Button(new Rect(r.x + 5 + leftPadding, r.y + 5, 35, EditorGUIUtility.singleLineHeight * 2), clips[index].useLSD ? lipSyncIcon : audioIcon))
			{
				clips[index].useLSD = !clips[index].useLSD;
			}

			if (clips[index].useLSD)
			{
				if (clips[index].lipSyncClip == convertedSetupData)
				{
					GUI.Label(objFieldRect, new GUIContent("Clip Editor Contents", infoIcon, "This item represents the data that currently exists in the Clip Editor."));
				}
				else
				{
					clips[index].lipSyncClip = (LipSyncData)EditorGUI.ObjectField(objFieldRect, clips[index].lipSyncClip, typeof(LipSyncData), false);
				}
			}
			else
			{
				clips[index].audioClip = (AudioClip)EditorGUI.ObjectField(objFieldRect, clips[index].audioClip, typeof(AudioClip), false);
			}

			EditorGUI.EndDisabledGroup();

			Rect popupRect = new Rect(r.x + objFieldWidth + 60 + leftPadding, r.y + 3, 130, EditorGUIUtility.singleLineHeight);
			if (GUI.Button(popupRect, ObjectNames.NicifyVariableName(clips[index].outputMode.ToString()), EditorStyles.popup))
			{
				GenericMenu dropdown = new GenericMenu();

				var modes = Enum.GetNames(typeof(OutputMode));

				for (int i = 0; i < modes.Length; i++)
				{
					int k = i;
					if (i == (int)OutputMode.ToClipEditor)
					{
						if (ToClipEditorExists())
						{
#if UNITY_2018_1_OR_NEWER
							dropdown.AddDisabledItem(new GUIContent(ObjectNames.NicifyVariableName(modes[i])), i == (int)clips[index].outputMode);
#else
							dropdown.AddDisabledItem(new GUIContent(ObjectNames.NicifyVariableName(modes[i])));
#endif
						}
						else
						{
							dropdown.AddItem(new GUIContent(ObjectNames.NicifyVariableName(modes[i])), i == (int)clips[index].outputMode, () => { clips[index].outputMode = (OutputMode)k; SetOutputPath(clips[index]); UpdateOverviewString(); });
						}
					}
					else
					{
						dropdown.AddItem(new GUIContent(ObjectNames.NicifyVariableName(modes[i])), i == (int)clips[index].outputMode, () => { clips[index].outputMode = (OutputMode)k; SetOutputPath(clips[index]); UpdateOverviewString(); });
					}
				}

				dropdown.DropDown(popupRect);
			}

			if (clips[index].outputMode == OutputMode.ToLipSyncData || clips[index].outputMode == OutputMode.ToXML)
			{
				GUI.Label(new Rect(r.x + 45 + leftPadding, r.y + EditorGUIUtility.singleLineHeight + 6, 70, EditorGUIUtility.singleLineHeight), "Output");
				clips[index].outputPath = EditorGUI.TextField(new Rect(r.x + 115 + leftPadding, r.y + EditorGUIUtility.singleLineHeight + 6, r.width - 220 - leftPadding, EditorGUIUtility.singleLineHeight), clips[index].outputPath);
				if (GUI.Button(new Rect(r.x + (r.width - 95), r.y + +EditorGUIUtility.singleLineHeight + 6, 40, EditorGUIUtility.singleLineHeight), "..."))
				{
					var defaultName = "";
					if (clips[index].useLSD)
					{
						if (clips[index].lipSyncClip == convertedSetupData)
						{
							defaultName = convertedSetupData.name;
							if (string.IsNullOrEmpty(defaultName))
							{
								defaultName = "Clip Editor Contents";
							}
						}
						else if (clips[index].lipSyncClip)
						{
							defaultName = clips[index].lipSyncClip.name;
						}
					}
					else
					{
						if (clips[index].audioClip)
						{
							defaultName = clips[index].audioClip.name;
						}
					}

					var path = "";
					if (clips[index].outputMode == OutputMode.ToLipSyncData)
					{
						path = EditorUtility.SaveFilePanelInProject("Output Path", defaultName, "asset", "");
					}
					else if (clips[index].outputMode == OutputMode.ToXML)
					{
						path = EditorUtility.SaveFilePanelInProject("Output Path", defaultName, "xml", "");
					}

					if (!string.IsNullOrEmpty(path))
					{
						clips[index].outputPath = path;
					}

					UpdateOverviewString();
				}

				if (GUI.Button(new Rect(r.x + (r.width - 45), r.y + +EditorGUIUtility.singleLineHeight + 6, 40, EditorGUIUtility.singleLineHeight), new GUIContent("D", "Set to default path for file")))
				{
					SetOutputPath(clips[index], true);
					UpdateOverviewString();
				}
			}

			if (hasErrors)
			{
				GUI.Box(new Rect(r.x - 5, r.y + (EditorGUIUtility.singleLineHeight) - 3, 18, 18), new GUIContent(errorIcon, "Clip will not be processed as it has errors: " + errors), GUIStyle.none);
			}

			if (EditorGUI.EndChangeCheck())
			{
				SetOutputPath(clips[index]);
				UpdateOverviewString();
			}
		};
		clipList.onAddDropdownCallback = (Rect r, ReorderableList list) =>
		{
			GenericMenu addMenu = new GenericMenu();

			bool hasEditorData = false;
			for (int i = 0; i < clips.Count; i++)
			{
				if (clips[i].lipSyncClip == convertedSetupData)
				{
					hasEditorData = true;
				}
			}

			if (hasEditorData)
			{
				addMenu.AddDisabledItem(new GUIContent("Add Clip Editor Contents"));
			}
			else
			{
				addMenu.AddItem(new GUIContent("Add Clip Editor Contents"), false, () =>
				{
					var clip = AddLipSyncClip(convertedSetupData);
					clip.isLocked = true;
				});
			}
			addMenu.AddSeparator("");

			addMenu.AddItem(new GUIContent("Add AudioClip"), false, () =>
			{
				AddAudioClip(null);
			});
			addMenu.AddItem(new GUIContent("Add LipSyncData"), false, () =>
			{
				AddLipSyncClip(null);
			});
			addMenu.AddSeparator("");
			addMenu.AddItem(new GUIContent("Add All Selected"), false, () =>
			{
				var audioClips = Selection.GetFiltered<AudioClip>(SelectionMode.Assets);
				var lipSyncDatas = Selection.GetFiltered<LipSyncData>(SelectionMode.Assets);

				for (int i = 0; i < audioClips.Length; i++)
				{
					AddAudioClip(audioClips[i]);
				}

				for (int i = 0; i < lipSyncDatas.Length; i++)
				{
					AddLipSyncClip(lipSyncDatas[i]);
				}
			});

			addMenu.DropDown(r);
		};

		moduleList = new ReorderableList(currentModules, typeof(AutoSyncModule));
		moduleList.onChangedCallback += (list) => { UpdateOverviewString(); };
		moduleList.onAddDropdownCallback = (Rect r, ReorderableList list) =>
		{
			GenericMenu addMenu = new GenericMenu();
			for (int i = 0; i < autoSyncModuleTypes.Count; i++)
			{
				bool isAdded = false;

				for (int m = 0; m < currentModules.Count; m++)
				{
					if (currentModules[m].GetType() == autoSyncModuleTypes[i])
					{
						isAdded = true;
						break;
					}
				}

				if (isAdded)
				{
					addMenu.AddDisabledItem(new GUIContent(moduleInfos[autoSyncModuleTypes[i]].displayName));
				}
				else
				{
					int e = i;
					addMenu.AddItem(new GUIContent(moduleInfos[autoSyncModuleTypes[i]].displayName), false, () => { AddModule(e); });
				}
			}
			addMenu.AddSeparator("");
			addMenu.AddItem(new GUIContent("Get More Modules"), false, () => { RDExtensionWindow.ShowWindow("LipSync_Pro"); });
			addMenu.DropDown(r);
		};
		moduleList.drawHeaderCallback = (Rect r) =>
		{
			GUI.Label(r, "Modules");
		};
		moduleList.drawElementCallback = (Rect r, int index, bool active, bool focused) =>
		{
			var missingFeatures = ClipFeatures.None;
			if (selectedClip < clips.Count)
				missingFeatures |= GetMissingClipFeaturesInClipEditor(clips[selectedClip], currentModules, index);

			var content = new GUIContent();

			var info = moduleInfos[currentModules[index].GetType()];

			if (missingFeatures == ClipFeatures.None)
			{
				content.image = infoIcon;
				content.text = info.displayName;
				content.tooltip = info.description;
			}
			else
			{
				content.image = errorIcon;
				content.text = info.displayName + " (Has Errors)";
				content.tooltip = "Missing: " + missingFeatures.ToString();
			}

			GUI.Label(r, content);
		};
		moduleList.onSelectCallback += (ReorderableList list) =>
		{
			currentIndex = list.index;

			if (currentEditor)
				DestroyImmediate(currentEditor);

			if (list.index >= 0)
			{
				currentEditor = Editor.CreateEditor(currentModules[list.index]);
			}
		};
		moduleList.onChangedCallback += (ReorderableList list) =>
		{
			moduleList.onSelectCallback.Invoke(moduleList);
		};
	}

	void OnDestroy()
	{
		parent.currentModal = null;
		parent.Focus();

		for (int i = 0; i < currentModules.Count; i++)
		{
			DestroyImmediate(currentModules[i]);
		}
	}

	void OnGUI()
	{
		bool ready = currentModules.Count > 0 && clips.Count > 0;

		Rect toolbarRect = EditorGUILayout.BeginHorizontal();
		toolbarRect.x = 0;
		GUI.Box(toolbarRect, "", EditorStyles.toolbar);
		GUILayout.Box("Preset:", EditorStyles.label);
		GUILayout.FlexibleSpace();
		GUILayout.Box(new GUIContent(currentPreset >= 0 ? presets[currentPreset].displayName + (presetChanged ? "*" : "") : "No Preset Loaded", currentPreset >= 0 ? presetChanged ? warningIcon : infoIcon : null, currentPreset >= 0 ? presets[currentPreset].description : ""), EditorStyles.label);
		GUILayout.FlexibleSpace();
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button(new GUIContent("Load", plusIcon, "Load a Preset"), EditorStyles.toolbarDropDown, GUILayout.Width(70)))
		{
			GenericMenu menu = new GenericMenu();

			for (int i = 0; i < presets.Length; i++)
			{
				var k = i;
				menu.AddItem(new GUIContent(presets[i].displayName), false, () =>
				{
					LoadPreset(k);
				});
			}

			menu.ShowAsContext();
		}
		if (GUILayout.Button(new GUIContent("Save As New", saveIcon, "Save the current setup as a new preset"), EditorStyles.toolbarButton, GUILayout.Width(100)))
		{
			var savePath = EditorUtility.SaveFilePanelInProject("Save AutoSync Preset", "New AutoSync Preset", "asset", "");
			if (!string.IsNullOrEmpty(savePath))
			{
				AutoSyncPreset preset = null;

				if (File.Exists(savePath))
				{
					preset = AssetDatabase.LoadAssetAtPath<AutoSyncPreset>(savePath);
				}
				else
				{
					preset = CreateInstance<AutoSyncPreset>();
					preset.CreateFromModules(currentModules.ToArray());

					preset.displayName = Path.GetFileNameWithoutExtension(savePath);
					preset.description = "Using: ";
					for (int i = 0; i < currentModules.Count; i++)
					{
						preset.description += currentModules[i].GetType().Name;
						if (i < currentModules.Count - 1)
							preset.description += ", ";
					}
				}

				AssetDatabase.CreateAsset(preset, savePath);
				AssetDatabase.Refresh();

				presets = AutoSyncUtility.GetPresets();
				currentPreset = -1;
			}
		}

		EditorGUI.BeginDisabledGroup(currentPreset == -1 || !presetChanged);
		if (GUILayout.Button(new GUIContent("Save Changes", saveIcon, "Overwrite your changes to the current preset"), EditorStyles.toolbarButton, GUILayout.Width(100)))
		{
			if (EditorUtility.DisplayDialog("Overwrite Preset?", "Are you sure you want to overwrite the saved preset? This cannot be undone.", "Yes", "No"))
			{
				string path = AssetDatabase.GetAssetPath(presets[currentPreset]);

				if (!string.IsNullOrEmpty(path))
				{
					presets[currentPreset].CreateFromModules(currentModules.ToArray());
					AssetDatabase.SaveAssets();
					presetChanged = false;
				}
			}
		}
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndHorizontal();
		mainScroll = GUILayout.BeginScrollView(mainScroll, false, true);
		GUILayout.Space(10);
		BeginPad(10);
		clipList.DoLayoutList();
		EndPad(10);
		GUILayout.Space(10);
		BeginPad(10);
		moduleList.DoLayoutList();
		EndPad(10);
		GUILayout.Space(5);
		BeginPad(10);
		var bgRect = EditorGUILayout.BeginVertical();
		bgRect.width += 4;
		var width = bgRect.width;

		GUI.Box(bgRect, "", "RL Header");
		BeginPad(8);
		GUILayout.Label("Edit Module");
		EndPad(0);
		EditorGUILayout.EndVertical();
		EndPad(10);
		BeginPad(10);
		bgRect = EditorGUILayout.BeginVertical();
		bgRect.width = width;
		GUI.Box(bgRect, "", "RL Background");
		GUILayout.Space(5);
		BeginPad(5);
		if (currentIndex >= 0 && currentIndex < currentModules.Count)
		{
			var missingFeatures = ClipFeatures.None;
			if (selectedClip < clips.Count)
				missingFeatures |= GetMissingClipFeaturesInClipEditor(clips[selectedClip], currentModules, currentIndex);


			if (missingFeatures != ClipFeatures.None)
			{
				EditorGUILayout.HelpBox(string.Format("This module requires: {0}.\n These features must either be present in the clip already, or be provided by a module above this one.", missingFeatures), MessageType.Error);
			}
			EditorGUI.BeginChangeCheck();
			currentEditor.OnInspectorGUI();
			if (EditorGUI.EndChangeCheck())
			{
				UpdateOverviewString();
				presetChanged = true;
			}
		}
		else if (currentModules.Count > 0)
		{
			GUILayout.Label("Select a module above to edit its settings.", EditorStyles.centeredGreyMiniLabel);
		}
		else
		{
			GUILayout.Label("Add a module and select it to edit.", EditorStyles.centeredGreyMiniLabel);
		}

		EndPad(3);
		GUILayout.Space(10);
		EditorGUILayout.EndVertical();
		EndPad(10);

		GUILayout.Space(20);
		EditorGUILayout.EndScrollView();
		GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
		GUILayout.Space(5);
		GUILayout.BeginHorizontal();
		GUILayout.Space(20);
		EditorGUI.BeginChangeCheck();
		skipOnFail = EditorGUILayout.ToggleLeft("Skip Failed Clips?", skipOnFail);
		closeOnFinish = EditorGUILayout.ToggleLeft("Close Window When Finished?", closeOnFinish);
		GUILayout.Space(20);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Space(20);
		autoLoadTranscript = EditorGUILayout.ToggleLeft("Auto-Load Transcript from .txt?", autoLoadTranscript);
		overwriteOutput = EditorGUILayout.ToggleLeft("Overwrite output file if already exists?", overwriteOutput);
		GUILayout.Space(20);
		GUILayout.EndHorizontal();

		if (EditorGUI.EndChangeCheck())
		{
			EditorPrefs.SetBool("LipSync_AS_SkipOnFail", skipOnFail);
			EditorPrefs.SetBool("LipSync_AS_CloseOnFinish", closeOnFinish);
			EditorPrefs.SetBool("LipSync_LoadTranscriptFromTxt", autoLoadTranscript);
			EditorPrefs.SetBool("LipSync_AS_OverwriteOutput", overwriteOutput);
		}

		GUILayout.Space(5);

		BeginPad(20);
		GUILayout.Label("Output Summary:", EditorStyles.boldLabel);
		outputScroll = GUILayout.BeginScrollView(outputScroll, GUILayout.MinHeight(75));
		GUILayout.Label(overviewString, EditorStyles.miniLabel);
		EndPad(20);
		GUILayout.EndScrollView();
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		EditorGUI.BeginDisabledGroup(!ready);
		if (GUILayout.Button("Run AutoSync", GUILayout.Height(25), GUILayout.Width(115)))
		{
			if (autoSyncInstance == null)
				autoSyncInstance = new AutoSync();

			currentClip = 0;

			for (int i = 0; i < clips.Count; i++)
			{
				if (clips[i].useLSD)
				{
					var temp = (TemporaryLipSyncData)clips[i].lipSyncClip;
					clips[i].outputClip = (LipSyncData)temp;
				}
				else
				{
					LipSyncData tempData = CreateInstance<LipSyncData>();
					tempData.clip = clips[i].audioClip;
					tempData.length = tempData.clip.length;

					if (autoLoadTranscript)
					{
						tempData.transcript = AutoSyncUtility.TryGetTranscript(tempData.clip);
					}

					clips[i].outputClip = tempData;
				}
			}

			autoSyncInstance.RunSequence(currentModules.ToArray(), FinishedClip, clips[currentClip].outputClip, phonemeTemplate, emotionTemplate);
		}
		EditorGUI.EndDisabledGroup();
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(15);
	}

	private void UpdateOverviewString()
	{
		List<string> outputs = new List<string>();
		for (int i = 0; i < clips.Count; i++)
		{
			// Check if Clip will be processed and continue without describing if not.
			if (clips[i].useLSD)
			{
				if (clips[i].lipSyncClip == null)
					continue;
			}
			else
			{
				if (clips[i].audioClip == null)
					continue;
			}

			var missingFeatures = ClipFeatures.None;
			for (int m = 0; m < currentModules.Count; m++)
			{
				missingFeatures |= GetMissingClipFeaturesInClipEditor(clips[i], currentModules, m);
			}

			if (missingFeatures != ClipFeatures.None)
				continue;

			// Clip will be processed (doesn't guarantee success)
			if (clips[i].outputMode != OutputMode.AppendToPrevious)
			{
				string newline = "Output " + (outputs.Count + 1) + ": ";

				if (clips[i].outputMode == OutputMode.ToClipEditor)
				{
					newline += "To Clip Editor - ";
				}
				else
				{
					newline += "\"" + Path.GetFileName(clips[i].outputPath) + "\" - ";
				}

				newline += "Created from ";

				if (clips[i].useLSD)
				{
					if (clips[i].lipSyncClip == convertedSetupData)
					{
						newline += "Clip Editor contents";
					}
					else
					{
						newline += clips[i].lipSyncClip.name;
					}
				}
				else
				{
					newline += clips[i].audioClip.name;
				}

				outputs.Add(newline);
			}
			else if (outputs.Count - 1 >= 0)
			{
				outputs[outputs.Count - 1] += ", ";

				if (clips[i].useLSD)
				{
					if (clips[i].lipSyncClip == convertedSetupData)
					{
						outputs[outputs.Count - 1] += "Clip Editor contents";
					}
					else
					{
						outputs[outputs.Count - 1] += clips[i].lipSyncClip.name;
					}
				}
				else
				{
					outputs[outputs.Count - 1] += clips[i].audioClip.name;
				}
			}
		}

		if (outputs.Count == 0)
		{
			overviewString = "Nothing To Show";
		}
		else
		{
			overviewString = "";
			for (int i = 0; i < outputs.Count; i++)
			{
				overviewString += outputs[i] + "\n";
			}
		}
	}

	private void LoadPreset(int presetIndex)
	{
		for (int i = 0; i < currentModules.Count; i++)
		{
			DestroyImmediate(currentModules[i]);
		}
		currentModules.Clear();
		currentPreset = presetIndex;
		presetChanged = false;

		if (presetIndex >= 0)
		{
			for (int i = 0; i < presets[presetIndex].modules.Length; i++)
			{
				AddModule(presets[presetIndex].modules[i], presets[presetIndex].moduleSettings[i]);
			}
		}
	}

	private ClipFeatures GetMissingClipFeaturesInClipEditor(ClipItem clipItem, List<AutoSyncModule> modules, int index)
	{
		var module = modules[index];
		var req = module.GetCompatibilityRequirements();
		ClipFeatures metCriteria = 0;

		// Find which criteria are met, or will be met once the module chain has run as far as the provided index.

		for (int i = 0; i < index; i++)
		{
			metCriteria |= modules[i].GetOutputCompatibility();
		}

		if (clipItem.useLSD && clipItem.lipSyncClip != null)
		{
			if (clipItem.lipSyncClip.clip)
				metCriteria |= ClipFeatures.AudioClip;

			if (!string.IsNullOrEmpty(clipItem.lipSyncClip.transcript))
				metCriteria |= ClipFeatures.Transcript;

			if (clipItem.lipSyncClip.phonemeData != null && clipItem.lipSyncClip.phonemeData.Length > 0)
				metCriteria |= ClipFeatures.Phonemes;

			if (clipItem.lipSyncClip.emotionData != null && clipItem.lipSyncClip.emotionData.Length > 0)
				metCriteria |= ClipFeatures.Emotions;

			if (clipItem.lipSyncClip.gestureData != null && clipItem.lipSyncClip.gestureData.Length > 0)
				metCriteria |= ClipFeatures.Gestures;
		}
		else if (!clipItem.useLSD)
		{
			if (clipItem.audioClip)
				metCriteria |= ClipFeatures.AudioClip;

			if (autoLoadTranscript)
				metCriteria |= ClipFeatures.Transcript;
		}

		// Compare masks
		var inBoth = req & metCriteria;
		return inBoth ^ req;
	}

	private bool ToClipEditorExists()
	{
		for (int i = 0; i < clips.Count; i++)
		{
			if (clips[i].outputMode == OutputMode.ToClipEditor)
			{
				return true;
			}
		}

		return false;
	}

	private void SetOutputPath(ClipItem clip, bool overrideExisting = false)
	{
		if (string.IsNullOrEmpty(clip.outputPath) || overrideExisting)
		{
			if (clip.useLSD)
			{
				if (clip.lipSyncClip != convertedSetupData)
				{
					if (clip.outputMode == OutputMode.ToLipSyncData)
					{
						clip.outputPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(clip.lipSyncClip), "asset");
					}
					else if (clip.outputMode == OutputMode.ToXML)
					{
						clip.outputPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(clip.lipSyncClip), "xml");
					}
				}
				else if (clip.lipSyncClip.clip)
				{
					if (clip.outputMode == OutputMode.ToLipSyncData)
					{
						clip.outputPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(clip.lipSyncClip.clip), "asset");
					}
					else if (clip.outputMode == OutputMode.ToXML)
					{
						clip.outputPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(clip.lipSyncClip.clip), "xml");
					}
				}
			}
			else
			{
				if (clip.outputMode == OutputMode.ToLipSyncData)
				{
					clip.outputPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(clip.audioClip), "asset");
				}
				else if (clip.outputMode == OutputMode.ToXML)
				{
					clip.outputPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(clip.audioClip), "xml");
				}
			}
		}
		else
		{
			if (clip.outputMode == OutputMode.ToLipSyncData)
			{
				clip.outputPath = Path.ChangeExtension(clip.outputPath, "asset");
			}
			else if (clip.outputMode == OutputMode.ToXML)
			{
				clip.outputPath = Path.ChangeExtension(clip.outputPath, "xml");
			}
		}
	}

	private ClipItem AddAudioClip(AudioClip clip)
	{
		var item = new ClipItem();

		item.audioClip = clip;
		item.useLSD = false;

		if (ToClipEditorExists())
		{
			item.outputMode = OutputMode.ToLipSyncData;
		}

		SetOutputPath(item);

		clips.Add(item);
		UpdateOverviewString();
		return item;
	}

	private ClipItem AddLipSyncClip(LipSyncData clip)
	{
		var item = new ClipItem();

		item.lipSyncClip = clip;
		item.useLSD = true;

		if (ToClipEditorExists())
		{
			item.outputMode = OutputMode.ToLipSyncData;
		}

		SetOutputPath(item);

		clips.Add(item);
		UpdateOverviewString();
		return item;
	}

	private void AddModule(string name, string jsonData)
	{
		var module = (AutoSyncModule)CreateInstance(name);

		if (!module)
			return;

		if (!string.IsNullOrEmpty(jsonData))
		{
			JsonUtility.FromJsonOverwrite(jsonData, module);
		}

		module.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

		currentModules.Add(module);
		UpdateOverviewString();
	}

	private void AddModule(int index)
	{
		var module = (AutoSyncModule)CreateInstance(autoSyncModuleTypes[index].Name);
		module.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

		currentModules.Add(module);
		UpdateOverviewString();
	}

	private void FinishedClip(LipSyncData outputData, AutoSync.ASProcessDelegateData data)
	{
		if (data.success)
		{
			// Successfully processed clip, store data for output once the batch is complete
			clips[currentClip].outputClip = outputData;

			clips[currentClip].hasBeenProcessed = true;
		}
		else
		{
			// AutoSync was unsuccessful, log error and either continue or stop depending on skipOnFailValue
			string clipName = "Undefined";
			if (outputData.clip)
			{
				clipName = outputData.clip.name;
			}

			if (skipOnFail)
			{
				batchIncomplete = true;
				clips[currentClip].hasBeenProcessed = false;
				Debug.LogErrorFormat("AutoSync: Processing failed on clip '{0}'. Continuing with batch.", clipName);
			}
			else
			{
				Debug.LogErrorFormat("AutoSync: Processing failed on clip '{0}'. Aborting AutoSync operation.", clipName);
				EditorUtility.ClearProgressBar();

				if (closeOnFinish)
				{
					setup.ShowNotification(new GUIContent("AutoSync Failed. See console for details."));
					setup.disabled = false;
					Close();
				}
			}
		}

		if (currentClip < clips.Count - 1)
		{
			// There are more clips to process in the batch, so process the next one
			currentClip++;

			autoSyncInstance = new AutoSync();
			autoSyncInstance.RunSequence(currentModules.ToArray(), FinishedClip, clips[currentClip].outputClip, phonemeTemplate, emotionTemplate);
		}
		else
		{
			// This was the last clip, move on to output
			var outputs = new List<ClipItem>();

			for (int i = 0; i < clips.Count; i++)
			{
				if (clips[i].outputMode != OutputMode.AppendToPrevious)
				{
					var output = new ClipItem();

					output.outputClip = clips[i].outputClip;
					output.outputMode = clips[i].outputMode;
					output.outputPath = clips[i].outputPath;

					var temp = (TemporaryLipSyncData)clips[i].outputClip;
					ArrayUtility.Add(ref output.appendedClips, (LipSyncData)temp);

					outputs.Add(output);
				}
				else
				{
					if (outputs.Count > 0)
					{
						if (outputs[outputs.Count - 1].outputClip.clip && clips[i].outputClip.clip)
						{
							ArrayUtility.Add(ref outputs[outputs.Count - 1].appendedClips, clips[i].outputClip);
						}
						else
						{
							Debug.LogFormat("AutoSync: Can't append clip at position {0}. Clip appending requires all appended clips contain audioclips with matching number of channels.", i);
						}
					}
					else
					{
						Debug.LogFormat("AutoSync: Clip Append (Clip {0}) was found in an invalid position. This clip will not be saved.", i);
					}
				}
			}

			for (int i = 0; i < outputs.Count; i++)
			{
				var settings = LipSyncEditorExtensions.GetProjectFile();
				var outputPath = "";

				if (outputs[i].outputMode == OutputMode.ToClipEditor)
				{
					outputPath = AssetDatabase.GetAssetPath(outputs[i].appendedClips[0].clip);
				}
				else
				{
					outputPath = outputs[i].outputPath;
				}

				var audioOutputPath = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(outputPath) + "/" + Path.GetFileNameWithoutExtension(outputPath) + "_with_appended_clips.wav");

				if (outputs[i].appendedClips.Length > 1)
				{
					// Merge Audio
					string[] paths = new string[outputs[i].appendedClips.Length];

					// Clips to be merged
					for (int k = 0; k < outputs[i].appendedClips.Length; k++)
					{
						var clip = outputs[i].appendedClips[k].clip;
						if (clip)
						{
							paths[k] = AssetDatabase.GetAssetPath(clip);
						}
					}

					AutoSyncConversionUtility.AppendFiles(audioOutputPath, paths);
					AssetDatabase.Refresh();
					outputs[i].outputClip.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioOutputPath);
					outputs[i].outputClip.length = outputs[i].outputClip.clip.length;

					// Merge Data
					float timeOffset = 0;
					outputs[i].outputClip.phonemeData = new PhonemeMarker[0];
					outputs[i].outputClip.emotionData = new EmotionMarker[0];
					outputs[i].outputClip.gestureData = new GestureMarker[0];

					for (int k = 0; k < outputs[i].appendedClips.Length; k++)
					{
						// Retime original markers and add to new output
						var phonemeData = outputs[i].appendedClips[k].phonemeData;
						for (int p = 0; p < phonemeData.Length; p++)
						{
							phonemeData[p].time = ((phonemeData[p].time * outputs[i].appendedClips[k].length) / outputs[i].outputClip.length) + timeOffset;

							ArrayUtility.Add(ref outputs[i].outputClip.phonemeData, phonemeData[p]);
						}

						if (outputs[i].appendedClips[k].emotionData != null)
						{
							var emotionData = outputs[i].appendedClips[k].emotionData;
							for (int p = 0; p < emotionData.Length; p++)
							{
								emotionData[p].startTime = ((emotionData[p].startTime * outputs[i].appendedClips[k].length) / outputs[i].outputClip.length) + timeOffset;
								emotionData[p].endTime = ((emotionData[p].endTime * outputs[i].appendedClips[k].length) / outputs[i].outputClip.length) + timeOffset;
								emotionData[p].blendInTime = ((emotionData[p].blendInTime * outputs[i].appendedClips[k].length) / outputs[i].outputClip.length);
								emotionData[p].blendOutTime = ((emotionData[p].blendOutTime * outputs[i].appendedClips[k].length) / outputs[i].outputClip.length);

								ArrayUtility.Add(ref outputs[i].outputClip.emotionData, emotionData[p]);
							}
						}

						if (outputs[i].appendedClips[k].gestureData != null)
						{
							var gestureData = outputs[i].appendedClips[k].gestureData;
							for (int p = 0; p < gestureData.Length; p++)
							{
								gestureData[p].time = ((gestureData[p].time * outputs[i].appendedClips[k].length) / outputs[i].outputClip.length) + timeOffset;

								ArrayUtility.Add(ref outputs[i].outputClip.gestureData, gestureData[p]);
							}
						}

						// Append transcript
						outputs[i].outputClip.transcript += (i == 0 ? "" : " ") + outputs[i].appendedClips[k].transcript;

						// Update timeOffset for next clip
						timeOffset += outputs[i].appendedClips[k].length / outputs[i].outputClip.length;
					}
				}

				switch (outputs[i].outputMode)
				{
					case OutputMode.ToClipEditor:
						setup.data = (TemporaryLipSyncData)outputs[i].outputClip;
						setup.changed = true;
						setup.previewOutOfDate = true;
						break;
					default:
					case OutputMode.ToLipSyncData:
						outputPath = Path.ChangeExtension(outputPath, "asset");

						try
						{
							LipSyncClipSetup.SaveFile(settings, outputPath, false, outputData.transcript, outputData.length, outputData.phonemeData, outputData.emotionData, outputData.gestureData, outputData.clip);
						}
						catch (Exception e)
						{
							Debug.LogError(e.StackTrace);
						}
						break;
					case OutputMode.ToXML:
						outputPath = Path.ChangeExtension(outputPath, "xml");

						try
						{
							LipSyncClipSetup.SaveFile(settings, outputPath, true, outputData.transcript, outputData.length, outputData.phonemeData, outputData.emotionData, outputData.gestureData, outputData.clip);
						}
						catch (Exception e)
						{
							Debug.LogError(e.StackTrace);
						}
						break;
					case OutputMode.AppendToPrevious:
						Debug.LogError("AutoSync: Invalid Append Operation. Check that a valid error-free clip precedes any clips you're attempting to append.");
						break;
				}
			}

			AssetDatabase.Refresh();
			EditorUtility.ClearProgressBar();

			if (closeOnFinish)
			{
				setup.disabled = false;

				if (!batchIncomplete)
				{
					setup.ShowNotification(new GUIContent("AutoSync Completed Successfully"));
				}
				else
				{
					setup.ShowNotification(new GUIContent("AutoSync Completed With Errors"));
				}

				Close();
			}
		}
	}

	public static AutoSyncWindow CreateWindow(ModalParent parent, LipSyncClipSetup setup, params int[] modules)
	{
		AutoSyncWindow window = CreateInstance<AutoSyncWindow>();

		window.position = new Rect(parent.center.x - 250, parent.center.y - 400, 500, 800);
		window.minSize = new Vector2(400, 500);
		window.titleContent = new GUIContent("AutoSync");

		window.setup = setup;

		window.OnEnable();

		window.convertedSetupData = (LipSyncData)setup.data;

		window.phonemeTemplate = new PhonemeMarker(0, 0)
		{
			intensity = setup.defaultPhonemeIntensity,
			useRandomness = setup.defaultUseRandomness,
			intensityRandomness = setup.defaultIntensityRandomness,
			blendableRandomness = setup.defaultBlendableRandomness,
			bonePositionRandomness = setup.defaultBonePositionRandomness,
			boneRotationRandomness = setup.defaultBoneRotationRandomness,
		};

		window.emotionTemplate = new EmotionMarker("", 0, 0, 0, 0, false, false, true, true)
		{
			intensity = setup.defaultPhonemeIntensity,
			continuousVariation = setup.defaultContinuousVariation,
			variationFrequency = setup.defaultVariationFrequency,
			intensityVariation = setup.defaultIntensityVariation,
			blendableVariation = setup.defaultBlendableVariation,
			bonePositionVariation = setup.defaultBonePositionVariation,
			boneRotationVariation = setup.defaultBoneRotationVariation,
		};

		var clipItem = window.AddLipSyncClip(window.convertedSetupData);
		clipItem.isLocked = true;

		for (int i = 0; i < modules.Length; i++)
		{
			window.AddModule(modules[i]);
		}

		window.Show(parent);
		return window;
	}

	[MenuItem("Window/Rogo Digital/LipSync Pro/AutoSync", false, 12)]
	private static void OpenFromMenu()
	{
		var clipSetup = LipSyncClipSetup.ShowWindow();
		CreateWindow(clipSetup, clipSetup, 1);
	}

	private void BeginPad(int pixels)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Space(pixels);
		GUILayout.BeginVertical();
	}

	private void EndPad(int pixels)
	{
		GUILayout.EndVertical();
		GUILayout.Space(pixels);
		GUILayout.EndHorizontal();
	}

	private class ClipItem
	{
		public AudioClip audioClip;
		public LipSyncData lipSyncClip;

		public bool useLSD;
		public OutputMode outputMode;
		public string outputPath;

		public bool isLocked;

		public LipSyncData outputClip;
		public bool hasBeenProcessed;
		public LipSyncData[] appendedClips = new LipSyncData[0];
	}

	private enum OutputMode
	{
		ToClipEditor,
		ToLipSyncData,
		ToXML,
		AppendToPrevious,
	}
}