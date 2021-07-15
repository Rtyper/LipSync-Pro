using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RogoDigital.Lipsync.Extensions.Timeline {
	[Serializable]
	public class LipSyncClip : PlayableAsset, ITimelineClipAsset {
		[HideInInspector]
		public LipSyncBehaviour template = new LipSyncBehaviour();
		public LipSyncData clip;

		public ClipCaps clipCaps {
			get {
				return ClipCaps.ClipIn;
			}
		}

		public override double duration {
			get {
				if(!clip) return base.duration;
				return clip.length;
			}
		}

		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner) {
			var playable = ScriptPlayable<LipSyncBehaviour>.Create(graph, template);
			var instance = playable.GetBehaviour();
			instance.clip = clip;

			return playable;
		}
	}

}