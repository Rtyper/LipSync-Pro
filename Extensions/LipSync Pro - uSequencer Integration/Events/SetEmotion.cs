using UnityEngine;
using WellFired;

namespace RogoDigital.Lipsync.Extensions.USequencer {
	[USequencerFriendlyName("LipSync Set Emotion")]
	[USequencerEvent("LipSync Pro/Set Emotion")]
	[USequencerEventHideDuration]
	public class SetEmotion : USEventBase {

		/// <summary>
		/// The emotion to blend to
		/// </summary>
		public string emotion;

		/// <summary>
		/// The length of time, in seconds, to transition into the emotion
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

			lipSync.SetEmotion(emotion, blendTime);
		}

		public override void ProcessEvent (float runningTime) {
		}
	}
}
