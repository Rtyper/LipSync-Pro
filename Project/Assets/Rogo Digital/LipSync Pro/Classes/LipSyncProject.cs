using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync {
	/// <summary>
	/// Stores per-project information and settings used by LipSync Pro.
	/// </summary>
	public class LipSyncProject : ScriptableObject {
		/// <summary>
		/// Master array of available emotion names.
		/// </summary>
		[SerializeField]
		public string[] emotions;
		/// <summary>
		/// Array of Colors used to represent emotions in the editor.
		/// </summary>
		[SerializeField]
		public Color[] emotionColors;

		/// <summary>
		/// Master list of available gesture names
		/// </summary>
		[SerializeField]
		public List<string> gestures = new List<string>();

		/// <summary>
		/// PhonemeSet used for this project
		/// </summary>
		[SerializeField]
		public PhonemeSet phonemeSet;
	}
}