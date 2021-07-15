using UnityEngine;
using System.Collections;
using Flux;

namespace RogoDigital.Lipsync.Extensions.Flux{
	/// <summary>
	/// Pauses a LipSyncData file on an actor.
	/// </summary>
	[FEvent("Rogo Digital/LipSync Pro/Pause Animation")]
	public class LipSyncPause : FEvent {

		/// <summary>
		/// The LipSync character to play on
		/// </summary>
		public LipSync character;

		protected override void OnTrigger( float timeSinceTrigger ){
			if(character != null) {
				character.Pause();
			}else{
				Debug.Log("LipSync.Pause Event in Flux called with no character to play on.");
			}
		}
	}
}