using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.AutoSync
{
	[AutoSyncModuleInfo("Phoneme Modification/Marker Cleanup Module", "Does a simple 'thinning out' of phoneme markers based on a minimum distance between them. Can improve very dense results, but could cause important markers to be removed.", "Rogo Digital")]
	public class ASPhonemeMarkerCleanupModule : AutoSyncModule
	{
		public float cleanupAggression = 0.003f;

		public override ClipFeatures GetCompatibilityRequirements ()
		{
			return ClipFeatures.Phonemes;
		}

		public override ClipFeatures GetOutputCompatibility ()
		{
			return ClipFeatures.None;
		}

		public override void Process (LipSyncData inputClip, AutoSync.ASProcessDelegate callback)
		{
			List<PhonemeMarker> output = new List<PhonemeMarker>(inputClip.phonemeData);
			List<bool> markedForDeletion = new List<bool>();
			output.Sort(LipSync.SortTime);

			for (int m = 0; m < inputClip.phonemeData.Length; m++)
			{
				if (m > 0)
				{
					if (inputClip.phonemeData[m].time - inputClip.phonemeData[m - 1].time < cleanupAggression && !markedForDeletion[m - 1])
					{
						markedForDeletion.Add(true);
					}
					else
					{
						markedForDeletion.Add(false);
					}
				}
				else
				{
					markedForDeletion.Add(false);
				}
			}

			for (int m = 0; m < markedForDeletion.Count; m++)
			{
				if (markedForDeletion[m])
				{
					output.Remove(inputClip.phonemeData[m]);
				}
			}

			inputClip.phonemeData = output.ToArray();
			callback.Invoke(inputClip, new AutoSync.ASProcessDelegateData(true, "", ClipFeatures.None));
		}
	}
}