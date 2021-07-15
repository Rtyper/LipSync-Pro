using UnityEngine;
using System.Collections;
using Flux;

namespace RogoDigital.Lipsync.Extensions.Flux {
	/// <summary>
	/// Resumes a LipSyncData file on an actor.
	/// </summary>
	[FEvent("Rogo Digital/LipSync Pro/Resume Animation")]
	public class LipSyncResume : FEvent {

		/// <summary>
		/// The LipSync character to play on
		/// </summary>
		public LipSync character;

		protected override void OnTrigger(float timeSinceTrigger) {
			if (character != null) {
				character.Resume();
			}else{
				Debug.Log("LipSync.Resume Event in Flux called with no character to play on.");
			}
		}
	}
}