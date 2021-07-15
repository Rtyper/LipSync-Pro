using UnityEngine;
using System.Collections.Generic;

#pragma warning disable 618

namespace RogoDigital.Lipsync {
	/// <summary>
	/// Stores a collection of phonemes to be used on a project-wide basis.
	/// </summary>
	[System.Serializable, CreateAssetMenu(fileName = "New Phoneme Set", menuName = "LipSync Pro/Phoneme Set")]
	public class PhonemeSet : ScriptableObject {

		public bool isLegacyFormat = true;
		[SerializeField]
		public string scriptingName;
		[SerializeField, System.Obsolete("Use phonemeList instead.", false)]
		public PhonemeCollection phonemes = new PhonemeCollection();
		[SerializeField]
		public List<Phoneme> phonemeList = new List<Phoneme>();

		[SerializeField, System.Obsolete("Use phonemeList[index].guideImage instead.", false)]
		public Texture2D[] guideImages;

		public void UpdateFormat ()
		{
			if (!isLegacyFormat)
				return;

			phonemeList.Clear();

			for (int i = 0; i < phonemes.Length; i++)
			{
				var newPhoneme = new Phoneme(phonemes[i].name, phonemes[i].number, phonemes[i].flag);

				if(i < guideImages.Length)
					newPhoneme.guideImage = guideImages[i];

				phonemeList.Add(newPhoneme);
			}

			isLegacyFormat = false;
		}

		[System.Serializable]
		public class PhonemeCollection {
			public List<string> phonemeNames;

            public int Length { get { return phonemeNames.Count; } }

			public Phoneme this[int index] {
				get {
					return new Phoneme(phonemeNames[index], index, Mathf.RoundToInt(Mathf.Pow(2, index)));
				}
			}

			public PhonemeCollection () {
				phonemeNames = new List<string>();
			}
		}

		[System.Serializable]
		public class Phoneme {
			/// <summary>
			/// The name of the phoneme.
			/// </summary>
			public string name;

			/// <summary>
			/// Sequential base-10 index of the phoneme
			/// </summary>
			public int number;

			/// <summary>
			/// Sequential power of 2 identifier for this phoneme (for use in bitmasks)
			/// </summary>
			public int flag;

            /// <summary>
            /// If a phoneme is marked as visually important, some actions in LipSync will avoid reducing its intensity/visibility.
            /// In English, this would be sounds like F, L or P, where moving the lips or tongue out of position would make the sound impossible.
            /// </summary>
            public bool visuallyImportant;

			/// <summary>
			/// Guide image to be displayed in the Scene View when editing this phoneme.
			/// </summary>
			public Texture2D guideImage;

			public Phoneme (string name, int number, int flag) {
				this.name = name;
				this.number = number;
				this.flag = flag;
			}
		}
	}

	#pragma warning restore 618
}