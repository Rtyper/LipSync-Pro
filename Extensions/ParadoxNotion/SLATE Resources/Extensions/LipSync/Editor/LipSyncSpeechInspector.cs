using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Linq;

using RogoDigital.Lipsync;

namespace Slate.ActionClips.RogoDigitalLipSync{

	[CustomEditor(typeof(Slate.ActionClips.RogoDigitalLipSync.LipSyncSpeech))]
	public class LipSyncSpeechInspector : ActionClipInspector<Slate.ActionClips.RogoDigitalLipSync.LipSyncSpeech> {
		public override void OnInspectorGUI(){
			base.OnInspectorGUI();
			if (action.isValid && GUILayout.Button("Set At Clip Length")){
				action.length = action.dataLength;
			}
		}
	}
}