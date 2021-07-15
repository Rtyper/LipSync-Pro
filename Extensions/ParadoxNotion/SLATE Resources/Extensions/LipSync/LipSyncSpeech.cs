using UnityEngine;
using System.Collections;
using System.Linq;

using RogoDigital;
using RogoDigital.Lipsync;

namespace Slate.ActionClips.RogoDigitalLipSync{

	[Category("LipSync")]
	[Name("Speech (RogoDigital LipSync)")]
	[Description("Note: The Eyes Look At takes effect only in playmode")]
	[Attachable(typeof(ActorActionTrack))]
	public class LipSyncSpeech : ActorActionClip<LipSync>, ISubClipContainable {

		[SerializeField] [HideInInspector]
		private float _blendIn = 0.2f;
		[SerializeField] [HideInInspector]
		private float _blendOut = 0.2f;
		[SerializeField] [HideInInspector]
		private float _length = 2;

		public LipSyncData lipSyncDataFile;
		public float clipOffset;
		
		public bool useTranscriptForSubtitles;
		[Multiline(5)]
		public string subtitlesText;
		public Color subtitlesColor = Color.white;
		
		public Transform eyesLookTarget;
		public float eyesLookWeight = 0.8f;

		//this is used for audio, subs and look weight
		private EaseType interpolation = EaseType.QuadraticInOut;
		private Transform lastLookTarget;
		private float lastLookWeight;

		public float subClipOffset{
			get {return clipOffset;}
			set {clipOffset = value;}
		}

		public override string info{
			get
			{
				if (!isValid){ return "NO DATA"; }
				return !string.IsNullOrEmpty(subs)? string.Format("<i>'{0}'</i>", subs) : lipSyncDataFile.name;
			}
		}

		public override bool isValid{
			get {return actor != null && lipSyncDataFile != null && lipSyncDataFile.clip != null;}
		}

		public override float length{
			get {return _length;}
			set {_length = value;}
		}

		public override float blendIn{
			get {return _blendIn;}
			set {_blendIn = value;}
		}
		
		public override float blendOut{
			get {return _blendOut;}
			set {_blendOut = value;}
		}

		public float dataLength{
			get {return lipSyncDataFile != null? lipSyncDataFile.length : 1;}
		}

		private EyeController _eyeController;
		private EyeController eyeController{
			get {return _eyeController != null && _eyeController.gameObject == actor.gameObject? _eyeController : _eyeController = actor.GetComponent<EyeController>();}
		}

		private string subs{
			get {return useTranscriptForSubtitles? lipSyncDataFile.transcript : subtitlesText;}
		}


		protected override void OnEnter(){

			if (actor.audioSource == null){
				AudioSampler.GetSourceForID(parent).clip = lipSyncDataFile.clip;
			} else {
				actor.audioSource.clip = lipSyncDataFile.clip;
			}

			actor.TempLoad( lipSyncDataFile.phonemeData, lipSyncDataFile.emotionData, lipSyncDataFile.clip, lipSyncDataFile.clip.length );
			actor.ProcessData();

			if (eyeController != null && eyesLookTarget != null){
				lastLookTarget = eyeController.viewTarget;
				lastLookWeight = eyeController.targetWeight;
				eyeController.viewTarget = eyesLookTarget;
			}
		}

		protected override void OnUpdate(float time, float previousTime){
			
			var weight = GetClipWeight(time);
			var iNorm = Mathf.Repeat(time - subClipOffset, dataLength) / dataLength;
			actor.PreviewAtTime(iNorm);

			if (actor.audioSource == null){
				AudioSampler.SampleForID(parent, lipSyncDataFile.clip, time - subClipOffset, previousTime - subClipOffset, weight);
			} else {
				AudioSampler.Sample(actor.audioSource, lipSyncDataFile.clip, time - subClipOffset, previousTime - subClipOffset, weight);
			}

			if (!string.IsNullOrEmpty(subs)){
				var lerpColor = subtitlesColor;
				lerpColor.a = Easing.Ease(interpolation, 0, 1, weight);
				DirectorGUI.UpdateSubtitles(string.Format("{0}: {1}", actor.name, subs), lerpColor);
			}

			if (eyeController != null && eyesLookTarget != null){
				eyeController.targetWeight = Easing.Ease(interpolation, lastLookWeight, eyesLookWeight, weight);
			}
		}

		protected override void OnExit(){
			
			if (actor.audioSource == null){
				AudioSampler.ReleaseSourceForID(parent);
			} else {
				actor.audioSource.clip = null;
			}

			actor.PreviewAtTime(1);

			if (eyeController != null && eyesLookTarget != null){
				eyeController.viewTarget = lastLookTarget;
				eyeController.targetWeight = lastLookWeight;
			}
		}

		protected override void OnReverse(){

			if (actor.audioSource == null){
				AudioSampler.ReleaseSourceForID(parent);
			} else {
				actor.audioSource.clip = null;
			}

			actor.PreviewAtTime(0);

			if (eyeController != null){
				eyeController.viewTarget = lastLookTarget;
				eyeController.targetWeight = lastLookWeight;
			}
		}

		protected override void OnReverseEnter(){
			actor.TempLoad( lipSyncDataFile.phonemeData, lipSyncDataFile.emotionData, lipSyncDataFile.clip, lipSyncDataFile.clip.length );
			actor.ProcessData();
			if (eyeController != null){
				eyeController.viewTarget = eyesLookTarget;
			}			
		}


		////////////////////////////////////////
		///////////GUI AND EDITOR STUFF/////////
		////////////////////////////////////////
		#if UNITY_EDITOR

		protected override void OnClipGUI(Rect rect){
			if (lipSyncDataFile != null && lipSyncDataFile.clip != null){
				var audioClip = lipSyncDataFile.clip;
				var totalWidth = rect.width;
				var audioRect = rect;
				audioRect.width = (audioClip.length/length) * totalWidth;
				var t = EditorTools.GetAudioClipTexture(audioClip, (int)audioRect.width, (int)audioRect.height);
				if (t != null){
					UnityEditor.Handles.color = new Color(0,0,0,0.2f);
					GUI.color = new Color(0.4f, 0.435f, 0.576f);
					audioRect.yMin += 2;
					audioRect.yMax -= 2;
					for (var f = subClipOffset; f < length; f += audioClip.length){
						audioRect.x = (f/length) * totalWidth;
						rect.x = audioRect.x;
						GUI.DrawTexture(audioRect, t);
						UnityEditor.Handles.DrawLine(new Vector2( rect.x, 0 ), new Vector2( rect.x, rect.height ));
					}
					UnityEditor.Handles.color = Color.white;
					GUI.color = Color.white;
				}
			}
		}			

		#endif

	}
}