using UnityEngine;
using System.Collections;
using Flux;

namespace RogoDigital.Lipsync.Extensions.Flux {
	/// <summary>
	/// Plays a LipSyncData file on an actor, starting at a certain point.
	/// </summary>
	[FEvent("Rogo Digital/LipSync Pro/Play Animation From Time")]
	public class LipSyncPlayFromTime : FEvent {

		/// <summary>
		/// The LipSync character to play on
		/// </summary>
		public LipSync character;

		/// <summary>
		/// The LipSyncData clip to be played.
		/// </summary>
		public LipSyncData lipSyncClip;

		/// <summary>
		/// The time, in seconds, to start the clip at.
		/// </summary>
		public float startTime = 0;

		protected override void OnTrigger(float timeSinceTrigger) {
			if (character != null) {
				character.PlayFromTime(lipSyncClip , startTime);
			}else{
				Debug.Log("LipSync.PlayFromTime Event in Flux called with no character to play on.");
			}
		}
	}
}