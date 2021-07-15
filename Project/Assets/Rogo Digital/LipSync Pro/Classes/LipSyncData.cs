using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync
{
	[System.Serializable]
	public class LipSyncData : ScriptableObject
	{
		public AudioClip clip;
		public PhonemeMarker[] phonemeData;
		public EmotionMarker[] emotionData;
		public GestureMarker[] gestureData;

		public float version;
		public float length;
		public string transcript;

		public AnimationCurve[] phonemePoseCurves = new AnimationCurve[0];
		public AnimationCurve[] emotionPoseCurves = new AnimationCurve[0];

		public int targetComponentID;
		public bool isPreprocessed;

		public List<int> indexBlendables;
		public List<AnimationCurve> animCurves;

		public List<Transform> bones;
		public List<TransformAnimationCurve> boneCurves;

		public List<Vector3> boneNeutralPositions;
		public List<Vector3> boneNeutralScales;
		public List<Quaternion> boneNeutralRotations;

		public void GenerateCurves (int phonemeCount, int emotionCount)
		{
			phonemePoseCurves = new AnimationCurve[phonemeCount];
			emotionPoseCurves = new AnimationCurve[emotionCount];

			// Create Phoneme Pose Curves
			for (int i = 0; i < phonemePoseCurves.Length; i++)
			{
				phonemePoseCurves[i] = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 0) });
			}

			// Create Emotion Pose Curves
			for (int i = 0; i < emotionPoseCurves.Length; i++)
			{
				emotionPoseCurves[i] = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 0) });
			}

			// Generate Phoneme Pose Keyframes
			for (int i = 0; i < phonemeData.Length; i++)
			{
				for (int p = 0; p < phonemePoseCurves.Length; p++)
				{
					if (p == phonemeData[i].phonemeNumber)
						continue;

					phonemePoseCurves[p].AddKey(phonemeData[i].time, 0);
				}

				phonemePoseCurves[phonemeData[i].phonemeNumber].AddKey(phonemeData[i].time, phonemeData[i].intensity);
			}

			// Generate Emotion Pose Keyframes
			for (int i = 0; i < emotionData.Length; i++)
			{
				//emotionPoseCurves[emotionData[i].phonemeNumber].AddKey(phonemeData[i].time, phonemeData[i].intensity);
			}
		}

		public static explicit operator LipSyncData (TemporaryLipSyncData data)
		{
			var output = CreateInstance<LipSyncData>();
			output.phonemeData = new PhonemeMarker[data.phonemeData.Count];
			output.emotionData = new EmotionMarker[data.emotionData.Count];
			output.gestureData = new GestureMarker[data.gestureData.Count];

			for (int i = 0; i < data.phonemeData.Count; i++)
			{
				output.phonemeData[i] = data.phonemeData[i].CreateCopy();
			}
			for (int i = 0; i < data.emotionData.Count; i++)
			{
				output.emotionData[i] = data.emotionData[i].CreateCopy();
			}
			for (int i = 0; i < data.gestureData.Count; i++)
			{
				output.gestureData[i] = data.gestureData[i].CreateCopy();
			}

			output.clip = data.clip;
			output.version = data.version;
			output.length = data.length;
			output.transcript = data.transcript;

			return output;
		}
	}
}