using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RogoDigital.Lipsync
{
	public class BlendshapeBlendSystem : BlendSystem
	{

		/// <summary>
		/// Main Character SkinnedMeshRenderer.
		/// </summary>
		public SkinnedMeshRenderer characterMesh;

		/// <summary>
		/// Any Additional SkinnedMeshRenderers.
		/// </summary>
		public SkinnedMeshRenderer[] optionalOtherMeshes;

		private bool wireframeVisible = true;

		// Do any setup necessary here. BlendSystems run in edit mode as well as play mode, so this will also be called when Unity starts or your scripts recompile.
		// Make sure you call base.OnEnable() here for expected behaviour.
		public override void OnEnable ()
		{
			// Sets info about this blend system for use in the editor.
			blendableDisplayName = "Blend Shape";
			blendableDisplayNamePlural = "Blend Shapes";
			noBlendablesMessage = "Your chosen Skinned Mesh Renderer has no Blend Shapes defined.";
			notReadyMessage = "Skinned Mesh Renderer not set. The Blend Shape BlendSystem requires at least one Skinned Mesh Renderer.";

			base.OnEnable();

#if UNITY_EDITOR
			if (!isReady)
				return;

#if UNITY_5_5_OR_NEWER
			EditorUtility.SetSelectedRenderState(characterMesh, wireframeVisible ? EditorSelectedRenderState.Highlight : EditorSelectedRenderState.Hidden);
			foreach (SkinnedMeshRenderer renderer in optionalOtherMeshes)
			{
				EditorUtility.SetSelectedRenderState(renderer, wireframeVisible ? EditorSelectedRenderState.Highlight : EditorSelectedRenderState.Hidden);
			}
#else
			EditorUtility.SetSelectedWireframeHidden(characterMesh, !wireframeVisible);
			foreach (SkinnedMeshRenderer renderer in optionalOtherMeshes) {
				EditorUtility.SetSelectedWireframeHidden(renderer, !wireframeVisible);
			}
#endif
#endif
		}

		/// <summary>
		/// Sets the value of a blendable.
		/// </summary>
		/// <param name="blendable">Blendable.</param>
		/// <param name="value">Value.</param>
		public override void SetBlendableValue (int blendable, float value)
		{
			if (!isReady || characterMesh == null)
				return;

			characterMesh.SetBlendShapeWeight(blendable, value);
			SetInternalValue(blendable, value);
			foreach (SkinnedMeshRenderer renderer in optionalOtherMeshes)
			{
				if (blendable < renderer.sharedMesh.blendShapeCount)
					renderer.SetBlendShapeWeight(blendable, value);
			}
		}

		public override string[] GetBlendables ()
		{
			if (!isReady || characterMesh == null)
				return null;

			bool setInternal = false;
			string[] blendShapes = new string[characterMesh.sharedMesh.blendShapeCount];
			if (blendableCount == 0)
				setInternal = true;

			for (int a = 0; a < blendShapes.Length; a++)
			{
				blendShapes[a] = characterMesh.sharedMesh.GetBlendShapeName(a) + " (" + a.ToString() + ")";
				if (setInternal)
					AddBlendable(a, characterMesh.GetBlendShapeWeight(a));
			}

			return blendShapes;
		}

		public override void OnVariableChanged ()
		{
			if (characterMesh != null)
			{
				isReady = true;
			}
			else
			{
				isReady = false;
			}
		}

		//Editor Buttons
		[BlendSystemButton("Toggle Wireframe")]
		public void ToggleWireframe ()
		{
			if (characterMesh != null)
			{
				wireframeVisible = !wireframeVisible;
#if UNITY_EDITOR
#if UNITY_5_5_OR_NEWER
				EditorUtility.SetSelectedRenderState(characterMesh, wireframeVisible ? EditorSelectedRenderState.Highlight : EditorSelectedRenderState.Hidden);
				foreach (SkinnedMeshRenderer renderer in optionalOtherMeshes)
				{
					EditorUtility.SetSelectedRenderState(renderer, wireframeVisible ? EditorSelectedRenderState.Highlight : EditorSelectedRenderState.Hidden);
				}
#else
				EditorUtility.SetSelectedWireframeHidden(characterMesh, !wireframeVisible);
				foreach (SkinnedMeshRenderer renderer in optionalOtherMeshes) {
					EditorUtility.SetSelectedWireframeHidden(renderer, !wireframeVisible);
				}
#endif
#endif
			}
		}
	}
}