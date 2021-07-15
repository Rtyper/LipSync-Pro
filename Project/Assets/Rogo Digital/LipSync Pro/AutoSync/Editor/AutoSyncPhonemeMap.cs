using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.AutoSync
{
	/// <summary>
	/// Structure for containing mappings between two seperate phoneme sets, either internal or external to LipSync Pro.
	/// </summary>
	[System.Serializable]
	public class AutoSyncPhonemeMap
	{
		/// <summary>
		/// Array of mappings between phoneme labels
		/// </summary>
		public PhonemeMapping[] map;

		/// <summary>
		/// Creates a string-string Dictionary to look-up "B" labels from "A" Labels.
		/// </summary>
		/// <returns>Dictionary containing mappings</returns>
		public Dictionary<string, string> GenerateAtoBDictionary ()
		{
			var dic = new Dictionary<string, string>();
			for (int i = 0; i < map.Length; i++)
			{
				dic.Add(map[i].aLabel, map[i].bLabel);
			}
			return dic;
		}

		/// <summary>
		/// Creates a string-string Dictionary to look-up "A" labels from "B" Labels.
		/// </summary>
		/// <returns>Dictionary containing mappings</returns>
		public Dictionary<string, string> GenerateBtoADictionary ()
		{
			var dic = new Dictionary<string, string>();
			for (int i = 0; i < map.Length; i++)
			{
				dic.Add(map[i].bLabel, map[i].aLabel);
			}
			return dic;
		}

		/// <summary>
		/// A single two-way mapping between phoneme labels
		/// </summary>
		[System.Serializable]
		public class PhonemeMapping
		{
			public string aLabel;
			public string bLabel;

			public PhonemeMapping (string aLabel, string bLabel)
			{
				this.aLabel = aLabel;
				this.bLabel = bLabel;
			}
		}

		public enum MappingMode
		{
			AutoDetect,
			InternalMap,
			ExternalMap,
		}
	}
}