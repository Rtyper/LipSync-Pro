using UnityEngine;
using System.IO;

namespace RogoDigital.Lipsync
{
	public class LipSyncPreset : ScriptableObject
	{
		// Data
		public string displayPath;
		public bool isRelative;
		[SerializeField]
		public PhonemeShapeInfo[] phonemeShapes;
		[SerializeField]
		public EmotionShapeInfo[] emotionShapes;

#if UNITY_EDITOR
		private void OnEnable()
		{
			// Auto-update: Convert asset path to the displayPath field on old presets that didn't have one set.
			if (string.IsNullOrEmpty(displayPath))
			{
				string path = UnityEditor.AssetDatabase.GetAssetPath(this);
				if (path.Contains("Presets/"))
				{
					var splitPath = path.Split('/');
					string newPath = "";
					bool adding = false;
					for (int i = 0; i < splitPath.Length; i++)
					{
						if (adding)
						{
							if (i == splitPath.Length - 1)
							{
								newPath += Path.GetFileNameWithoutExtension(splitPath[i]);
							}
							else
							{
								newPath += splitPath[i] + "/";
							}
						}
						else
						{
							if (splitPath[i].ToLowerInvariant() == "presets")
							{
								adding = true;
							}
						}
					}
					displayPath = newPath;
				}
				else
				{
					displayPath = Path.GetFileNameWithoutExtension(path);
				}
			}
		}
#endif

		// Functions
		/// <summary>
		/// Returns the index of the blendable in blendSystem that best matches the supplied BlendableInfo.
		/// </summary>
		/// <param name="blendable"></param>
		/// <param name="blendSystem"></param>
		/// <returns>The index of the blendable if found, or -1 if not.</returns>
		public int FindBlendable(BlendableInfo blendable, BlendSystem blendSystem)
		{
			// First attempts to match based on name, then index. If both fail, returns -1.
			if (!string.IsNullOrEmpty(blendable.blendableName))
			{
				string cleanName = blendable.blendableName;

				if (cleanName.Contains("(" + blendable.blendableNumber.ToString() + ")"))
				{
					string[] parts = cleanName.Split(new string[] { "(" + blendable.blendableNumber.ToString() + ")" }, System.StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length > 0)
					{
						cleanName = parts[0];
					}
				}

				string[] blendables = blendSystem.GetBlendables();

				for (int a = 0; a < blendables.Length; a++)
				{
					string cleanBlendableName = blendables[a];
					if (cleanBlendableName.Contains("(" + a.ToString() + ")"))
					{
						string[] parts = cleanBlendableName.Split(new string[] { "(" + a.ToString() + ")" }, System.StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length > 0)
						{
							cleanBlendableName = parts[0];
						}
					}

					if (cleanBlendableName == cleanName)
					{
						return a;
					}
				}
			}

			if (blendable.blendableNumber < blendSystem.blendableCount)
			{
				if (!string.IsNullOrEmpty(blendable.blendableName))
				{
					if (blendable.blendableName != blendSystem.GetBlendables()[blendable.blendableNumber])
					{
						Debug.LogWarning("[LipSync] Blendable " + blendable.blendableName + " used in the '" + this.name + "' preset couldn't be matched based on name, and the blendable at the same index in the " + blendSystem.GetType().Name + " has a different name. This may not be the shape you were expecting.");
					}
				}

				return blendable.blendableNumber;
			}

			return -1;
		}

		/// <summary>
		/// Returns the Transform that best matches the supplied BoneInfo, using /searchRoot/ as a base.
		/// </summary>
		/// <param name="bone"></param>
		/// <param name="blendSystem"></param>
		/// <returns>The matching bone transform if found, or null if not.</returns>
		public Transform FindBone(BoneInfo bone, Transform searchRoot)
		{
			// First attempts to find the transform at the same position in the hierarchy relative to searchRoot, then a transform anywhere under searchRoot with the same name.
			// If both fail, returns null;
			Transform fullMatch = searchRoot.Find(bone.path + bone.name);
			if (fullMatch != null) return fullMatch;

			return searchRoot.FindDeepChild(bone.name);
		}

		public void CreateFromShapes(PhonemeShape[] phonemes, EmotionShape[] emotions, BlendSystem blendSystem, bool relative)
		{
			isRelative = relative;
			phonemeShapes = new PhonemeShapeInfo[phonemes.Length];
			emotionShapes = new EmotionShapeInfo[emotions.Length];

			for (int s = 0; s < phonemeShapes.Length; s++)
			{
				phonemeShapes[s] = new PhonemeShapeInfo();
				phonemeShapes[s].phonemeName = phonemes[s].phonemeName;

				phonemeShapes[s].blendables = new BlendableInfo[phonemes[s].blendShapes.Count];
				for (int b = 0; b < phonemeShapes[s].blendables.Length; b++)
				{
					phonemeShapes[s].blendables[b].blendableNumber = phonemes[s].blendShapes[b];
					phonemeShapes[s].blendables[b].weight = phonemes[s].weights[b];

					if (blendSystem != null)
					{
						phonemeShapes[s].blendables[b].blendableName = blendSystem.GetBlendables()[phonemes[s].blendShapes[b]];
					}
				}

				phonemeShapes[s].bones = new BoneInfo[phonemes[s].bones.Count];
				for (int b = 0; b < phonemeShapes[s].bones.Length; b++)
				{
					phonemeShapes[s].bones[b].name = phonemes[s].bones[b].bone.name;
					phonemeShapes[s].bones[b].lockPosition = phonemes[s].bones[b].lockPosition;
					phonemeShapes[s].bones[b].lockRotation = phonemes[s].bones[b].lockRotation;
					phonemeShapes[s].bones[b].lockScale = phonemes[s].bones[b].lockScale;

					if (relative)
					{
						phonemeShapes[s].bones[b].localPosition = phonemes[s].bones[b].neutralPosition - phonemes[s].bones[b].endPosition;
						phonemeShapes[s].bones[b].localRotation = phonemes[s].bones[b].neutralRotation - phonemes[s].bones[b].endRotation;
						phonemeShapes[s].bones[b].localScale = phonemes[s].bones[b].neutralScale - phonemes[s].bones[b].endScale;
					}
					else
					{
						phonemeShapes[s].bones[b].localPosition = phonemes[s].bones[b].endPosition;
						phonemeShapes[s].bones[b].localRotation = phonemes[s].bones[b].endRotation;
						phonemeShapes[s].bones[b].localScale = phonemes[s].bones[b].endScale;
					}
				}
			}

			for (int s = 0; s < emotionShapes.Length; s++)
			{
				emotionShapes[s] = new EmotionShapeInfo();
				emotionShapes[s].emotion = emotions[s].emotion;

				emotionShapes[s].blendables = new BlendableInfo[emotions[s].blendShapes.Count];
				for (int b = 0; b < emotionShapes[s].blendables.Length; b++)
				{
					emotionShapes[s].blendables[b].blendableNumber = emotions[s].blendShapes[b];
					emotionShapes[s].blendables[b].weight = emotions[s].weights[b];

					if (blendSystem != null)
					{
						emotionShapes[s].blendables[b].blendableName = blendSystem.GetBlendables()[emotions[s].blendShapes[b]];
					}
				}

				emotionShapes[s].bones = new BoneInfo[emotions[s].bones.Count];
				for (int b = 0; b < emotionShapes[s].bones.Length; b++)
				{
					emotionShapes[s].bones[b].name = emotions[s].bones[b].bone.name;
					emotionShapes[s].bones[b].lockPosition = emotions[s].bones[b].lockPosition;
					emotionShapes[s].bones[b].lockRotation = emotions[s].bones[b].lockRotation;
					emotionShapes[s].bones[b].lockScale = emotions[s].bones[b].lockScale;

					if (relative)
					{
						emotionShapes[s].bones[b].localPosition = emotions[s].bones[b].neutralPosition - phonemes[s].bones[b].endPosition;
						emotionShapes[s].bones[b].localRotation = emotions[s].bones[b].neutralRotation - phonemes[s].bones[b].endRotation;
						emotionShapes[s].bones[b].localScale = emotions[s].bones[b].neutralScale - emotions[s].bones[b].endScale;
					}
					else
					{
						emotionShapes[s].bones[b].localPosition = emotions[s].bones[b].endPosition;
						emotionShapes[s].bones[b].localRotation = emotions[s].bones[b].endRotation;
						emotionShapes[s].bones[b].localScale = emotions[s].bones[b].endScale;
					}
				}
			}
		}

		// Structures
		[System.Serializable]
		public struct PhonemeShapeInfo
		{
			[SerializeField]
			public string phonemeName;
			[SerializeField, System.Obsolete("Please use PhonemeShapeInfo.phonemeName")]
			public Phoneme phoneme;
			[SerializeField]
			public BlendableInfo[] blendables;
			[SerializeField]
			public BoneInfo[] bones;
		}
		[System.Serializable]
		public struct EmotionShapeInfo
		{
			[SerializeField]
			public string emotion;
			[SerializeField]
			public BlendableInfo[] blendables;
			[SerializeField]
			public BoneInfo[] bones;
		}
		[System.Serializable]
		public struct BlendableInfo
		{
			[SerializeField]
			public int blendableNumber;
			[SerializeField]
			public string blendableName;
			[SerializeField]
			public float weight;
		}
		[System.Serializable]
		public struct BoneInfo
		{
			[SerializeField]
			public string path;
			[SerializeField]
			public string name;
			[SerializeField]
			public Vector3 localPosition;
			[SerializeField]
			public Vector3 localRotation;
			[SerializeField]
			public Vector3 localScale;
			[SerializeField]
			public bool lockPosition;
			[SerializeField]
			public bool lockRotation;
			[SerializeField]
			public bool lockScale;
		}
	}
}