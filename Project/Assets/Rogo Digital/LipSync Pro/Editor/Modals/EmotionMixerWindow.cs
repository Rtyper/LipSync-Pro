using RogoDigital;
using RogoDigital.Lipsync;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class EmotionMixerWindow : ModalWindow {
	EmotionMixer mixer;
	LipSyncProject settings;
	LipSyncClipSetup setup;
	ReorderableList emotionsList;
	GUIStyle centeredStyle;
	int dragging = -1;

	#region GUI Textures
	Texture2D emotionBar;
	#endregion

	void Setup () {
		emotionsList = new ReorderableList(mixer.emotions, typeof(EmotionMixer.EmotionComponent), true, true, true, true);
		emotionsList.drawHeaderCallback = (Rect position) => {
			GUI.Label(position, "Emotions");
		};


		emotionsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
			EmotionMixer.EmotionComponent element = (EmotionMixer.EmotionComponent)emotionsList.list[index];
			rect.y += 2;
			Rect fullRect = rect;
			rect.width *= (Mathf.Round(element.weight * 100) / 100);
			Rect cursorRect = new Rect(rect.x + rect.width - 6, rect.y, 12, rect.height);
			EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.SplitResizeLeftRight);

			Color labelColor = Color.white;
			for (int c = 0; c < settings.emotions.Length; c++) {
				if (settings.emotions[c] == mixer.emotions[index].emotion) {
					GUI.color = settings.emotionColors[c];
					float lum = (0.299f * GUI.color.r + 0.587f * GUI.color.g + 0.114f * GUI.color.b);
					if (lum > 0.5f) labelColor = Color.black;
				}
			}
			GUI.DrawTexture(rect, emotionBar);

			if (Event.current.type == EventType.MouseUp && dragging > -1) {
				dragging = -1;
			}

			if (Event.current.type == EventType.MouseDown && cursorRect.Contains(Event.current.mousePosition) && dragging == -1) {
				dragging = index;
			}

			if (dragging == index) {
				float newValue = GUI.HorizontalSlider(fullRect, element.weight, 0, 1, GUIStyle.none, GUIStyle.none);
				if (newValue != element.weight) {
					mixer.SetWeight(index, newValue);
					mixer.displayColor = Color.black;
					for (int i = 0; i < mixer.emotions.Count; i++) {
						for (int c = 0; c < settings.emotions.Length; c++) {
							if (settings.emotions[c] == mixer.emotions[i].emotion) {
								mixer.displayColor += mixer.emotions[i].weight * settings.emotionColors[c];
							}
						}
					}
				}
			}

			GUI.color = labelColor;
			GUI.Label(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element.emotion + " (" + (Mathf.Round(element.weight * 100) / 100) + ")", centeredStyle);
			GUI.color = Color.white;

		};
		emotionsList.onAddDropdownCallback += (Rect buttonRect, ReorderableList list) => {
			GenericMenu menu = new GenericMenu();
			for (int i = 0; i < settings.emotions.Length; i++) {
				bool exists = false;
				for (int a = 0; a < list.list.Count; a++) {
					if (((EmotionMixer.EmotionComponent)list.list[a]).emotion == settings.emotions[i]) {
						exists = true;
						break;
					}
				}

				if (!exists) {
					menu.AddItem(new GUIContent(settings.emotions[i]), false, (object emotion) => {
						list.list.Add(new EmotionMixer.EmotionComponent((string)emotion, 0f));
						mixer.SetWeight(mixer.emotions.Count - 1, 0.25f);
						mixer.displayColor = Color.black;
						for (int b = 0; b < mixer.emotions.Count; b++) {
							for (int c = 0; c < settings.emotions.Length; c++) {
								if (settings.emotions[c] == mixer.emotions[b].emotion) {
									mixer.displayColor += mixer.emotions[b].weight * settings.emotionColors[c];
								}
							}
						}
					}, settings.emotions[i]);
				} else {
					menu.AddDisabledItem(new GUIContent(settings.emotions[i]));
				}
			}
			menu.DropDown(buttonRect);
		};
		emotionsList.onRemoveCallback += (ReorderableList list) => {
			mixer.SetWeight(emotionsList.index, 0f, true);
			list.list.RemoveAt(emotionsList.index);
			mixer.displayColor = Color.black;
			for (int i = 0; i < mixer.emotions.Count; i++) {
				for (int c = 0; c < settings.emotions.Length; c++) {
					if (settings.emotions[c] == mixer.emotions[i].emotion) {
						mixer.displayColor += mixer.emotions[i].weight * settings.emotionColors[c];
					}
				}
			}
		};
	}

	void OnEnable () {
		emotionBar = (Texture2D)EditorGUIUtility.Load("Rogo Digital/LipSync/emotion-area.png");
	}

	void OnGUI () {
		if (emotionsList == null) {
			Setup();
		}

		if (centeredStyle == null) {
			centeredStyle = new GUIStyle(EditorStyles.whiteLabel);
			centeredStyle.alignment = TextAnchor.MiddleCenter;
		}
		GUILayout.Space(10);
		var oldMixingMode = mixer.mixingMode;
		mixer.mixingMode = (EmotionMixer.MixingMode)EditorGUILayout.EnumPopup("Mixing Mode", mixer.mixingMode);
		if (oldMixingMode != mixer.mixingMode) {
			if (mixer.mixingMode == EmotionMixer.MixingMode.Normal && oldMixingMode != EmotionMixer.MixingMode.Normal) {
				if (EditorUtility.DisplayDialog("Reset Mixer Values?", "Switching to 'Normal' mixing mode will reset emotion percentages.", "Reset", "Cancel")) {
					for (int i = 0; i < mixer.emotions.Count; i++) {
						var em = mixer.emotions[i];
						em.weight = 1f / mixer.emotions.Count;
						mixer.emotions[i] = em;
					}
				} else {
					mixer.mixingMode = oldMixingMode;
				}
			} else if (mixer.mixingMode == EmotionMixer.MixingMode.Additive && oldMixingMode != EmotionMixer.MixingMode.Additive) {
				if (!EditorUtility.DisplayDialog("Unlock Mixer Values?", "Switching to 'Additive' mixing mode will unlock emotion percentages so they can equal more than 100%. You will not be able to switch back to 'Normal' mode without losing these percentages.", "Unlock", "Cancel")) {
					mixer.mixingMode = oldMixingMode;
				}
			}
		}
		EditorGUI.BeginChangeCheck();
		GUILayout.Space(10);
		emotionsList.DoLayoutList();
		if (EditorGUI.EndChangeCheck()) {
			setup.changed = true;
			setup.previewOutOfDate = true;
		}
		GUILayout.Space(10);
		LipSyncEditorExtensions.BeginPaddedHorizontal(20);
		if (GUILayout.Button("Done", GUILayout.MaxWidth(200), GUILayout.Height(30))) {
			Close();
		}
		LipSyncEditorExtensions.EndPaddedHorizontal(20);
	}

	public static void CreateWindow (LipSyncClipSetup setup, EmotionMixer mixer, LipSyncProject settings) {
		Create(setup, mixer, settings);
	}

	private static EmotionMixerWindow Create (LipSyncClipSetup setup, EmotionMixer mixer, LipSyncProject settings) {
		EmotionMixerWindow window = CreateInstance<EmotionMixerWindow>();

		window.position = new Rect(setup.center.x - 250, setup.center.y - 100, 500, 200);
		window.minSize = new Vector2(500, 200);
		window.titleContent = new GUIContent("Emotion Mixer");
		window.mixer = mixer;
		window.settings = settings;
		window.setup = setup;

		window.Setup();
		window.Show(setup);
		return window;
	}
}
