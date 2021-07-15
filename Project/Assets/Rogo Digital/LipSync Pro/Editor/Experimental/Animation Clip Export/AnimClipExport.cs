using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

namespace RogoDigital.Lipsync.Experimental
{
	// Requires: Modified TransformAnimationCurve.cs
	public class AnimClipExport : ModalWindow
	{
		private LipSyncClipSetup setup;

		private LipSync character;
		private float smoothingWeight = 0.3f;

		[InitializeOnLoadMethod]
		public static void Init()
		{
			LipSyncClipSetup.onDrawFileMenu += DrawMenu;
		}

		private static void DrawMenu(LipSyncClipSetup clipEditor, GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Export to AnimClip"), false, () =>
			{
				ShowWindow(clipEditor);
			});
		}

		private void OnGUI()
		{
			GUILayout.Space(10);
			character = (LipSync)EditorGUILayout.ObjectField("LipSync Character", character, typeof(LipSync), true);
			smoothingWeight = EditorGUILayout.Slider(new GUIContent("Smoothing Weight", "Lower values produce smoother animations, 0 being the smoothest and 1 being similar to normal LipSync component playback. Higher values make the animation snappier and more exaggerated."), smoothingWeight, 0, 3);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			if (GUILayout.Button("Export", GUILayout.Height(30)))
			{
				EditorUtility.DisplayProgressBar("Exporting Animation", "", 0.5f);
				character.TempLoad(setup.PhonemeData, setup.EmotionData, setup.Clip, setup.FileLength);
				character.ProcessData();

				List<int> indexBlendables;
				List<AnimationCurve> animCurves;
				List<Transform> bones;
				List<TransformAnimationCurve> boneCurves;
				List<Vector3> boneNeutralPositions;
				List<Quaternion> boneNeutralRotations;
				List<Vector3> boneNeutralScales;

				character.GetCurveDataOut(out indexBlendables, out animCurves, out bones, out boneCurves, out boneNeutralPositions, out boneNeutralRotations, out boneNeutralScales);

				var animClip = new AnimationClip();
				var animClipExportProvider = FindProviderType(character.blendSystem.GetType());

				if (animClipExportProvider == null)
				{
					EditorApplication.Beep();
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Export Failed", "Character's BlendSystem does not support animation clip export. Please consult the documentation for more information.", "OK");
					Close();
					return;
				}
				else
				{
					animClipExportProvider.CreateClipCurves(character.blendSystem, animClip, indexBlendables, animCurves, setup, smoothingWeight);
				}

				// Add bones
				if (bones != null)
				{
					for (int i = 0; i < bones.Count; i++)
					{
						var curves = boneCurves[i].GetAnimationCurves();
						for (int c = 0; c < curves.Length; c++)
						{
							ChangeCurveLength(curves[c], setup.FileLength, smoothingWeight);
						}

						var path = AnimationUtility.CalculateTransformPath(bones[i], character.transform);

						animClip.SetCurve(path, typeof(Transform), "localPosition.x", curves[0]);
						animClip.SetCurve(path, typeof(Transform), "localPosition.y", curves[1]);
						animClip.SetCurve(path, typeof(Transform), "localPosition.z", curves[2]);

						animClip.SetCurve(path, typeof(Transform), "localRotation.x", curves[3]);
						animClip.SetCurve(path, typeof(Transform), "localRotation.y", curves[4]);
						animClip.SetCurve(path, typeof(Transform), "localRotation.z", curves[5]);
						animClip.SetCurve(path, typeof(Transform), "localRotation.w", curves[6]);

						animClip.SetCurve(path, typeof(Transform), "localScale.x", curves[7]);
						animClip.SetCurve(path, typeof(Transform), "localScale.y", curves[8]);
						animClip.SetCurve(path, typeof(Transform), "localScale.z", curves[9]);
					}
				}

				string resultPath = EditorUtility.SaveFilePanelInProject("Save Animation", setup.fileName, "asset", "");
				if (string.IsNullOrEmpty(resultPath))
				{
					EditorUtility.ClearProgressBar();
					return;
				}
				else
				{
					AssetDatabase.CreateAsset(animClip, resultPath);
					AssetDatabase.Refresh();
					EditorUtility.ClearProgressBar();
					EditorUtility.DisplayDialog("Exported Successfully", "Animation exported successfully.", "OK");
					Close();
					return;
				}
			}
			GUILayout.Space(20);
			GUILayout.EndHorizontal();
			GUILayout.Space(10);
		}

		public static void ChangeCurveLength(AnimationCurve curve, float newLength, float smoothingWeight)
		{
			var keys = curve.keys;
			for (int i = 0; i < curve.length; i++)
			{
				keys[i].time = keys[i].time * newLength;
			}
			curve.keys = keys;

			for (int i = 0; i < curve.length; i++)
			{
				curve.SmoothTangents(i, smoothingWeight);
			}
		}

		public IAnimClipExportProvider FindProviderType(Type blendSystemType)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				var types = assemblies[i].GetTypes();
				for (int a = 0; a < types.Length; a++)
				{
					var attributes = types[a].GetCustomAttributes(typeof(AnimClipExportProviderAttribute), true);
					if (attributes.Length > 0)
					{
						if (((AnimClipExportProviderAttribute)attributes[0]).blendSystemType == blendSystemType)
						{
							return (IAnimClipExportProvider)Activator.CreateInstance(types[a]);
						}
					}
				}
			}

			return null;
		}

		public static void ShowWindow(LipSyncClipSetup clipEditor)
		{
			var window = CreateInstance<AnimClipExport>();

			window.position = new Rect(clipEditor.center.x - 150, clipEditor.center.y - 100, 300, 200);
			window.minSize = new Vector2(300, 200);
			window.titleContent = new GUIContent("Export Settings");

			window.setup = clipEditor;
			window.Show(clipEditor);
		}
	}
}