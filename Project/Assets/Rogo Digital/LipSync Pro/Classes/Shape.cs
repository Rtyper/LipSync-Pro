using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class Shape : System.Object {

		/// <summary>
		/// The blendable indeces.
		/// </summary>
		[SerializeField]
		public List<int> blendShapes = new List<int>();

		/// <summary>
		/// The blendable names. Used for re-syncing
		/// </summary>
		[SerializeField]
		public List<string> blendableNames = new List<string>();

		/// <summary>
		/// The associated weights.
		/// </summary>
		[SerializeField]
		public List<float> weights = new List<float>();

		/// <summary>
		/// List of bone shapes.
		/// </summary>
		[SerializeField]
		public List<BoneShape> bones = new List<BoneShape>();

		/// <summary>
		/// Whether or not this shape exists in the project
		/// Will always be true for phoneme shapes.
		/// </summary>
		[SerializeField]
		public bool verified = true;

		public bool HasBone (Transform bone) {
			for (int b = 0; b < bones.Count; b++) {
				if (bones[b].bone == bone) return true;
			}
			return false;
		}

		public int IndexOfBone (Transform bone) {
			for (int b = 0; b < bones.Count; b++) {
				if (bones[b].bone == bone) return b;
			}
			return -1;
		}
	}
}