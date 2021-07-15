using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RogoDigital.Lipsync.Extensions.Timeline {
	public class LipSyncMixerBehaviour : PlayableBehaviour {

		private LipSyncData currentClip = null;
		 
		public override void ProcessFrame (Playable playable, FrameData info, object playerData) {
			LipSync character = (LipSync)playerData;

			if (!character) return;

			int inputCount = playable.GetInputCount();

			for (int i = 0; i < inputCount; i++) {
				ScriptPlayable<LipSyncBehaviour> playableInput = (ScriptPlayable<LipSyncBehaviour>)playable.GetInput(i);
				LipSyncBehaviour input = playableInput.GetBehaviour();
				if (!input.clip) continue;

				if (playableInput.GetPlayState() == PlayState.Playing) {
					// Load clip
					if (input.clip != currentClip) {
						character.TempLoad(input.clip.phonemeData, input.clip.emotionData, input.clip.clip, input.clip.length);
						character.ProcessData();
						currentClip = input.clip;
					}
					float playbackTime = 0;

					if (info.timeHeld) {
						playbackTime = 1;
					} else {
						playbackTime = (float)(playableInput.GetTime() / currentClip.length);
						character.PreviewAtTime(playbackTime);
						if (input.clip.clip && Application.isPlaying && info.evaluationType == FrameData.EvaluationType.Playback) {
							character.PreviewAudioAtTime(playbackTime, (float)(playableInput.GetDuration() - playableInput.GetTime()));
						}
					}

					return;
				}
			}

			// If execution has reached this point, no clip played
			character.PreviewAtTime(0);
		}
	}
}