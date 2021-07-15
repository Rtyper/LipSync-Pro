using UnityEngine;
using System.Collections;
using Flux;

namespace RogoDigital.Lipsync.Extensions.Flux {
	/// <summary>
	/// Stops a LipSyncData file on an actor.
	/// </summary>
	[FEvent("Rogo Digital/LipSync Pro/Resume Animation")]
	public class LipSyncStop : FEvent {

		/// <summary>
		/// The LipSync character to play on
		/// </summary>
		public LipSync character;

		/// <summary>
		/// Whether to stop the audio as well as the animation.
		/// </summary>
		public bool stopAudio = true;

		protected override void OnTrigger(float timeSinceTrigger) {
			if(character != null) {
				character.Stop(stopAudio);
			}else{
				Debug.Log("LipSync.Stop Event in Flux called with no character to play on.");
			}
		}
	}
}