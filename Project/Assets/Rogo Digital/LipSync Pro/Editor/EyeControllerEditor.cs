using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using RogoDigital;
using RogoDigital.Lipsync;
using System.Collections.Generic;

[CustomEditor(typeof(EyeController)), CanEditMultipleObjects]
public class EyeControllerEditor : Editor {
	private EyeController myTarget;

	private Texture2D logo;
	private BlendSystemEditor blendSystemEditor;
	private SerializedProperty boneUpdateAnimation;

	// Blinking
	private AnimBool showBlinking;
	private AnimBool showBlinkingClassicControl;

	private SerializedProperty blinkingEnabled;
	private SerializedProperty blinkingControlMode;

	private SerializedProperty leftEyeBlinkBlendshape;
	private SerializedProperty rightEyeBlinkBlendshape;

	private SerializedProperty minimumBlinkGap;
	private SerializedProperty maximumBlinkGap;
	private SerializedProperty blinkDuration;

	// Looking Shared
	private AnimBool showLookShared;
	private AnimBool showLookingClassicControl;

	private SerializedProperty leftEyeLookAtBone;
	private SerializedProperty rightEyeLookAtBone;

	private SerializedProperty eyeRotationRangeX;
	private SerializedProperty eyeRotationRangeY;
	private SerializedProperty eyeLookOffset;
	private SerializedProperty eyeForwardAxis;
	private SerializedProperty eyeTurnSpeed;

	// Random Looking
	private AnimBool showRandomLook;

	private SerializedProperty randomLookingEnabled;
	private SerializedProperty lookingControlMode;

	private SerializedProperty minimumChangeDirectionGap;
	private SerializedProperty maximumChangeDirectionGap;

	// Look Target
	private AnimBool showLookTarget;

	private SerializedProperty targetEnabled;

	private SerializedProperty viewTarget;
	private SerializedProperty targetWeight;

	private SerializedProperty autoTarget;
	private AnimBool showAutoTarget;
	private SerializedProperty autoTargetTag;
	private SerializedProperty autoTargetDistance;

	private int blendSystemNumber = 0;
	private List<System.Type> blendSystems;
	private List<string> blendSystemNames;

	private GUIStyle inlineToolbar;

	private string[] blendables;

	void OnEnable () {
		if (!EditorGUIUtility.isProSkin) {
			logo = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Eye Controller/Light/EyeController_logo.png");
		} else {
			logo = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Eye Controller/Dark/EyeController_logo.png");
		}

		myTarget = (EyeController)target;
		
		blendSystemNumber = BlendSystemEditor.FindBlendSystems(myTarget);

		boneUpdateAnimation = serializedObject.FindProperty("boneUpdateAnimation");

		blinkingEnabled = serializedObject.FindProperty("blinkingEnabled");
		blinkingControlMode = serializedObject.FindProperty("blinkingControlMode");

		leftEyeBlinkBlendshape = serializedObject.FindProperty("leftEyeBlinkBlendable");
		rightEyeBlinkBlendshape = serializedObject.FindProperty("rightEyeBlinkBlendable");

		minimumBlinkGap = serializedObject.FindProperty("minimumBlinkGap");
		maximumBlinkGap = serializedObject.FindProperty("maximumBlinkGap");
		blinkDuration = serializedObject.FindProperty("blinkDuration");

		leftEyeLookAtBone = serializedObject.FindProperty("_leftEyeLookAtBone");
		rightEyeLookAtBone = serializedObject.FindProperty("_rightEyeLookAtBone");
		eyeRotationRangeX = serializedObject.FindProperty("eyeRotationRangeX");
		eyeRotationRangeY = serializedObject.FindProperty("eyeRotationRangeY");

		eyeLookOffset = serializedObject.FindProperty("eyeLookOffset");
		eyeForwardAxis = serializedObject.FindProperty("eyeForwardAxis");
		eyeTurnSpeed = serializedObject.FindProperty("eyeTurnSpeed");

		randomLookingEnabled = serializedObject.FindProperty("randomLookingEnabled");
		lookingControlMode = serializedObject.FindProperty("lookingControlMode");

		minimumChangeDirectionGap = serializedObject.FindProperty("minimumChangeDirectionGap");
		maximumChangeDirectionGap = serializedObject.FindProperty("maximumChangeDirectionGap");

		targetEnabled = serializedObject.FindProperty("targetEnabled");

		viewTarget = serializedObject.FindProperty("viewTarget");
		targetWeight = serializedObject.FindProperty("targetWeight");

		autoTarget = serializedObject.FindProperty("autoTarget");
		autoTargetTag = serializedObject.FindProperty("autoTargetTag");
		autoTargetDistance = serializedObject.FindProperty("autoTargetDistance");

		showBlinking = new AnimBool(myTarget.blinkingEnabled, Repaint);
		showRandomLook = new AnimBool(myTarget.randomLookingEnabled, Repaint);
		showLookTarget = new AnimBool(myTarget.targetEnabled, Repaint);
		showAutoTarget = new AnimBool(myTarget.autoTarget, Repaint);
		showLookShared = new AnimBool(myTarget.randomLookingEnabled || myTarget.targetEnabled, Repaint);
		showBlinkingClassicControl = new AnimBool(myTarget.blinkingControlMode == EyeController.ControlMode.Classic, Repaint);
		showLookingClassicControl = new AnimBool(myTarget.lookingControlMode == EyeController.ControlMode.Classic, Repaint);

		if (myTarget.blendSystem != null) {
			if (myTarget.blendSystem.isReady) {
				myTarget.blendSystem.onBlendablesChanged += GetBlendShapes;
				GetBlendShapes();
				BlendSystemEditor.GetBlendSystemButtons(myTarget.blendSystem);
			}
		}
	}

	void OnDisable () {
		if (myTarget.blendSystem != null) {
			if (myTarget.blendSystem.isReady) {
				myTarget.blendSystem.onBlendablesChanged -= GetBlendShapes;

				Shape shape = null;

				switch (LipSyncEditorExtensions.oldToggle) {
					case 0:
						shape = myTarget.blinkingShape;
						break;
					case 1:
						shape = myTarget.lookingUpShape;
						break;
					case 2:
						shape = myTarget.lookingDownShape;
						break;
					case 3:
						shape = myTarget.lookingLeftShape;
						break;
					case 4:
						shape = myTarget.lookingRightShape;
						break;
				}

				if (shape != null) {
					for (int blendable = 0; blendable < shape.weights.Count; blendable++) {
						myTarget.blendSystem.SetBlendableValue(shape.blendShapes[blendable], 0);
					}

					foreach (BoneShape bone in shape.bones) {
						if (bone.bone != null) {
							bone.bone.localPosition = bone.neutralPosition;
							bone.bone.localEulerAngles = bone.neutralRotation;
						}
					}
				}
			}
		}

		LipSyncEditorExtensions.currentToggle = -1;
	}

	public override void OnInspectorGUI () {

		// Create styles if necesarry
		if (inlineToolbar == null) {
			inlineToolbar = new GUIStyle((GUIStyle)"TE toolbarbutton");
			inlineToolbar.fixedHeight = 0;
			inlineToolbar.fixedWidth = 0;
		}

		LipSyncEditorExtensions.BeginPaddedHorizontal();
		GUILayout.Box(logo, GUIStyle.none);
		LipSyncEditorExtensions.EndPaddedHorizontal();

		GUILayout.Space(20);

		serializedObject.Update();

		Rect lineRect;
		EditorGUILayout.HelpBox("Enable or disable Eye Controller functionality below.", MessageType.Info);

		GUILayout.Space(10);

		blendSystemNumber = BlendSystemEditor.DrawBlendSystemEditor(myTarget, blendSystemNumber, "EyeController requires a blend system to function.");

		if (myTarget.blendSystem != null) {
			if (myTarget.blendSystem.isReady) {

				if (blendables == null) {
					myTarget.blendSystem.onBlendablesChanged += GetBlendShapes;
					GetBlendShapes();
				}

				EditorGUILayout.PropertyField(boneUpdateAnimation, new GUIContent("Account for Animation", "If true, will calculate relative bone positions/rotations each frame. Improves results when using animation, but will cause errors when not."));
				GUILayout.Space(10);
				BlendSystemEditor.DrawBlendSystemButtons(myTarget.blendSystem);
				GUILayout.Space(10);

				// Blinking
				lineRect = EditorGUILayout.BeginHorizontal();
				GUI.Box(lineRect, "", (GUIStyle)"flow node 0");
				GUILayout.Space(10);
				blinkingEnabled.boolValue = EditorGUILayout.ToggleLeft("Blinking", blinkingEnabled.boolValue, EditorStyles.largeLabel, GUILayout.ExpandWidth(true), GUILayout.Height(24));
				showBlinking.target = blinkingEnabled.boolValue;
				GUILayout.FlexibleSpace();
				blinkingControlMode.enumValueIndex = GUILayout.Toolbar(blinkingControlMode.enumValueIndex, System.Enum.GetNames(typeof(EyeController.ControlMode)), inlineToolbar, GUILayout.MaxWidth(300), GUILayout.Height(24));
				showBlinkingClassicControl.target = blinkingControlMode.enumValueIndex == (int)EyeController.ControlMode.Classic;
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel++;
				if (LipSyncEditorExtensions.FixedBeginFadeGroup(showBlinking.faded)) {
					GUILayout.Space(5);

					// Classic Mode
					if (LipSyncEditorExtensions.FixedBeginFadeGroup(showBlinkingClassicControl.faded)) {
						if(blendables != null) {
							leftEyeBlinkBlendshape.intValue = EditorGUILayout.Popup("Left Eye Blink Blendshape", leftEyeBlinkBlendshape.intValue, blendables, GUILayout.MaxWidth(500));
							rightEyeBlinkBlendshape.intValue = EditorGUILayout.Popup("Right Eye Blink Blendshape", rightEyeBlinkBlendshape.intValue, blendables, GUILayout.MaxWidth(500));
						}
					}
					LipSyncEditorExtensions.FixedEndFadeGroup(showBlinkingClassicControl.faded);

					// Pose Mode
					if (LipSyncEditorExtensions.FixedBeginFadeGroup(1 - showBlinkingClassicControl.faded)) {
						this.DrawShapeEditor(myTarget.blendSystem, blendables, true, true, myTarget.blinkingShape, "Blinking", 0);
					}
					LipSyncEditorExtensions.FixedEndFadeGroup(1 - showBlinkingClassicControl.faded);

					GUILayout.Space(10);

					float minGap = minimumBlinkGap.floatValue;
					float maxGap = maximumBlinkGap.floatValue;

					MinMaxSliderWithNumbers(new GUIContent("Blink Gap", "Time, in seconds, between blinks."), ref minGap, ref maxGap, 0.1f, 20);

					minimumBlinkGap.floatValue = minGap;
					maximumBlinkGap.floatValue = maxGap;

					EditorGUILayout.PropertyField(blinkDuration, new GUIContent("Blink Duration", "How long each blink takes."));
					GUILayout.Space(10);
				}
				LipSyncEditorExtensions.FixedEndFadeGroup(showBlinking.faded);
				EditorGUI.indentLevel--;
				GUILayout.Space(2);
				// Random Look Direction
				lineRect = EditorGUILayout.BeginHorizontal();
				GUI.Box(lineRect, "", (GUIStyle)"flow node 0");
				GUILayout.Space(10);
				randomLookingEnabled.boolValue = EditorGUILayout.ToggleLeft("Random Looking", randomLookingEnabled.boolValue, EditorStyles.largeLabel, GUILayout.ExpandWidth(true), GUILayout.Height(24));
				showRandomLook.target = randomLookingEnabled.boolValue;
				GUILayout.FlexibleSpace();
				lookingControlMode.enumValueIndex = GUILayout.Toolbar(lookingControlMode.enumValueIndex, System.Enum.GetNames(typeof(EyeController.ControlMode)), inlineToolbar, GUILayout.MaxWidth(300), GUILayout.Height(24));
				showLookingClassicControl.target = lookingControlMode.enumValueIndex == (int)EyeController.ControlMode.Classic;
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel++;
				if (LipSyncEditorExtensions.FixedBeginFadeGroup(showRandomLook.faded)) {
					GUILayout.Space(5);

					float minGap = minimumChangeDirectionGap.floatValue;
					float maxGap = maximumChangeDirectionGap.floatValue;

					MinMaxSliderWithNumbers(new GUIContent("Change Direction Gap", "Time, in seconds, between the eyes turning to a new direction."), ref minGap, ref maxGap, 1f, 30);

					minimumChangeDirectionGap.floatValue = minGap;
					maximumChangeDirectionGap.floatValue = maxGap;

					GUILayout.Space(10);
				}
				LipSyncEditorExtensions.FixedEndFadeGroup(showRandomLook.faded);
				EditorGUI.indentLevel--;

				// Look Targets
				lineRect = EditorGUILayout.BeginHorizontal();
				GUI.Box(lineRect, "", (GUIStyle)"flow node 0");
				GUILayout.Space(10);
				EditorGUI.BeginDisabledGroup(lookingControlMode.enumValueIndex == (int)EyeController.ControlMode.PoseBased);
				targetEnabled.boolValue = EditorGUILayout.ToggleLeft("Look At Target", targetEnabled.boolValue, EditorStyles.largeLabel, GUILayout.ExpandWidth(true), GUILayout.Height(24)) && lookingControlMode.enumValueIndex == (int)EyeController.ControlMode.Classic;
				showLookTarget.target = targetEnabled.boolValue;
				EditorGUI.EndDisabledGroup();
				GUILayout.FlexibleSpace();
				GUILayout.Space(24);
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel++;
				if (LipSyncEditorExtensions.FixedBeginFadeGroup(showLookTarget.faded)) {
					GUILayout.Space(5);
					if (LipSyncEditorExtensions.FixedBeginFadeGroup(1 - showAutoTarget.faded)) {
						EditorGUILayout.PropertyField(viewTarget, new GUIContent("Target", "Transform to look at."));
						GUILayout.Space(10);
					}
					LipSyncEditorExtensions.FixedEndFadeGroup(1 - showAutoTarget.faded);
					EditorGUILayout.PropertyField(autoTarget, new GUIContent("Use Auto Target"));
					showAutoTarget.target = myTarget.autoTarget;
					if (LipSyncEditorExtensions.FixedBeginFadeGroup(showAutoTarget.faded)) {
						EditorGUI.indentLevel++;
						EditorGUILayout.PropertyField(autoTargetTag, new GUIContent("Auto Target Tag", "Tag to use when searching for targets."));
						EditorGUILayout.PropertyField(autoTargetDistance, new GUIContent("Auto Target Distance", "The maximum distance between a target and the character for it to be targeted."));
						EditorGUI.indentLevel--;
						GUILayout.Space(10);
					}
					LipSyncEditorExtensions.FixedEndFadeGroup(showAutoTarget.faded);
					EditorGUILayout.Slider(targetWeight, 0, 1, new GUIContent("Look At Amount"));
					GUILayout.Space(10);
				}
				LipSyncEditorExtensions.FixedEndFadeGroup(showLookTarget.faded);
				if (lookingControlMode.enumValueIndex == (int)EyeController.ControlMode.PoseBased) {
					EditorGUILayout.HelpBox("Targeting is only available in classic mode.", MessageType.Warning);
				}
				EditorGUI.indentLevel--;

				// Shared Look Controls
				GUILayout.Space(-2);
				lineRect = EditorGUILayout.BeginHorizontal();
				GUI.Box(lineRect, "", (GUIStyle)"flow node 0");
				GUILayout.Space(24);
				GUILayout.Label("Looking (Shared)", EditorStyles.largeLabel, GUILayout.ExpandWidth(true), GUILayout.Height(24));
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				showLookShared.target = myTarget.targetEnabled || myTarget.randomLookingEnabled;
				EditorGUI.indentLevel++;
				if (LipSyncEditorExtensions.FixedBeginFadeGroup(showLookShared.faded)) {
					GUILayout.Space(5);

					// Classic Mode
					if (LipSyncEditorExtensions.FixedBeginFadeGroup(showLookingClassicControl.faded)) {
						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField(leftEyeLookAtBone);
						EditorGUILayout.PropertyField(rightEyeLookAtBone);
						if (EditorGUI.EndChangeCheck()) {
							myTarget.LeftEyeLookAtBone = (Transform)leftEyeLookAtBone.objectReferenceValue;
							myTarget.RightEyeLookAtBone = (Transform)rightEyeLookAtBone.objectReferenceValue;
						}
						GUILayout.Space(5);
						MinMaxSliderWithNumbers(new GUIContent("X Axis Range"), eyeRotationRangeX, -90, 90);
						MinMaxSliderWithNumbers(new GUIContent("Y Axis Range"), eyeRotationRangeY, -90, 90);
						GUILayout.Space(5);
						EditorGUILayout.PropertyField(eyeLookOffset);
						EditorGUILayout.PropertyField(eyeForwardAxis);
					}
					LipSyncEditorExtensions.FixedEndFadeGroup(showLookingClassicControl.faded);

					// Pose Mode
					if (LipSyncEditorExtensions.FixedBeginFadeGroup(1 - showLookingClassicControl.faded)) {
						this.DrawShapeEditor(myTarget.blendSystem, blendables, true, true, myTarget.lookingUpShape, "Looking Up", 1);
						this.DrawShapeEditor(myTarget.blendSystem, blendables, true, true, myTarget.lookingDownShape, "Looking Down", 2);
						this.DrawShapeEditor(myTarget.blendSystem, blendables, true, true, myTarget.lookingLeftShape, "Looking Left", 3);
						this.DrawShapeEditor(myTarget.blendSystem, blendables, true, true, myTarget.lookingRightShape, "Looking Right", 4);
					}
					LipSyncEditorExtensions.FixedEndFadeGroup(1 - showLookingClassicControl.faded);
					GUILayout.Space(5);
					EditorGUILayout.PropertyField(eyeTurnSpeed, new GUIContent("Eye Turn Speed", "The speed at which eyes rotate."));
					GUILayout.Space(10);
				}
				LipSyncEditorExtensions.FixedEndFadeGroup(showLookShared.faded);
				EditorGUI.indentLevel--;
				GUILayout.Space(10);
			}

			if (LipSyncEditorExtensions.oldToggle != LipSyncEditorExtensions.currentToggle && LipSyncEditorExtensions.currentTarget == myTarget) {

				Shape oldShape = null;
				Shape newShape = null;

				switch (LipSyncEditorExtensions.oldToggle) {
					case 0:
						oldShape = myTarget.blinkingShape;
						break;
					case 1:
						oldShape = myTarget.lookingUpShape;
						break;
					case 2:
						oldShape = myTarget.lookingDownShape;
						break;
					case 3:
						oldShape = myTarget.lookingLeftShape;
						break;
					case 4:
						oldShape = myTarget.lookingRightShape;
						break;
				}

				switch (LipSyncEditorExtensions.currentToggle) {
					case 0:
						newShape = myTarget.blinkingShape;
						break;
					case 1:
						newShape = myTarget.lookingUpShape;
						break;
					case 2:
						newShape = myTarget.lookingDownShape;
						break;
					case 3:
						newShape = myTarget.lookingLeftShape;
						break;
					case 4:
						newShape = myTarget.lookingRightShape;
						break;
				}

				if (LipSyncEditorExtensions.oldToggle > -1) {
					if(oldShape != null) {
						foreach (BoneShape boneshape in oldShape.bones) {
							if (boneshape.bone != null) {
								boneshape.bone.localPosition = boneshape.neutralPosition;
								boneshape.bone.localEulerAngles = boneshape.neutralRotation;
							}
						}

						foreach (int blendable in oldShape.blendShapes) {
							myTarget.blendSystem.SetBlendableValue(blendable, 0);
						}
					}
				}

				if (LipSyncEditorExtensions.currentToggle > -1) {
					foreach (BoneShape boneshape in newShape.bones) {
						if (boneshape.bone != null) {
							boneshape.bone.localPosition = boneshape.endPosition;
							boneshape.bone.localEulerAngles = boneshape.endRotation;
						}
					}

					for (int b = 0; b < newShape.blendShapes.Count; b++) {
						myTarget.blendSystem.SetBlendableValue(newShape.blendShapes[b], newShape.weights[b]);
					}
				}

				LipSyncEditorExtensions.oldToggle = LipSyncEditorExtensions.currentToggle;
			}

			if (GUI.changed) {
				if (blendables == null) {
					GetBlendShapes();
				}

				Shape newShape = null;

				switch (LipSyncEditorExtensions.currentToggle) {
					case 0:
						newShape = myTarget.blinkingShape;
						break;
					case 1:
						newShape = myTarget.lookingUpShape;
						break;
					case 2:
						newShape = myTarget.lookingDownShape;
						break;
					case 3:
						newShape = myTarget.lookingLeftShape;
						break;
					case 4:
						newShape = myTarget.lookingRightShape;
						break;
				}

				if (LipSyncEditorExtensions.currentToggle > -1 && LipSyncEditorExtensions.currentTarget == myTarget) {
					for (int b = 0; b < newShape.blendShapes.Count; b++) {
						myTarget.blendSystem.SetBlendableValue(newShape.blendShapes[b], newShape.weights[b]);
					}
				}

				EditorUtility.SetDirty(myTarget);
				serializedObject.SetIsDifferentCacheDirty();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}

	void OnSceneGUI () {

		// Bone Handles
		if (LipSyncEditorExtensions.currentToggle >= 0 && LipSyncEditorExtensions.currentTarget == myTarget) {
			BoneShape bone = null;
			Shape shape = null;

			switch (LipSyncEditorExtensions.currentToggle) {
				case 0:
					shape = myTarget.blinkingShape;
					break;
				case 1:
					shape = myTarget.lookingUpShape;
					break;
				case 2:
					shape = myTarget.lookingDownShape;
					break;
				case 3:
					shape = myTarget.lookingLeftShape;
					break;
				case 4:
					shape = myTarget.lookingRightShape;
					break;
			}

			if (LipSyncEditorExtensions.selectedBone < shape.bones.Count && shape.bones.Count > 0) {
				bone = shape.bones[LipSyncEditorExtensions.selectedBone];
			} else {
				return;
			}

			if (bone.bone == null)
				return;

			if (Tools.current == Tool.Move) {
				Undo.RecordObject(bone.bone, "Move");

				Vector3 change = Handles.PositionHandle(bone.bone.position, bone.bone.rotation);
				if (change != bone.bone.position) {
					bone.bone.position = change;
					bone.endPosition = bone.bone.localPosition;
				}
			} else if (Tools.current == Tool.Rotate) {
				Undo.RecordObject(bone.bone, "Rotate");
				Quaternion change = Handles.RotationHandle(bone.bone.rotation, bone.bone.position);
				if (change != bone.bone.rotation) {
					bone.bone.rotation = change;
					bone.endRotation = bone.bone.localEulerAngles;
				}
			} else if (Tools.current == Tool.Scale) {
				Undo.RecordObject(bone.bone, "Scale");
				Vector3 change = Handles.ScaleHandle(bone.bone.localScale, bone.bone.position, bone.bone.rotation, HandleUtility.GetHandleSize(bone.bone.position));
				if (change != bone.bone.localScale) {
					bone.bone.localScale = change;
				}
			}

		}
	}

	void MinMaxSliderWithNumbers (GUIContent label, SerializedProperty property, float minLimit, float maxLimit) {
		Vector2 val = property.vector2Value;
		MinMaxSliderWithNumbers(label, ref val.x, ref val.y, minLimit, maxLimit);
		property.vector2Value = val;
	}

	void MinMaxSliderWithNumbers (GUIContent label, ref float minValue, ref float maxValue, float minLimit, float maxLimit) {
		GUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel(label);
		minValue = EditorGUILayout.FloatField(minValue, GUILayout.Width(65));
		EditorGUILayout.MinMaxSlider(ref minValue, ref maxValue, minLimit, maxLimit);
		maxValue = EditorGUILayout.FloatField(maxValue, GUILayout.Width(65));
		GUILayout.EndHorizontal();

		minValue = Mathf.Clamp(minValue, minLimit, maxValue);
		maxValue = Mathf.Clamp(maxValue, minValue, maxLimit);
	}

	void GetBlendShapes () {
		if (myTarget.blendSystem.isReady) {
			blendables = myTarget.blendSystem.GetBlendables();
		}
	}
}
