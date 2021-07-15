using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RogoDigital.Lipsync.AutoSync
{
	public class ASMontrealPhonemeDetectionSettings : AutoSyncModuleSettings
	{
		public string applicationPath;

		private void OnEnable ()
		{
			applicationPath = EditorPrefs.GetString("as_montrealfa_application_path");
		}

		public override void InitSetupWizardValues ()
		{
			string detectedOS = SystemInfo.operatingSystem;
			applicationPath = EditorPrefs.GetString("as_montrealfa_application_path");
			if (string.IsNullOrEmpty(applicationPath))
			{
				var dirGUIDS = AssetDatabase.FindAssets("AutoSync");
				if (dirGUIDS.Length == 0)
				{
					return;
				}
				string baseSearchPath = AssetDatabase.GUIDToAssetPath(dirGUIDS[0]);

				if (detectedOS.ToLowerInvariant().Contains("windows"))
				{
					string path = SearchForFile(baseSearchPath, "mfa_align.exe");
					if (path == null)
					{
						applicationPath = "";
					}
					else
					{
						applicationPath = path;
					}
				}
				else if (detectedOS.ToLowerInvariant().Contains("mac"))
				{
					string path = SearchForFile(baseSearchPath, "align");
					if (path == null)
					{
						applicationPath = "";
					}
					else
					{
						applicationPath = path;
					}
				}
				else
				{
					applicationPath = "";
				}
			}
		}

		public override void DrawSetupWizardSection ()
		{
			EditorGUILayout.HelpBox("The Montreal Forced Aligner contains multiple executables, the one required below is \"bin/mfa_align.exe\" on Windows, or \"lib/align\" on macOS - used to align phonemes with audio.", MessageType.Info);
			applicationPath = LipSyncEditorExtensions.DrawPathField("MFA Aligner Application Path", applicationPath, "ls_debug", "Find mfa_align executable");
		}

		public override void ApplySetupWizardValues ()
		{
			EditorPrefs.SetString("as_montrealfa_application_path", applicationPath);
			EditorPrefs.SetBool("as_montrealfa_application_path_verified", AutoSyncUtility.VerifyProgramAtPath(applicationPath, "align"));
		}
	}
}