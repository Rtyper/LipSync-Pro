using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class EmotionMixer {
		[SerializeField]
		public List<EmotionComponent> emotions;
		[SerializeField]
		public MixingMode mixingMode;

		// Editor Only
		public Color displayColor;

		public EmotionMixer () {
			emotions = new List<EmotionComponent>();
			displayColor = new Color(0, 0, 0);
		}

		public EmotionShape GetShape (LipSync character) {
			EmotionShape shape = new EmotionShape("Mixed");

			if (!character) return shape;
			if (!character.blendSystem) return shape;

			// Cache Emotions
			Dictionary<string, EmotionShape> emotionCache = new Dictionary<string, EmotionShape>();
			foreach (EmotionShape emotionShape in character.emotions) {
				emotionCache.Add(emotionShape.emotion, emotionShape);
			}

			for (int i = 0; i < emotions.Count; i++) {
				if (emotionCache.ContainsKey(emotions[i].emotion)) {
					EmotionShape subShape = emotionCache[emotions[i].emotion];

					// Blendables
					for (int b = 0; b < subShape.blendShapes.Count; b++) {
						if (shape.blendShapes.Contains(subShape.blendShapes[b])) {
							Mathf.Clamp(shape.weights[shape.blendShapes.IndexOf(subShape.blendShapes[b])] += subShape.weights[b] * emotions[i].weight, character.blendSystem.blendRangeLow, character.blendSystem.blendRangeHigh);

						} else {
							shape.blendShapes.Add(subShape.blendShapes[b]);
							shape.weights.Add(subShape.weights[b] * emotions[i].weight);

						}
					}

					// Bones
					for (int b = 0; b < subShape.bones.Count; b++) {
						BoneShape bone = subShape.bones[b];

						if (shape.HasBone(bone.bone)) {
							shape.bones[shape.IndexOfBone(bone.bone)].endPosition += bone.endPosition * emotions[i].weight;
							shape.bones[shape.IndexOfBone(bone.bone)].endRotation += bone.endRotation * emotions[i].weight;
						} else {
							shape.bones.Add(new BoneShape(bone.bone, bone.endPosition * emotions[i].weight, bone.endRotation * emotions[i].weight));
						}
					}
				}
			}

			return shape;
		}

		public void SetWeight (int index, float weight) {
			SetWeight(index, weight, false);
		}

		public void SetWeight (int index, float weight, bool bypassMinChecks) {

			if (mixingMode == MixingMode.Additive) {
				emotions[index] = new EmotionComponent(emotions[index].emotion, weight);
				return;
			}

			if (!bypassMinChecks) weight = Mathf.Clamp(weight, 0.01f, 1);

			float totalWeight = 0;
			float[] oldWeights = new float[emotions.Count];

			if ((emotions.Count) == 1) {
				emotions[index] = new EmotionComponent(emotions[index].emotion, 1);
				return;
			}

			for (int i = 0; i < emotions.Count; i++) {
				oldWeights[i] = emotions[i].weight;
				if (i != index) {
					totalWeight += emotions[i].weight;
				}
			}

			emotions[index] = new EmotionComponent(emotions[index].emotion, weight);
			float newTotalWeight = totalWeight - (weight - oldWeights[index]);

			for (int i = 0; i < emotions.Count; i++) {
				if (i != index) {
					float newWeight = newTotalWeight * (emotions[i].weight / totalWeight);

					if (newWeight > 0.02f || bypassMinChecks) {
						emotions[i] = new EmotionComponent(emotions[i].emotion, newWeight);
					} else {
						for (int a = 0; a < emotions.Count; a++) {
							emotions[a] = new EmotionComponent(emotions[a].emotion, oldWeights[a]);
						}
						break;
					}
				}
			}
		}

		[System.Serializable]
		public struct EmotionComponent {
			public string emotion;
			public float weight;

			public EmotionComponent (string emotion, float weight) {
				this.emotion = emotion;
				this.weight = weight;
			}
		}

		public enum MixingMode {
			Normal,
			Additive,
		}
	}

}