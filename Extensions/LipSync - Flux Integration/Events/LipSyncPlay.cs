using UnityEngine;
using System.Collections;
using Flux;

namespace RogoDigital.Lipsync.Extensions.Flux {
	/// <summary>
	/// Plays a LipSyncData file on an actor.
	/// </summary>
	[FEvent("Rogo Digital/LipSync Pro/Play Animation")]
	public class LipSyncPlay : FEvent {

		/// <summary>
		/// The LipSync character to play on
		/// </summary>
		public LipSync character;

		/// <summary>
		/// The LipSyncData clip to be played.
		/// </summary>
		public LipSyncData lipSyncClip;

		/// <summary>
		/// The delay, in seconds, before the clip starts playing.
		/// </summary>
		public float delay = 0;

		protected override void OnTrigger(float timeSinceTrigger) {
			if(character != null) {
				character.Play(lipSyncClip , delay);
			}else{
				Debug.Log("LipSync.Play Event in Flux called with no character to play on.");
			}
		}
	}
}