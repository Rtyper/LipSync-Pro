using UnityEngine;
using System.Collections;
using Flux;

namespace RogoDigital.Lipsync.Extensions.Flux {
	/// <summary>
	/// Stops a LipSyncData file on an actor.
	/// </summary>
	[FEvent("Rogo Digital/LipSync Pro/Set Emotion")]
	public class LipSyncSetEmotion : FEvent {

		/// <summary>
		/// The LipSync character to play on
		/// </summary>
		public LipSync character;

		/// <summary>
		/// Name of the emotion to blend to
		/// </summary>
		public string emotion;

		/// <summary>
		/// How long, in seconds, the blend-in should take.
		/// </summary>
		public float blendTime = 0.5f;

		protected override void OnTrigger(float timeSinceTrigger) {
			if (character != null) {
				character.SetEmotion(emotion, blendTime);
			} else {
				Debug.Log("LipSync.SetEmotion Event in Flux called with no character to play on.");
			}
		}
	}
}