using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync
{
	public class TemporaryLipSyncData : ScriptableObject
	{
		public AudioClip clip;
		public List<PhonemeMarker> phonemeData;
		public List<EmotionMarker> emotionData;
		public List<GestureMarker> gestureData;

		public float version;
		public float length = 10;
		public string transcript = "";

		private void OnEnable()
		{
			hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
		}

		public static explicit operator TemporaryLipSyncData(LipSyncData data)
		{
			var output = CreateInstance<TemporaryLipSyncData>();

			// Data
			output.phonemeData = new List<PhonemeMarker>();
			output.emotionData = new List<EmotionMarker>();
			output.gestureData = new List<GestureMarker>();

			if (data.phonemeData != null)
			{
				for (int i = 0; i < data.phonemeData.Length; i++)
				{
					output.phonemeData.Add(data.phonemeData[i].CreateCopy());
				}
			}

			if (data.emotionData != null)
			{
				for (int i = 0; i < data.emotionData.Length; i++)
				{
					output.emotionData.Add(data.emotionData[i].CreateCopy());
				}
			}

			if (data.gestureData != null)
			{
				for (int i = 0; i < data.gestureData.Length; i++)
				{
					output.gestureData.Add(data.gestureData[i].CreateCopy());
				}
			}

			output.clip = data.clip;
			output.version = data.version;
			output.length = data.length;
			output.transcript = data.transcript;

			return output;
		}
	}
}