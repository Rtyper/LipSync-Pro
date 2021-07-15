using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync
{
	[System.Serializable]
	public class BlendshapeManager : MonoBehaviour
	{

		[Space]
		public AdvancedBlendShape[] blendShapes = new AdvancedBlendShape[0];

		[HideInInspector]
		public AdvancedBlendshapeBlendSystem blendSystem;

		[System.Serializable]
		public struct AdvancedBlendShape
		{
			public string name;
			public BlendShapeMapping[] mappings;
		}

		[System.Serializable]
		public struct BlendShapeMapping
		{
			public SkinnedMeshRenderer skinnedMeshRenderer;
			public int blendShapeIndex;
		}
	}
}