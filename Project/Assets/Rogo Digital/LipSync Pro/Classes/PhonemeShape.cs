using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync
{
	[System.Serializable]
	public class PhonemeShape : Shape
	{

		[SerializeField]
		public string phonemeName;
		[SerializeField, System.Obsolete("Use phonemeName instead.")]
		public Phoneme phoneme;

		public PhonemeShape (string phonemeName)
		{
			this.phonemeName = phonemeName;
			blendShapes = new List<int>();
			weights = new List<float>();
			bones = new List<BoneShape>();
		}

		[System.Obsolete("Use the new string constructor instead.")]
		public PhonemeShape (Phoneme ePhoneme)
		{
			phoneme = ePhoneme;
			blendShapes = new List<int>();
			weights = new List<float>();
			bones = new List<BoneShape>();
		}
	}
}