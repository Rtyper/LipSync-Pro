using UnityEngine;
using System.Collections;
using CinemaDirector;

namespace RogoDigital.Lipsync.Extensions.CinemaDirector{
	/// <summary>
	/// Stops a LipSyncData file on an actor.
	/// </summary>
	[CutsceneItemAttribute("Rogo Digital LipSync", "Stop", CutsceneItemGenre.ActorItem)]
	public class LipSyncStop : CinemaActorEvent {

		/// <summary>
		/// Whether to stop the audio as well as the animation.
		/// </summary>
		public bool stopAudio = true;

		public override void Trigger (GameObject target) {
			if(target != null) {
				LipSync character = target.GetComponent<LipSync>();
				if(character != null) {
					character.Stop(stopAudio);
				}else{
					Debug.Log("LipSync.Stop Event in Cinema Director called on Actor with no LipSync component.");
				}
			}else{
				Debug.Log("LipSync.Stop Event in Cinema Director called with no Actor to play on.");
			}
		}
	}
}