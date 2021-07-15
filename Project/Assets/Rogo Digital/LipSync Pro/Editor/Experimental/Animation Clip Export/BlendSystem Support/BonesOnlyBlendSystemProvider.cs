using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.Experimental
{
	[AnimClipExportProvider(typeof(BonesOnlyBlendSystem))]
	public class BonesOnlyBlendSystemProvider : IAnimClipExportProvider
	{
		public void CreateClipCurves(BlendSystem blendSystem, AnimationClip clip, List<int> indexBlendables, List<AnimationCurve> animCurves, LipSyncClipSetup setup, float smoothingWeight)
		{
			// This Page Left Intentionally Blank
		}
	}
}