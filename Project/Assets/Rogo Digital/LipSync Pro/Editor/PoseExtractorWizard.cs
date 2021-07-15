using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

using RogoDigital;
using RogoDigital.Lipsync;

public class PoseExtractorWizard : WizardWindow {

	private Vector2 scrollPosition;
	private AvatarMask mask;

	private string identifier;
	private Transform target;
	private Shape pose;
	private AnimationClip animClip;

	private int currentFrame;

	private string[] transformPaths;
	private Transform[] transformReferences;
	private bool[] transformsToUse;
	private Vector3[] startPositions;
	private Quaternion[] startRotations;

	private bool dontReset;
	private string errorText = "";

	public override void OnWizardGUI () {
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Creating Pose for " + identifier, EditorStyles.boldLabel);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(10);

		switch (currentStep) {
			case 1:
				EditorGUI.BeginChangeCheck();
				animClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animClip, typeof(AnimationClip), false);
				if (EditorGUI.EndChangeCheck()) {
					RefreshInfo();
				}
				if(errorText != "") {
					EditorGUILayout.HelpBox(errorText, MessageType.Error);
				}
				GUILayout.Space(10);

				if (animClip != null) {
					if (transformPaths.Length > 0) {
						EditorGUI.BeginDisabledGroup(animClip.legacy);
						EditorGUI.BeginChangeCheck();
						mask = (AvatarMask)EditorGUILayout.ObjectField("Use Avatar Mask", mask, typeof(AvatarMask), false);
						if (EditorGUI.EndChangeCheck()) {
							if (mask != null) {
								for (int mTransform = 0; mTransform < mask.transformCount; mTransform++) {
									for (int b = 0; b < transformPaths.Length; b++) {
										if (transformPaths[b] == mask.GetTransformPath(mTransform)) {
											transformsToUse[b] = mask.GetTransformActive(mTransform);
										}
									}
								}
							}
						}
						EditorGUI.EndDisabledGroup();

						if (animClip.legacy) {
							EditorGUILayout.HelpBox("Legacy Animations are not compatible with AvatarMasks", MessageType.Info);
						}
						scrollPosition = GUILayout.BeginScrollView(scrollPosition);
						GUILayout.BeginHorizontal();
						GUILayout.Space(10);
						bool check = AllEnabled();
						EditorGUI.BeginChangeCheck();
						EditorGUI.BeginDisabledGroup(mask != null);
						check = GUILayout.Toggle(check, "");
						EditorGUI.EndDisabledGroup();
						if (EditorGUI.EndChangeCheck()) {
							SetAll(check);
						}
						GUILayout.Label("Stored Transform Path");
						GUILayout.FlexibleSpace();
						GUILayout.Label("Transform Reference");
						GUILayout.Space(10);
						GUILayout.EndHorizontal();

						if (transformPaths.Length > 0) {
							canContinue = true;
						}
						bool allFalse = true;
						for (int b = 0; b < transformPaths.Length; b++) {
							Rect bar = EditorGUILayout.BeginHorizontal();
							if (transformReferences[b] == null && transformsToUse[b]) {
								GUI.Box(new Rect(bar.x + 5, bar.y, bar.width - 10, bar.height), "", (GUIStyle)"TL SelectionButton PreDropGlow");
								canContinue = false;
							}
							GUILayout.Space(10);
							EditorGUI.BeginDisabledGroup(mask != null);
							transformsToUse[b] = GUILayout.Toggle(transformsToUse[b], "");
							if (transformsToUse[b] == true) allFalse = false;
							EditorGUI.EndDisabledGroup();
							int trimPoint = 0;
							if (transformPaths[b].Length > 28) {
								trimPoint = transformPaths[b].Length - 25;
							}
							EditorGUI.BeginDisabledGroup(!transformsToUse[b]);
							GUILayout.Label(new GUIContent("..." + transformPaths[b].Substring(trimPoint), transformPaths[b]));
							GUILayout.FlexibleSpace();
							transformReferences[b] = (Transform)EditorGUILayout.ObjectField(transformReferences[b], typeof(Transform), true, GUILayout.MaxWidth(160));
							EditorGUI.EndDisabledGroup();
							GUILayout.Space(10);
							EditorGUILayout.EndHorizontal();
						}
						if (allFalse) canContinue = false;
						GUILayout.EndScrollView();
					} else {
						EditorGUILayout.HelpBox("No usable transform curves found in the clip.", MessageType.Error);
					}
				} else {
					GUILayout.Label("No Clip Selected.", EditorStyles.centeredGreyMiniLabel);
				}

				break;
			case 2:
				EditorGUI.BeginDisabledGroup(true);
				animClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animClip, typeof(AnimationClip), false);
				EditorGUI.EndDisabledGroup();
				GUILayout.Space(10);
				EditorGUI.BeginChangeCheck();
				currentFrame = Mathf.Clamp(EditorGUILayout.IntSlider("Animation Frame", currentFrame, 0, (int)(animClip.length * animClip.frameRate)), 0, (int)(animClip.length * animClip.frameRate));
				if (EditorGUI.EndChangeCheck()) {
					animClip.SampleAnimation(target.gameObject, (float)currentFrame / animClip.frameRate);
				}
				GUILayout.Space(5);
				scrollPosition = GUILayout.BeginScrollView(scrollPosition);
				for (int b = 0; b < transformPaths.Length; b++) {
					if (transformsToUse[b]) {
						GUILayout.BeginHorizontal();
						GUILayout.Space(10);
						GUILayout.Label(transformReferences[b].name, GUILayout.MaxWidth(160));
						GUILayout.FlexibleSpace();
						GUILayout.Label(transformReferences[b].position.ToString());
						GUILayout.Space(5);
						GUILayout.Label(transformReferences[b].eulerAngles.ToString());
						GUILayout.Space(10);
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndScrollView();
				break;
		}
	}

	public override void OnBackPressed () {
		switch (currentStep) {
			case 2:
				for (int b = 0; b < transformPaths.Length; b++) {
					if (transformsToUse[b] && transformReferences[b] != null) {
						transformReferences[b].localPosition = startPositions[b];
						transformReferences[b].localRotation = startRotations[b];
					}
				}
				scrollPosition = Vector2.zero;
				break;
		}
	}

	public override void OnContinuePressed () {
		switch (currentStep) {
			case 1:
				scrollPosition = Vector2.zero;
				currentFrame = 0;
				startPositions = new Vector3[transformReferences.Length];
				startRotations = new Quaternion[transformReferences.Length];

				for (int b = 0; b < transformPaths.Length; b++) {
					if (transformsToUse[b] && transformReferences[b] != null) {
						startPositions[b] = transformReferences[b].localPosition;
						startRotations[b] = transformReferences[b].localRotation;
					}
				}

				animClip.SampleAnimation(target.gameObject, (float)currentFrame / animClip.frameRate);
				break;
			case 2:
				List<BoneShape> boneShapes = new List<BoneShape>();
				for (int b = 0; b < transformPaths.Length; b++) {
					if (transformsToUse[b] && transformReferences[b] != null) {
						BoneShape bone = new BoneShape(transformReferences[b], transformReferences[b].localPosition, transformReferences[b].localEulerAngles);

						bone.neutralPosition = startPositions[b];
						bone.neutralRotation = startRotations[b].eulerAngles;
						boneShapes.Add(bone);
					}
				}

				pose.bones = boneShapes;

				dontReset = true;
				break;
		}
	}

	void OnDestroy () {
		if (startPositions != null && !dontReset) {
			for (int b = 0; b < transformPaths.Length; b++) {
				if (transformsToUse[b] && transformReferences[b] != null) {
					transformReferences[b].localPosition = startPositions[b];
					transformReferences[b].localRotation = startRotations[b];
				}
			}
		}
	}

	bool AllEnabled () {
		bool result = true;
		for (int b = 0; b < transformsToUse.Length; b++) {
			if (!transformsToUse[b])
				result = false;
		}

		return result;
	}

	void SetAll (bool setting) {
		for (int b = 0; b < transformsToUse.Length; b++) {
			transformsToUse[b] = setting;
		}
	}

	void RefreshInfo () {
		errorText = "";

		if (animClip == null) {
			canContinue = false;
		} else {
			// Account for import modes
			if(animClip.isHumanMotion) {
				
			}

			EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(animClip);
			List<string> finalBindings = new List<string>();

			for (int b = 0; b < bindings.Length; b++) {
				Debug.LogFormat("Binding: {0}", bindings[b].propertyName);
				if (!string.IsNullOrEmpty(bindings[b].path)) {
					if (!finalBindings.Contains(bindings[b].path)) {
						finalBindings.Add(bindings[b].path);
					}
				}
			}

			transformPaths = new string[finalBindings.Count];
			transformReferences = new Transform[finalBindings.Count];
			transformsToUse = new bool[finalBindings.Count];

			if (finalBindings.Count > 0) {
				for (int b = 0; b < finalBindings.Count; b++) {
					transformPaths[b] = finalBindings[b];
					transformsToUse[b] = true;

					// Attempt to find transforms from path
					transformReferences[b] = target.Find(transformPaths[b]);
				}

				canContinue = true;
			} else {
				canContinue = false;
			}
		}
	}

	public static void ShowWindow (Transform target, Shape shape, string identifer) {
		PoseExtractorWizard window = EditorWindow.GetWindow<PoseExtractorWizard>(true);
		window.topMessage = "Creates a Pose from an AnimationClip.";
		window.totalSteps = 2;
		window.Focus();

		window.titleContent = new GUIContent("Pose Extractor Wizard");
		window.pose = shape;
		window.target = target;
		window.identifier = identifer;

		window.canContinue = false;
	}
}
