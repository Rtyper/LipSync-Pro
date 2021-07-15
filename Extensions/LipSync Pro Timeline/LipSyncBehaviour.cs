using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace RogoDigital.Lipsync.Extensions.Timeline {
	[Serializable]
	public class LipSyncBehaviour : PlayableBehaviour {
		public LipSyncData clip;

		public override void OnGraphStart (Playable playable) {
			
		}
	}
}