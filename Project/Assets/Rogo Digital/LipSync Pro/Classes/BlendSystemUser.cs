using UnityEngine;

namespace RogoDigital.Lipsync {
	public class BlendSystemUser : MonoBehaviour {
		/// <summary>
		/// BlendSystem used
		/// </summary>
		public BlendSystem blendSystem;

		protected void OnDestroy () {
			blendSystem.Unregister(this);
		}

		/// <summary>
		/// Used in situations where the BlendSystemUser may have been reset, or the reference to the BlendSystem lost without unregistering.
		/// </summary>
		protected void CleanUpBlendSystems () {
			BlendSystem[] blendSystems = GetComponents<BlendSystem>();
			for (int b = 0; b < blendSystems.Length; b++) {
				if(blendSystems[b].users != null) {
					for (int u = 0; u < blendSystems[b].users.Length; u++) {
						if (blendSystems[b].users[u] == this) blendSystems[b].Unregister(this);
					}
				}
			}
		}

	}
}