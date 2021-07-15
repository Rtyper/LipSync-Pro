using UnityEngine;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class EmotionMarker : System.Object {
		[SerializeField]
		public string emotion;
		[SerializeField]
		public bool isMixer;
		[SerializeField]
		public EmotionMixer mixer;
		[SerializeField]
		public float startTime;
		[SerializeField]
		public float endTime;
		[SerializeField]
		public float blendInTime;
		[SerializeField]
		public float blendOutTime;
		[SerializeField]
		public bool blendToMarker;
		[SerializeField]
		public bool blendFromMarker;
		[SerializeField]
		public bool customBlendIn;
		[SerializeField]
		public bool customBlendOut;
		[SerializeField]
		public float intensity = 1;
		[SerializeField]
		public bool continuousVariation = false;
		[SerializeField]
		public float variationFrequency = 0.5f;
		[SerializeField]
		public float intensityVariation = 0.35f;
		[SerializeField]
		public float blendableVariation = 0.1f;
		[SerializeField]
		public float bonePositionVariation = 0.1f;
		[SerializeField]
		public float boneRotationVariation = 0.1f;

		// Editor Only
		public bool invalid = false;

		public EmotionMarker (string emotion, float startTime, float endTime, float blendInTime, float blendOutTime, bool blendToMarker, bool blendFromMarker, bool customBlendIn, bool customBlendOut) {
			this.emotion = emotion;
			this.startTime = startTime;
			this.endTime = endTime;
			this.blendInTime = blendInTime;
			this.blendOutTime = blendOutTime;
			this.blendToMarker = blendToMarker;
			this.blendFromMarker = blendFromMarker;
			this.customBlendIn = customBlendIn;
			this.customBlendOut = customBlendOut;
		}

		public EmotionMarker (EmotionMixer mixer, float startTime, float endTime, float blendInTime, float blendOutTime, bool blendToMarker, bool blendFromMarker, bool customBlendIn, bool customBlendOut) {
			isMixer = true;
			this.mixer = mixer;
			this.startTime = startTime;
			this.endTime = endTime;
			this.blendInTime = blendInTime;
			this.blendOutTime = blendOutTime;
			this.blendToMarker = blendToMarker;
			this.blendFromMarker = blendFromMarker;
			this.customBlendIn = customBlendIn;
			this.customBlendOut = customBlendOut;
		}

		public EmotionMarker (string emotion, float startTime, float endTime, float blendInTime, float blendOutTime, bool blendToMarker, bool blendFromMarker, bool customBlendIn, bool customBlendOut, float intensity) {
			this.emotion = emotion;
			this.startTime = startTime;
			this.endTime = endTime;
			this.blendInTime = blendInTime;
			this.blendOutTime = blendOutTime;
			this.blendToMarker = blendToMarker;
			this.blendFromMarker = blendFromMarker;
			this.customBlendIn = customBlendIn;
			this.customBlendOut = customBlendOut;
			this.intensity = intensity;
		}

		public EmotionMarker CreateCopy () {
			EmotionMarker m = new EmotionMarker(emotion, startTime, endTime, blendInTime, blendOutTime, blendToMarker, blendFromMarker, customBlendIn, customBlendOut, intensity);

			m.isMixer = isMixer;
			m.mixer = mixer;
			m.blendableVariation = blendableVariation;
			m.bonePositionVariation = bonePositionVariation;
			m.boneRotationVariation = boneRotationVariation;
			m.intensityVariation = intensityVariation;
			m.continuousVariation = continuousVariation;

			return m;
		}
	}
}