using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Linq;

using RogoDigital.Lipsync;

namespace Slate.ActionClips.RogoDigitalLipSync{

	[CustomEditor(typeof(Slate.ActionClips.RogoDigitalLipSync.LipSyncEmotion))]
	public class LipSyncEmotionInspector : ActionClipInspector<Slate.ActionClips.RogoDigitalLipSync.LipSyncEmotion> {

		public override void OnInspectorGUI(){
			
			base.ShowCommonInspector();

			var lipsync = action.actor.GetComponent<LipSync>();
			if (lipsync != null){
				var options = lipsync.emotions.Select(e => e.emotion).ToList();
				action.emotionName = EditorTools.Popup<string>("Emotion", action.emotionName, options);
			}

			base.ShowAnimatableParameters();
		}
	}
}