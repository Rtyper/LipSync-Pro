using UnityEngine;
using System.Collections;
using CinemaDirector;

namespace RogoDigital.Lipsync.Extensions.CinemaDirector{
	/// <summary>
	/// Plays a LipSyncData file on an actor, starting at a certain point.
	/// </summary>
	[CutsceneItemAttribute("Rogo Digital LipSync", "Play From Time", CutsceneItemGenre.ActorItem)]
	public class LipSyncPlayFromTime : CinemaActorEvent {

		/// <summary>
		/// The LipSyncData clip to be played.
		/// </summary>
		public LipSyncData lipSyncClip;

		/// <summary>
		/// The time, in seconds, to start the clip at.
		/// </summary>
		public float startTime = 0;

		public override void Trigger (GameObject target) {
			if(target != null) {
				LipSync character = target.GetComponent<LipSync>();
				if(character != null) {
					character.PlayFromTime(lipSyncClip , startTime);
				}else{
					Debug.Log("LipSync.PlayFromTime Event in Cinema Director called on Actor with no LipSync component.");
				}
			}else{
				Debug.Log("LipSync.PlayFromTime Event in Cinema Director called with no Actor to play on.");
			}
		}
	}
}