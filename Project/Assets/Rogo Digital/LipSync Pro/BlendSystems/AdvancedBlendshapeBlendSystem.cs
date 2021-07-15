using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RogoDigital.Lipsync {
	[RequireComponent(typeof(BlendshapeManager))]
	public class AdvancedBlendshapeBlendSystem : BlendSystem {
		[SerializeField]
		private BlendshapeManager manager;

		public override void OnEnable () {
			// Sets info about this blend system for use in the editor.
			blendableDisplayName = "Blend Shape";
			blendableDisplayNamePlural = "Blend Shapes";
			noBlendablesMessage = "Your chosen Skinned Mesh Renderer has no Blend Shapes defined.";
			notReadyMessage = "Skinned Mesh Renderer not set. The Blend Shape BlendSystem requires at least one Skinned Mesh Renderer.";

			if (manager == null) {
				if (gameObject.GetComponents<BlendshapeManager>().Length > 1) {
					manager = gameObject.AddComponent<BlendshapeManager>();
				} else {
					manager = gameObject.GetComponent<BlendshapeManager>();
				}
				manager.blendSystem = this;
			} else if (manager.blendSystem == null) {
				manager.blendSystem = this;
			}

			isReady = true;

			base.OnEnable();
		}

		public override string[] GetBlendables () {
			if (!isReady)
				return null;

			bool setInternal = false;
			string[] blendShapes = new string[manager.blendShapes.Length];
			if (blendableCount == 0) setInternal = true;

			for (int a = 0; a < blendShapes.Length; a++) {
				blendShapes[a] = manager.blendShapes[a].name + " (" + a.ToString() + ")";
				float value = 0;
				if(manager.blendShapes[a].mappings.Length > 0) {
					value = manager.blendShapes[a].mappings[0].skinnedMeshRenderer.GetBlendShapeWeight(manager.blendShapes[a].mappings[0].blendShapeIndex);
				}
				if (setInternal) AddBlendable(a, value);
			}

			return blendShapes;
		}

		public override void SetBlendableValue (int blendable, float value) {
			if (!isReady)
				return;

			if (blendable >= manager.blendShapes.Length)
				return;

			if(manager.blendShapes[blendable].mappings != null) {
				for (int i = 0; i < manager.blendShapes[blendable].mappings.Length; i++) {
					SetInternalValue(blendable, value);
					manager.blendShapes[blendable].mappings[i].skinnedMeshRenderer.SetBlendShapeWeight(manager.blendShapes[blendable].mappings[i].blendShapeIndex, value);
				}
			}
		}

	}
}