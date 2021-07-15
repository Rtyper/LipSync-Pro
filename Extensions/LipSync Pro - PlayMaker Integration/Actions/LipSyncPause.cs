using UnityEngine;
using System.Collections;
using CinemaDirector;

namespace RogoDigital.Lipsync.Extensions.CinemaDirector{
	/// <summary>
	/// Pauses a LipSyncData file on an actor.
	/// </summary>
	[CutsceneItemAttribute("Rogo Digital LipSync", "Pause", CutsceneItemGenre.ActorItem)]
	public class LipSyncPause : CinemaActorEvent {

		public override void Trigger (GameObject target) {
			if(target != null) {
				LipSync character = target.GetComponent<LipSync>();
				if(character != null) {
					character.Pause();
				}else{
					Debug.Log("LipSync.Pause Event in Cinema Director called on Actor with no LipSync component.");
				}
			}else{
				Debug.Log("LipSync.Pause Event in Cinema Director called with no Actor to play on.");
			}
		}
	}
}