using UnityEngine;
using System.Collections;
using CinemaDirector;

namespace RogoDigital.Lipsync.Extensions.CinemaDirector{
	/// <summary>
	/// Resumes a LipSyncData file on an actor.
	/// </summary>
	[CutsceneItemAttribute("Rogo Digital LipSync", "Resume", CutsceneItemGenre.ActorItem)]
	public class LipSyncResume : CinemaActorEvent {

		public override void Trigger (GameObject target) {
			if(target != null) {
				LipSync character = target.GetComponent<LipSync>();
				if(character != null) {
					character.Resume();
				}else{
					Debug.Log("LipSync.Resume Event in Cinema Director called on Actor with no LipSync component.");
				}
			}else{
				Debug.Log("LipSync.Resume Event in Cinema Director called with no Actor to play on.");
			}
		}
	}
}