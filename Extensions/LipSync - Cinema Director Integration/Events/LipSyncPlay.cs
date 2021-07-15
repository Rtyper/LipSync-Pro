using UnityEngine;
using System.Collections;
using CinemaDirector;

namespace RogoDigital.Lipsync.Extensions.CinemaDirector{
	/// <summary>
	/// Plays a LipSyncData file on an actor.
	/// </summary>
	[CutsceneItemAttribute("Rogo Digital LipSync", "Play", CutsceneItemGenre.ActorItem)]
	public class LipSyncPlay : CinemaActorEvent {

		/// <summary>
		/// The LipSyncData clip to be played.
		/// </summary>
		public LipSyncData lipSyncClip;

		/// <summary>
		/// The delay, in seconds, before the clip starts playing.
		/// </summary>
		public float delay = 0;

		public override void Trigger (GameObject target) {
			if(target != null) {
				LipSync character = target.GetComponent<LipSync>();
				if(character != null) {
					character.Play(lipSyncClip , delay);
				}else{
					Debug.Log("LipSync.Play Event in Cinema Director called on Actor with no LipSync component.");
				}
			}else{
				Debug.Log("LipSync.Play Event in Cinema Director called with no Actor to play on.");
			}
		}
	}
}