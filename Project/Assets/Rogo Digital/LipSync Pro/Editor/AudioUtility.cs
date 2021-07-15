using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RogoDigital {
    public static class AudioUtility {

        public static AudioSource source;

        public static void Initialize () {
            GameObject go = new GameObject("LipSync Editor Audio", typeof(AudioSource));
            go.hideFlags = HideFlags.HideAndDontSave;

            source = go.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = EditorPrefs.GetFloat("LipSync_Volume", 1f);
        }

        public static void PlayClip (AudioClip clip) {
            if (source == null) Initialize();

            source.clip = clip;
            source.Play();
        }

        public static void StopClip (AudioClip clip) {
            if (source == null) Initialize();

            SetClipSamplePosition(clip, 0);
            source.Stop();
        }

        public static void PauseClip (AudioClip clip) {
            if (source == null) Initialize();

            source.Pause();
        }

        public static void ResumeClip (AudioClip clip) {
            if (source == null) Initialize();

            source.UnPause();
        }

        public static void SetVolume (float volume) {
            if (source == null) Initialize();

            source.volume = volume;
        }

        public static bool IsClipPlaying (AudioClip clip) {
            if (source == null) Initialize();

            return source.isPlaying;
        }

        public static void StopAllClips () {
            if (source == null) Initialize();

            source.Stop();
        }

        public static float GetClipPosition (AudioClip clip) {
            if (source == null) Initialize();

            return source.time;
        }

        public static void SetClipSamplePosition (AudioClip clip, int iSamplePosition) {
            if (source == null) Initialize();

            source.timeSamples = Mathf.Clamp(iSamplePosition, 0, clip.samples - 1);
        }

        public static int GetSampleCount (AudioClip clip) {
            if (source == null) Initialize();

            return clip.samples;
        }

#if UNITY_5_3_6 || UNITY_5_4_OR_NEWER
        // Waveform caching
        private static AudioClip storedClip;
        private static float[] minMaxData;

        public static void DrawWaveForm(AudioClip clip, int channel, Rect position) {
            DrawWaveForm(clip, channel, position, 0, clip.length);
        }

        public static void DrawWaveForm (AudioClip clip, int channel, Rect position, float start, float length) {
            if (minMaxData != null && clip == storedClip) {
                var curveColor = new Color(255 / 255f, 168 / 255f, 7 / 255f);
                int numChannels = clip.channels;
                int numSamples = Mathf.FloorToInt(minMaxData.Length / (2f * numChannels) * (length / clip.length));

                AudioCurveRendering.DrawMinMaxFilledCurve(
                    position,
                    delegate (float x, out Color col, out float minValue, out float maxValue) {
                        col = curveColor;
                        float p = Mathf.Clamp(x * (numSamples - 2), 0.0f, numSamples - 2);
                        int i = (int)Mathf.Floor(p);
                        float s = (start / clip.length) * Mathf.FloorToInt(minMaxData.Length / (2 * numChannels) - 2);
                        int si = (int)Mathf.Floor(s);

                        int offset1 = Mathf.Clamp(((i + si) * numChannels + channel) * 2, 0, minMaxData.Length - 2);
                        int offset2 = Mathf.Clamp(offset1 + numChannels * 2, 0, minMaxData.Length - 2);

                        minValue = Mathf.Min(minMaxData[offset1 + 1], minMaxData[offset2 + 1]);
                        maxValue = Mathf.Max(minMaxData[offset1 + 0], minMaxData[offset2 + 0]);
                        if (minValue > maxValue) { float tmp = minValue; minValue = maxValue; maxValue = tmp; }
                    }
                );

                return;
            }

			// If execution has reached this point, the waveform data needs generating
			var path = AssetDatabase.GetAssetPath(clip);
			if (path == null)
				return;
			var importer = AssetImporter.GetAtPath(path);
			if (importer == null)
				return;
			var assembly = Assembly.GetAssembly(typeof(AssetImporter));
			if (assembly == null)
				return;
			var type = assembly.GetType("UnityEditor.AudioUtil");
			if (type == null)
				return;
			var audioUtilGetMinMaxData = type.GetMethod("GetMinMaxData");
			if (audioUtilGetMinMaxData == null)
				return;
			minMaxData = audioUtilGetMinMaxData.Invoke(null, new object[] { importer }) as float[];

			storedClip = clip;
			if (minMaxData == null) return;

            DrawWaveForm(clip, channel, position, start, length);
        }
#else
		public static Texture2D GetWaveForm (AudioClip clip, int channel, float width, float height) {
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(
				"GetWaveForm",
				BindingFlags.Static | BindingFlags.Public
				);

			string path = AssetDatabase.GetAssetPath(clip);
			AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);

			Texture2D texture = (Texture2D)method.Invoke(
				null,
				new object[] {
				clip,
				importer,
				channel,
				width,
				height
			}
			);

			return texture;
		}
#endif

    }
}