using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using RogoDigital.Lipsync;

namespace Slate.ActionClips.RogoDigitalLipSync{

	[Category("LipSync")]
	[Name("Emotion (RogoDigital LipSync)")]
	[Description("Set the emotion of a character for a period of time.")]
	public class LipSyncEmotion : ActorActionClip<LipSync> {

		[SerializeField] [HideInInspector]
		private float _length = 2f;
		[SerializeField] [HideInInspector]
		private float _blendIn = 0.5f;
		[SerializeField] [HideInInspector]
		private float _blendOut = 0.5f;

		[AnimatableParameter(0,1)]
		public float weight = 1f;
		[HideInInspector]
		public string emotionName;
		public EaseType interpolation = EaseType.QuadraticInOut;

		private EmotionShape eShape;

		public override string info{
			get {return string.Format("Emotion '{0}'", emotionName);}
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


		protected override void OnEnter(){
			eShape = actor.emotions.Find(e => e.emotion == this.emotionName);
		}

		protected override void OnUpdate(float deltaTime){
			var value = Easing.Ease(interpolation, 0, weight, GetClipWeight(deltaTime));
			SampleEmotion(value);
		}

		protected override void OnReverse(){
			SampleEmotion(0);
		}

		void SampleEmotion(float value){
			if (eShape != null && actor.blendSystem != null){
				var i = 0;
				foreach(var index in eShape.blendShapes){
					var w = eShape.weights[i];
					actor.blendSystem.SetBlendableValue(index, value * w);
					i++;
				}
			}
		}
	}
}