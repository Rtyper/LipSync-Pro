using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace RogoDigital.Lipsync.Experimental
{
	[AnimClipExportProvider(typeof(AdvancedBlendshapeBlendSystem))]
	public class AdvancedBlendshapeBlendSystemProvider : IAnimClipExportProvider
	{
		public void CreateClipCurves(BlendSystem blendSystem, AnimationClip clip, List<int> indexBlendables, List<AnimationCurve> animCurves, LipSyncClipSetup setup, float smoothingWeight)
		{
			var bs = (AdvancedBlendshapeBlendSystem)blendSystem;

			var managerField = bs.GetType().GetField("manager", BindingFlags.NonPublic | BindingFlags.Instance);
			if (managerField == null)
			{
				return;
			}
			else
			{
				var manager = (BlendshapeManager)managerField.GetValue(blendSystem);
				if (manager == null)
				{
					return;
				}
				else
				{
					for (int i = 0; i < indexBlendables.Count; i++)
					{
						var curve = animCurves[i];
						AnimClipExport.ChangeCurveLength(curve, setup.FileLength, smoothingWeight);

						var mappings = manager.blendShapes[indexBlendables[i]].mappings;
						for (int m = 0; m < mappings.Length; m++)
						{
							clip.SetCurve(mappings[m].skinnedMeshRenderer.name, typeof(SkinnedMeshRenderer), "blendShape." + mappings[m].skinnedMeshRenderer.sharedMesh.GetBlendShapeName(mappings[m].blendShapeIndex), curve);
						}
					}
				}
			}
		}
	}
}