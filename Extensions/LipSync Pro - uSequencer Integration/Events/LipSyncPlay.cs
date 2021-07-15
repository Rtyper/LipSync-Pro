using UnityEngine;
using WellFired;

namespace RogoDigital.Lipsync.Extensions.USequencer {
	[USequencerFriendlyName("Play LipSync Clip")]
	[USequencerEvent("LipSync Pro/Play")]
	public class LipSyncPlay : USEventBase {

		/// <summary>
		/// The LipSyncData clip to play.
		/// </summary>
		public LipSyncData clip;

		private LipSync lipSync;

		public void Update () {
			if (clip)
				Duration = clip.length;
		}

		public override void FireEvent () {
			if (lipSync == null) lipSync = AffectedObject.GetComponent<LipSync>();

			if (!clip) {
				Debug.Log("Attempting to play a LipSync clip on a GameObject but you haven't given the event a LipSyncData clip from LipSyncPlay::FireEvent");
				return;
			}


			if (!lipSync) {
				Debug.Log("Attempting to play an LipSync clip on a GameObject without a LipSync Component from LipSyncPlay.FireEvent");
				return;
			}

			if (!Application.isPlaying) {
				lipSync.TempLoad(clip.phonemeData, clip.emotionData, clip.clip, clip.length);
				lipSync.ProcessData();
				return;
			}

			lipSync.Play(clip);
		}

		public override void ProcessEvent (float deltaTime) {
			if (!Application.isPlaying) {
				if(lipSync == null) lipSync = AffectedObject.GetComponent<LipSync>();

				if (!clip) {
					Debug.Log("Attempting to play a LipSync clip on a GameObject but you haven't given the event a LipSyncData clip from LipSyncPlay::FireEvent");
					return;
				}

				
				if (!lipSync) {
					Debug.Log("Attempting to play an LipSync clip on a GameObject without a LipSync Component from LipSyncPlay.FireEvent");
					return;
				}

				lipSync.PreviewAtTime(deltaTime / clip.length);
			}
		}

		public override void PauseEvent () {
			if (!Application.isPlaying) return;
			if (lipSync == null) lipSync = AffectedObject.GetComponent<LipSync>();

			if (!lipSync) {
				return;
			}

			lipSync.Pause();
		}

		public override void ResumeEvent () {
			if (!Application.isPlaying) return;
			if (lipSync == null) lipSync = AffectedObject.GetComponent<LipSync>();

			if (!lipSync) {
				return;
			}

			lipSync.Resume();
		}

		public override void StopEvent () {
			if (lipSync == null) lipSync = AffectedObject.GetComponent<LipSync>();

			if (!lipSync) {
				return;
			}

			if (Application.isPlaying) {
				lipSync.Stop(true);
			} else {
				lipSync.PreviewAtTime(0);
			}
			
		}

		public override void EndEvent () {
			if (lipSync == null) lipSync = AffectedObject.GetComponent<LipSync>();

			if (!lipSync) {
				return;
			}

			if (Application.isPlaying) {
				lipSync.Stop(true);
			} else {
				lipSync.PreviewAtTime(0);
			}
		}

		public override void UndoEvent () {
			if (lipSync == null) lipSync = AffectedObject.GetComponent<LipSync>();

			if (!lipSync) {
				return;
			}

			if (Application.isPlaying) {
				lipSync.Stop(true);
			} else {
				lipSync.PreviewAtTime(0);
			}
		}
	}
}
