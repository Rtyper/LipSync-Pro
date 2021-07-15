using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.AutoSync
{
	[AutoSyncModuleInfo("Phoneme Modification/Intensity From Volume Module", "Sets the intensity of phoneme markers according to the volume of the audio.", "Rogo Digital")]
	public class ASPhonemeIntensityFromVolumeModule : AutoSyncModule
	{
		public bool applyCurveRelative;
		public AnimationCurve remapCurve = new AnimationCurve(new Keyframe[]
		{
			new Keyframe(0,0.6f),
			new Keyframe(0.8f,1)
		});

		public override ClipFeatures GetCompatibilityRequirements()
		{
			return ClipFeatures.Phonemes | ClipFeatures.AudioClip;
		}

		public override ClipFeatures GetOutputCompatibility()
		{
			return ClipFeatures.None;
		}

		public override void Process(LipSyncData inputClip, AutoSync.ASProcessDelegate callback)
		{
			var projectSettings = LipSyncEditorExtensions.GetProjectFile();

			float[] values = new float[inputClip.phonemeData.Length];
			float min = 1;
			float max = 0;

			for (int m = 0; m < inputClip.phonemeData.Length; m++)
			{
				values[m] = GetRMS(4096, Mathf.RoundToInt(inputClip.phonemeData[m].time * inputClip.clip.samples), inputClip.clip);

				if(values[m] > max)
				{
					max = values[m];
				}

				if (values[m] < min)
				{
					min = values[m];
				}
			}

			for (int m = 0; m < inputClip.phonemeData.Length; m++)
			{
				if(!projectSettings.phonemeSet.phonemeList[inputClip.phonemeData[m].phonemeNumber].visuallyImportant)
					inputClip.phonemeData[m].intensity = remapCurve.Evaluate(Mathf.InverseLerp(min, max, values[m]));
			}

			callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(true, "", ClipFeatures.None));
		}

		float GetRMS(int samples, int offset, AudioClip clip)
		{
			float[] sampleData = new float[samples];

			clip.GetData(sampleData, offset); // fill array with samples

			float sum = 0;
			for (int i = 0; i < samples; i++)
			{
				sum += sampleData[i] * sampleData[i]; // sum squared samples
			}

			return Mathf.Sqrt(sum / samples); // rms = square root of average
		}
	}
}