using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RogoDigital.Lipsync.Extensions.Timeline {
	[TrackColor(1f, 0.77f, 0f)]
	[TrackClipType(typeof(LipSyncClip))]
	[TrackBindingType(typeof(LipSync))]
	public class LipSyncTrack : TrackAsset {

		public override Playable CreateTrackMixer (PlayableGraph graph, GameObject go, int inputCount) {
			return ScriptPlayable<LipSyncMixerBehaviour>.Create(graph, inputCount);
		}

		public override void GatherProperties (PlayableDirector director, IPropertyCollector driver) {
#if UNITY_EDITOR
			LipSync character = (LipSync)director.GetGenericBinding(this);
			if (!character || !character.blendSystem) return;

			driver.AddFromComponent(character.gameObject, character.blendSystem);

			if(character.audioSource != null)
				driver.AddFromComponent(character.audioSource.gameObject, character.audioSource);
#endif
			base.GatherProperties(director, driver);
		}
	}
}