using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.Experimental
{
	public interface IAnimClipExportProvider
	{
		void CreateClipCurves(BlendSystem blendSystem, AnimationClip clip, List<int> indexBlendables, List<AnimationCurve> animCurves, LipSyncClipSetup setup, float smoothingWeight);
	}
}