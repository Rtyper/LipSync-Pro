using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace RogoDigital.Lipsync.AutoSync
{
	public abstract class AutoSyncModuleSettings : ScriptableObject
	{
		public virtual void InitSetupWizardValues () { }
		public virtual void DrawSetupWizardSection () { }
		public virtual void ApplySetupWizardValues () { }

		protected static string SearchForFile (string rootPath, string searchPattern)
		{
			string[] paths = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);
			if (paths.Length > 0)
			{
				return paths[0];
			}
			return null;
		}
	}
}