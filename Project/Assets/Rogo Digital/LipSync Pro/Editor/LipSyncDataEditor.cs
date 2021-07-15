using UnityEngine;
using UnityEditor;
using RogoDigital.Lipsync;
using RogoDigital;

[CustomEditor(typeof(LipSyncData))]
public class LipSyncDataEditor : Editor
{
	private LipSyncData lsdTarget;
	private float prevTime;

	private string infoString;
	private float oldSeekPosition;
	private float seekPosition;
	private bool isPlaying = false;

	private bool visualPreview = false;
	private LipSync previewTarget = null;
	private bool previewing = false;
	private float stopTimer = 0;
	private float resetTime = 0;

	private Texture2D playhead_top;
	private Texture2D playhead_line;
	private Texture2D playhead_bottom;
	private Texture2D playIcon;
	private Texture2D stopIcon;
	private Texture2D pauseIcon;
	private Texture2D previewIcon;

	void OnEnable()
	{
		lsdTarget = (LipSyncData)target;

		playhead_top = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_top.png");
		playhead_line = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_middle.png");
		playhead_bottom = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Playhead_bottom.png");

		playIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/play.png");
		stopIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/stop.png");
		pauseIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/pause.png");
		previewIcon = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/Dark/eye.png");

		infoString = lsdTarget.name + " LipSyncData.\nCreated with LipSync Pro " + lsdTarget.version + "\n\n";
		if (lsdTarget.clip) infoString += "AudioClip: " + lsdTarget.clip.name + ".\n";
		infoString += "Length: " + lsdTarget.length + ".\nData: ";
		if (lsdTarget.phonemeData.Length > 0)
		{
			infoString += lsdTarget.phonemeData.Length + " phonemes";
			if (lsdTarget.emotionData.Length > 0) infoString += ", ";
		}

		if (lsdTarget.emotionData.Length > 0)
		{
			infoString += lsdTarget.emotionData.Length + " emotions";
			if (lsdTarget.gestureData.Length > 0) infoString += ", ";
		}

		if (lsdTarget.gestureData.Length > 0)
		{
			infoString += lsdTarget.gestureData.Length + " gestures";
		}

		infoString += ".";
	}

	void OnDisable()
	{
		if (previewTarget != null)
		{
			UpdatePreview(0);
			previewTarget = null;
		}

		if (lsdTarget.clip && isPlaying)
		{
			AudioUtility.StopClip(lsdTarget.clip);
		}
	}

	public override bool RequiresConstantRepaint()
	{
		if (isPlaying || previewing)
		{
			return true;
		}
		return false;
	}

	public override bool HasPreviewGUI()
	{
		return true;
	}

	public override void OnPreviewSettings()
	{
		if (isPlaying)
		{
			if (GUILayout.Button(pauseIcon, (GUIStyle)"PreButton", GUILayout.Width(50)))
			{
				isPlaying = false;

				if (lsdTarget.clip)
				{
					AudioUtility.PauseClip(lsdTarget.clip);
				}
			}
		}
		else
		{
			if (GUILayout.Button(playIcon, (GUIStyle)"PreButton", GUILayout.Width(50)))
			{
				isPlaying = true;

				if (lsdTarget.clip)
				{
					AudioUtility.PlayClip(lsdTarget.clip);
				}
			}
		}
		if (GUILayout.Button(stopIcon, (GUIStyle)"PreButton", GUILayout.Width(50)))
		{
			isPlaying = false;
			seekPosition = 0;
			oldSeekPosition = 0;

			if (lsdTarget.clip)
			{
				AudioUtility.StopClip(lsdTarget.clip);
			}
		}
		GUILayout.Space(25);
		bool newPreviewState = GUILayout.Toggle(visualPreview, new GUIContent(previewIcon, "Preview Animation"), (GUIStyle)"PreButton", GUILayout.Width(50));
		if (visualPreview != newPreviewState)
		{
			GenericMenu previewMenu = new GenericMenu();

			previewMenu.AddDisabledItem(new GUIContent("Choose a target"));

			LipSync[] targets = GameObject.FindObjectsOfType<LipSync>();

			previewMenu.AddItem(new GUIContent("No Preview"), !visualPreview, TargetChosen, null);
			foreach (LipSync t in targets)
			{
				previewMenu.AddItem(new GUIContent(t.name), previewTarget == t ? true : false, TargetChosen, t);
			}

			previewMenu.ShowAsContext();
		}
	}

	void TargetChosen(object data)
	{
		if (data != null)
		{
			visualPreview = true;
			previewTarget = (LipSync)data;

			previewTarget.TempLoad(lsdTarget.phonemeData, lsdTarget.emotionData, lsdTarget.clip, lsdTarget.length);
			previewTarget.ProcessData();
		}
		else
		{
			visualPreview = false;
		}
	}

	public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
	{
		if (lsdTarget.length == 0) lsdTarget.length = lsdTarget.clip.length;
		if (lsdTarget.clip != null) AudioUtility.DrawWaveForm(lsdTarget.clip, 0, new Rect(0, r.y + 3, EditorGUIUtility.currentViewWidth, r.height));

		//Playhead
		if (Event.current.button != 1)
		{
			seekPosition = GUI.HorizontalSlider(new Rect(0, r.y + 3, EditorGUIUtility.currentViewWidth, r.height), seekPosition, 0, 1, GUIStyle.none, GUIStyle.none);
		}

		GUI.DrawTexture(new Rect((seekPosition * EditorGUIUtility.currentViewWidth) - 3, r.y, 7, r.height), playhead_line);
		GUI.DrawTexture(new Rect((seekPosition * EditorGUIUtility.currentViewWidth) - 7, r.y, 15, 15), playhead_top);
		GUI.DrawTexture(new Rect((seekPosition * EditorGUIUtility.currentViewWidth) - 7, r.y + r.height - 16, 15, 15), playhead_bottom);

		if (Event.current.type == EventType.Repaint) LipSyncEditorExtensions.DrawTimeline(r.y, 0, lsdTarget.length, r.width);

		if (visualPreview && previewTarget != null)
		{
			EditorGUI.HelpBox(new Rect(20, r.y + r.height - 45, r.width - 40, 25), "Preview mode active. Note: only Phonemes and Emotions will be shown in the preview.", MessageType.Info);
		}
		else if (previewTarget != null)
		{
			UpdatePreview(0);
			previewTarget = null;
		}
	}

	public override void OnInspectorGUI()
	{
		GUILayout.Space(10);
		EditorGUILayout.HelpBox(infoString, MessageType.Info);
		GUILayout.Space(10);
		if (GUILayout.Button("Edit LipSync Data", GUILayout.Height(50)))
		{
			LipSyncClipSetup.ShowWindow(AssetDatabase.GetAssetPath(target), false);
		}
		GUILayout.Space(10);
		if (lsdTarget.isPreprocessed)
		{
			EditorGUILayout.HelpBox("This clip has been preprocessed for a specific LipSync component. This is good for performance, but may cause the clip to play back incorrectly on other characters, or not at all. \nYou can remove this pre-processing data if you'd like the clip to be completely retargetable.", MessageType.Warning);
			GUI.color = Color.red;
			if (GUILayout.Button("Remove Pre-processed Data", GUILayout.Height(30)))
			{
				if (EditorUtility.DisplayDialog("Delete Data", "Are you sure? Undoing this action will required pre-processing again from the Preprocessor window.", "Yes", "No"))
				{
					serializedObject.Update();
					serializedObject.FindProperty("isPreprocessed").boolValue = false;
					serializedObject.ApplyModifiedProperties();
				}
			}
			GUI.color = Color.white;
		}

		Update();
	}

	void Update()
	{
		float deltaTime = Time.realtimeSinceStartup - prevTime;
		prevTime = Time.realtimeSinceStartup;

		if (seekPosition != oldSeekPosition)
		{
			oldSeekPosition = seekPosition;

			if (!isPlaying)
			{
				if (!previewing && lsdTarget.clip != null)
				{
					AudioUtility.PlayClip(lsdTarget.clip);
				}

				previewing = true;
				stopTimer = 0.1f;
				prevTime = Time.realtimeSinceStartup;
				resetTime = seekPosition;
			}

			if (lsdTarget.clip) AudioUtility.SetClipSamplePosition(lsdTarget.clip, (int)(seekPosition * AudioUtility.GetSampleCount(lsdTarget.clip)));
		}

		if (previewing)
		{
			stopTimer -= deltaTime;
			if (lsdTarget.clip)
			{
				seekPosition = AudioUtility.GetClipPosition(lsdTarget.clip) / lsdTarget.length;
				oldSeekPosition = seekPosition;
			}

			if (stopTimer <= 0)
			{
				previewing = false;
				seekPosition = resetTime;
				oldSeekPosition = seekPosition;
				if (lsdTarget.clip != null)
				{
					AudioUtility.PauseClip(lsdTarget.clip);
					AudioUtility.SetClipSamplePosition(lsdTarget.clip, (int)(seekPosition * AudioUtility.GetSampleCount(lsdTarget.clip)));
				}
			}
		}

		if (isPlaying && lsdTarget.clip == null)
		{
			seekPosition += deltaTime / lsdTarget.length;
			oldSeekPosition = seekPosition;
			if (seekPosition >= 1)
			{
				isPlaying = false;
				seekPosition = 0;
			}
		}
		else if (isPlaying)
		{
			seekPosition = AudioUtility.GetClipPosition(lsdTarget.clip) / lsdTarget.length;
			oldSeekPosition = seekPosition;

			isPlaying = AudioUtility.IsClipPlaying(lsdTarget.clip);
		}

		if (isPlaying && visualPreview || previewing && visualPreview)
		{
			UpdatePreview(seekPosition);
		}
	}

	void UpdatePreview(float time)
	{
		if (previewTarget != null)
		{
			if (previewTarget.blendSystem != null)
			{
				if (previewTarget.blendSystem.isReady)
				{
					previewTarget.PreviewAtTime(time);
					EditorUtility.SetDirty(previewTarget.blendSystem);
				}
			}
		}
	}

	// Double Click to load
	static LipSyncDataEditor()
	{
		EditorApplication.projectWindowItemOnGUI += OnProjectWindowItem;
	}

	static void OnProjectWindowItem(string itemGUID, Rect itemRect)
	{
		if (Selection.activeObject is LipSyncData)
		{
			//Draw project view summary

			// Sanity check - make sure we're dealing with the item we selected. The itemGUID parameter
			// contains the asset GUID of the current item being updated by the project window.
			if (AssetDatabase.GUIDToAssetPath(itemGUID) == AssetDatabase.GetAssetPath(Selection.activeObject))
			{
				if (Event.current.isMouse && Event.current.clickCount == 2)
				{
					LipSyncClipSetup.ShowWindow(AssetDatabase.GUIDToAssetPath(itemGUID), false);

					Event.current.Use();
				}
			}
		}
	}
}