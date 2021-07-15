using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.AutoSync
{
	[CreateAssetMenu(fileName = "External Phoneme Map", menuName = "LipSync Pro/AutoSync/External Phoneme Map")]
	public class AutoSyncExternalPhonemeMap : ScriptableObject
	{
		public string displayName;
		public string setAName, setBName;
		public AutoSyncPhonemeMap phonemeMap = new AutoSyncPhonemeMap();
	}
}