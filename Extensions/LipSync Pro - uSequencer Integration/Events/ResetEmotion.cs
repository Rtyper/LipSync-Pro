using UnityEngine;
using WellFired;

namespace RogoDigital.Lipsync.Extensions.USequencer {
	[USequencerFriendlyName("LipSync Reset Emotion")]
	[USequencerEvent("LipSync Pro/Reset Emotion")]
	[USequencerEventHideDuration]
	public class ResetEmotion : USEventBase {

		/// <summary>
		/// The length of time, in seconds, to transition back to neutral
		/// </summary>
		public float blendTime = 1;

		private LipSync lipSync;

		public override void FireEvent () {
			if (!Application.isPlaying) return;
			if (lipSync == null) lipSync = AffectedObject.GetComponent<LipSync>();

			if (!lipSync) {
				Debug.Log("Attempting to play an LipSync clip on a GameObject without a LipSync Component from LipSyncPlay.FireEvent");
				return;
			}

			lipSync.ResetEmotion(blendTime);
		}

		public override void ProcessEvent (float runningTime) {
		}
	}
}
