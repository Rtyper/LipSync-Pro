using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.Experimental
{
	[AnimClipExportProvider(typeof(BlendshapeBlendSystem))]
	public class BlendshapeBlendSystemProvider : IAnimClipExportProvider
	{
		public void CreateClipCurves(BlendSystem blendSystem, AnimationClip clip, List<int> indexBlendables, List<AnimationCurve> animCurves, LipSyncClipSetup setup, float smoothingWeight)
		{
			var bs = (BlendshapeBlendSystem)blendSystem;

			for (int i = 0; i < indexBlendables.Count; i++)
			{
				var curve = animCurves[i];
				AnimClipExport.ChangeCurveLength(curve, setup.FileLength, smoothingWeight);

				clip.SetCurve(bs.characterMesh.name, typeof(SkinnedMeshRenderer), "blendShape." + bs.characterMesh.sharedMesh.GetBlendShapeName(indexBlendables[i]), curve);

				for (int m = 0; m < bs.optionalOtherMeshes.Length; m++)
				{
					clip.SetCurve(bs.optionalOtherMeshes[m].name, typeof(SkinnedMeshRenderer), "blendShape." + bs.optionalOtherMeshes[m].sharedMesh.GetBlendShapeName(indexBlendables[i]), curve);
				}
			}
		}
	}
}