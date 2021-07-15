using UnityEngine;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class PhonemeMarker : System.Object {
		[SerializeField, System.Obsolete("Use PhonemeMarker.phonemeNumber instead.")]
		public Phoneme phoneme;
		[SerializeField]
		public int phonemeNumber;
		[SerializeField]
		public float time;
		[SerializeField]
		public float intensity = 1;
		[SerializeField]
		public bool sustain = false;
		[SerializeField]
		public bool useRandomness = false;
		[SerializeField]
		public float intensityRandomness = 0.1f;
		[SerializeField]
		public float blendableRandomness = 0.3f;
		[SerializeField]
		public float bonePositionRandomness = 0.3f;
		[SerializeField]
		public float boneRotationRandomness = 0.3f;

		public PhonemeMarker (int phonemeNumber, float time, float intensity, bool sustain) {
			this.phonemeNumber = phonemeNumber;
			this.time = time;
			this.intensity = intensity;
			this.sustain = sustain;
		}

		public PhonemeMarker (int phonemeNumber, float time) {
			this.phonemeNumber = phonemeNumber;
			this.time = time;
		}

		[System.Obsolete("Use int constructors instead.")]
		public PhonemeMarker (Phoneme phoneme, float time, float intensity, bool sustain) {
			this.phoneme = phoneme;
			this.time = time;
			this.intensity = intensity;
			this.sustain = sustain;
		}

		[System.Obsolete("Use int constructors instead.")]
		public PhonemeMarker (Phoneme phoneme, float time) {
			this.phoneme = phoneme;
			this.time = time;
		}

		public PhonemeMarker CreateCopy () {
			PhonemeMarker m = new PhonemeMarker(phonemeNumber, time, intensity, sustain);
			m.blendableRandomness = blendableRandomness;
			m.bonePositionRandomness = bonePositionRandomness;
			m.boneRotationRandomness = boneRotationRandomness;
			m.intensityRandomness = intensityRandomness;
			m.useRandomness = useRandomness;

			return m;
		}
	}
}