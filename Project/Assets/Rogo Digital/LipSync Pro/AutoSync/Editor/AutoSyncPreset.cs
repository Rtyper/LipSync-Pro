using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.AutoSync
{
	[CreateAssetMenu(fileName = "AutoSync Preset", menuName = "LipSync Pro/AutoSync/AutoSync Preset")]
	public class AutoSyncPreset : ScriptableObject
	{
		public string displayName, description;
		public string[] modules;
		public string[] moduleSettings;

		public void CreateFromModules (AutoSyncModule[] currentModules)
		{
			modules = new string[currentModules.Length];
			moduleSettings = new string[currentModules.Length];

			for (int i = 0; i < currentModules.Length; i++)
			{
				modules[i] = currentModules[i].GetType().Name;
				moduleSettings[i] = JsonUtility.ToJson(currentModules[i]);
			}
		}
	}
}